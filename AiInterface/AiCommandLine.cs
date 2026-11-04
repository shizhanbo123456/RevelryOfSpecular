// AI 操控的命令行接口：命令注册 / 解析 / 分发 + 中断原因列表，外部 AI 的唯一入口
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Ros.Transport;
using UnityEngine;
using UnityEngine.AI;

[Flags]
public enum AiPhase
{
    Offline = 1 << 0,
    Lobby = 1 << 1,
    Battle = 1 << 2,
    All = Offline | Lobby | Battle,
}

public enum AiIntent
{
    Stand,
    MoveTo,
    AttackTarget,
    Stop,
}

public enum AiInterruptKind
{
    CommandEnd,
    Timeout,
    ChatMention,
    LuaTrigger,
    LuaError,
    BattleStart,
    BattleEnd,
    OtherSignal,
    EnemySpotted,
    SkillBroadcast,
    NeutralSpawn,
    DayNight,
    SkillGained,
    Prompt,
    MoveFailed,
}

// 意图上行通道：协议接入前为 null，命令直接回「未接入」，不做假发送
public delegate bool AiIntentSender(AiIntent intent, Vector2 position, ushort targetId, out string error);

// 中断原因列表：有界、同类型合并、等待时整批取走
public sealed class AiInterruptHub
{
    private const int Capacity = 16;
    private const float MergeWindow = 2f;

    private sealed class Entry
    {
        public AiInterruptKind kind;
        public float time;
        public readonly List<string> fields = new();
        public int count = 1;
    }

    private readonly List<Entry> entries = new();
    private readonly HashSet<AiInterruptKind> enabled = new();
    private readonly StringBuilder builder = new();
    private readonly StringBuilder names = new();

    private Action<string> waiting;

    public AiInterruptHub()
    {
        // 默认只开核心中断，可选类由「设置可选中断」打开
        enabled.Add(AiInterruptKind.CommandEnd);
        enabled.Add(AiInterruptKind.Timeout);
        enabled.Add(AiInterruptKind.ChatMention);
        enabled.Add(AiInterruptKind.LuaTrigger);
        enabled.Add(AiInterruptKind.LuaError);
        enabled.Add(AiInterruptKind.BattleStart);
        enabled.Add(AiInterruptKind.BattleEnd);
        enabled.Add(AiInterruptKind.MoveFailed);
    }

    public bool HasWaiting => waiting != null;

    public static bool IsOptional(AiInterruptKind kind)
    {
        return kind == AiInterruptKind.OtherSignal
            || kind == AiInterruptKind.EnemySpotted
            || kind == AiInterruptKind.SkillBroadcast
            || kind == AiInterruptKind.NeutralSpawn
            || kind == AiInterruptKind.DayNight
            || kind == AiInterruptKind.SkillGained
            || kind == AiInterruptKind.Prompt;
    }

    public void SetEnabled(AiInterruptKind kind, bool on)
    {
        if (on) enabled.Add(kind);
        else enabled.Remove(kind);
    }

    public void SetAllOptional(bool on)
    {
        foreach (AiInterruptKind kind in Enum.GetValues(typeof(AiInterruptKind)))
        {
            if (!IsOptional(kind)) continue;
            if (on) enabled.Add(kind);
            else enabled.Remove(kind);
        }
    }

    public void Push(AiInterruptKind kind, params string[] fields)
    {
        if (!enabled.Contains(kind)) return;

        Entry target = null;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].kind != kind) continue;
            if (Time.time - entries[i].time > MergeWindow) break;
            target = entries[i];
            break;
        }

        if (target != null)
        {
            target.fields.Clear();
            if (fields != null) target.fields.AddRange(fields);
            target.time = Time.time;
            target.count++;
        }
        else
        {
            var entry = new Entry { kind = kind, time = Time.time };
            if (fields != null) entry.fields.AddRange(fields);
            entries.Add(entry);
            while (entries.Count > Capacity) entries.RemoveAt(0);
        }

        FlushWaiting();
    }

    public bool TryDrain(out string text)
    {
        text = null;
        if (entries.Count == 0) return false;
        text = Build();
        entries.Clear();
        return true;
    }

    public void BeginWait(Action<string> reply)
    {
        if (waiting != null) throw new InvalidOperationException("已有等待中断挂起");
        waiting = reply;
        FlushWaiting();
    }

    public void CancelWait() => waiting = null;

    public string DescribeEnabled()
    {
        names.Clear();
        names.Append("可选中断：");
        bool any = false;
        foreach (AiInterruptKind kind in Enum.GetValues(typeof(AiInterruptKind)))
        {
            if (!IsOptional(kind) || !enabled.Contains(kind)) continue;
            if (any) names.Append(',');
            names.Append(kind.ToString());
            any = true;
        }
        if (!any) names.Append("全关");
        return names.ToString();
    }

    private void FlushWaiting()
    {
        if (waiting == null || entries.Count == 0) return;
        var reply = waiting;
        waiting = null;
        string text = Build();
        entries.Clear();
        reply(text);
    }

    private string Build()
    {
        builder.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            if (i > 0) builder.Append('\n');
            var e = entries[i];
            builder.Append('[').Append(e.kind.ToString()).Append(']');
            for (int j = 0; j < e.fields.Count; j++)
            {
                builder.Append(j == 0 ? " " : " | ").Append(e.fields[j]);
            }
            if (e.count > 1) builder.Append(" ×").Append(e.count);
        }
        return builder.ToString();
    }
}

public sealed class AiCommandLine : ClientSubManager
{
    public static AiCommandLine Current { get; private set; }

    // 由意图协议层实现后赋值：返回 false 时 error 原样回给调用方
    public static AiIntentSender IntentSender;

    private sealed class Command
    {
        public string name;
        public AiPhase phase;
        public int minArgs;
        public int maxArgs;
        public string usage;
        public Action<string[], Action<string>> handler;
    }

    private struct ChatRecord
    {
        public string kind;
        public string camp;
        public string name;
        public string content;
    }

    private const int ChatLogCapacity = 200;
    private const string ScreenshotFolder = "Screenshots";

    private readonly List<Command> commands = new();
    private readonly List<ChatRecord> chatLog = new();
    private readonly List<string> selfBuffs = new();
    private readonly List<int> heldSnapshot = new();

    private readonly Dictionary<ushort, SCMinimapEntity> minimap = new();
    private readonly Dictionary<ushort, float> minimapSeen = new();
    private readonly List<ushort> minimapExpired = new();

    private readonly AiInterruptHub hub = new();

    private int selfHealth;
    private int selfMaxHealth;
    private SCDefensePointProgress guardProgress;

    private Timer.TimerHandle waitTimer;
    private bool lastReady;

    private Task<NetworkManager.ConnectResult> connectTask;
    private Action<string> connectReply;
    private string connectIp;

    public AiInterruptHub Interrupts => hub;

    public int SelfHealth => selfHealth;

    public int SelfMaxHealth => selfMaxHealth;

    public IReadOnlyList<string> SelfBuffs => selfBuffs;

    public SCDefensePointProgress GuardSnapshot => guardProgress;

    public void CopyMinimap(List<SCMinimapEntity> buffer)
    {
        buffer.Clear();
        foreach (var pair in minimap) buffer.Add(pair.Value);
    }

    public override void Init(ClientLogicManager owner)
    {
        base.Init(owner);
        Current = this;
        IntentSender = SendIntentChannel;
        RegisterBuiltin();
    }

    // 意图通道自接线：命令行与 Lua 的意图都从这里上行
    private static bool SendIntentChannel(AiIntent intent, Vector2 position, ushort targetId, out string error)
    {
        if (!NetworkManager.CanSendWorldCommand)
        {
            error = "未联网";
            return false;
        }
        Tool.NetworkManager.SendAiIntent(new CSAiIntent
        {
            intent = (int)intent,
            posX = position.x,
            posZ = position.y,
            targetId = targetId,
        });
        error = null;
        return true;
    }

    // 服务器回执：目前只有移动失败（目的地算不出路）
    public void OnServerFeedback(SCAiFeedback info)
    {
        if (info.kind == 0)
        {
            hub.Push(AiInterruptKind.MoveFailed,
                "坐标=(" + info.posX.ToString("0.##") + "," + info.posZ.ToString("0.##") + ")",
                "目标=" + info.targetId);
            return;
        }
        Debug.LogError("[AI] 未知的 AI 回执类型：" + info.kind);
    }

    public override void Tick(float deltaTime)
    {
        TickMinimapExpire();
        TickConnect();
        TickEnemySpot();
    }

    protected override void BindEvents()
    {
        EventManager.AddEvent<SCChat>(ClientEvent.OnChatMessage, OnChatMessage);
        EventManager.AddEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.AddEvent<int>(ClientEvent.OnShowPrompt, OnPrompt);
        EventManager.AddEvent<SCMinimapEntity>(ClientEvent.OnMinimapUpdate, OnMinimapUpdate);
        EventManager.AddEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplay);
        EventManager.AddEvent<SCDefensePointProgress>(ClientEvent.OnDefensePointProgress, OnGuardProgress);
        EventManager.AddEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
        EventManager.AddEvent<int>(ClientEvent.OnBattleEnd, OnBattleEnd);
        EventManager.AddEvent(ClientEvent.OnSkillBarRefresh, OnSkillBarRefresh);
        EventManager.AddEvent<SCQuickMessage>(ClientEvent.OnQuickMessage, OnQuickMessage);
    }

    protected override void UnbindEvents()
    {
        EventManager.RemoveEvent<SCChat>(ClientEvent.OnChatMessage, OnChatMessage);
        EventManager.RemoveEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.RemoveEvent<int>(ClientEvent.OnShowPrompt, OnPrompt);
        EventManager.RemoveEvent<SCMinimapEntity>(ClientEvent.OnMinimapUpdate, OnMinimapUpdate);
        EventManager.RemoveEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplay);
        EventManager.RemoveEvent<SCDefensePointProgress>(ClientEvent.OnDefensePointProgress, OnGuardProgress);
        EventManager.RemoveEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
        EventManager.RemoveEvent<int>(ClientEvent.OnBattleEnd, OnBattleEnd);
        EventManager.RemoveEvent(ClientEvent.OnSkillBarRefresh, OnSkillBarRefresh);
        EventManager.RemoveEvent<SCQuickMessage>(ClientEvent.OnQuickMessage, OnQuickMessage);
        if (waitTimer != null) waitTimer.Cancel();
        waitTimer = null;
        if (Current == this) Current = null;
    }

    #region 对外入口
    public static AiPhase CurrentPhase
    {
        get
        {
            if (Tool.NetworkManager == null) return AiPhase.Offline;
            if (!NetworkManager.CanSendWorldCommand) return AiPhase.Offline;
            return NetworkManager.BattleRunning ? AiPhase.Battle : AiPhase.Lobby;
        }
    }

    // 除「等待中断」外都立即回调；line 非法时也走回调，错误作为返回文本
    public void Submit(string line, Action<string> onResult)
    {
        if (onResult == null) throw new ArgumentNullException(nameof(onResult));
        if (string.IsNullOrEmpty(line) || line.Trim().Length == 0)
        {
            onResult("错误：空命令");
            return;
        }

        var tokens = SplitArgs(line);
        string name = tokens[0];

        if (name == "等待中断")
        {
            HandleWaitInterrupt(tokens, onResult);
            return;
        }

        if (hub.HasWaiting)
        {
            onResult("错误：存在挂起的「等待中断」，先取走中断再执行其它命令");
            return;
        }

        var cmd = Find(name);
        if (cmd == null)
        {
            onResult($"错误：未知命令 {name}（用 获取命令列表 查看）");
            return;
        }

        AiPhase phase = CurrentPhase;
        if ((cmd.phase & phase) == 0)
        {
            onResult($"错误：命令 {name} 在当前相位 {phase} 不可用");
            return;
        }

        int argCount = tokens.Count - 1;
        if (argCount < cmd.minArgs || (cmd.maxArgs >= 0 && argCount > cmd.maxArgs))
        {
            onResult($"错误：参数数量不符，用法 {cmd.usage}");
            return;
        }

        var args = new string[argCount];
        for (int i = 0; i < argCount; i++) args[i] = tokens[i + 1];
        cmd.handler(args, onResult);
    }
    #endregion

    #region 注册
    private void RegisterBuiltin()
    {
        // 通用
        Add("发送聊天", AiPhase.All, 2, -1, "发送聊天 <0 全频道|1 阵营> <内容>", SendChat);
        Add("读取聊天记录", AiPhase.All, 1, 1, "读取聊天记录 <数量>", ReadChat);
        Add("获取本地玩家名称", AiPhase.All, 0, 0, "获取本地玩家名称", GetPlayerName);
        Add("设置可选中断", AiPhase.All, 1, -1, "设置可选中断 <类型…|all|none>", SetOptionalInterrupts);
        Add("获取命令列表", AiPhase.All, 0, 0, "获取命令列表", ListCommands);
        Add("执行lua拓展内容", AiPhase.All, 1, -1, "执行lua拓展内容 <命令名> <参数…>", RunLuaExtension);

        // Home
        Add("选择角色", AiPhase.Offline, 2, 2, "选择角色 <进攻下标> <防守下标>", SelectCharacter);
        Add("连接服务器", AiPhase.Offline, 0, 1, "连接服务器 [ip]", Connect);
        Add("设置本地玩家名称", AiPhase.Offline, 1, -1, "设置本地玩家名称 <名称>", SetPlayerName);

        // Lobby
        Add("选择阵营", AiPhase.Lobby, 1, 1, "选择阵营 <0 进攻|1 防守|2 观战>", SelectCamp);
        Add("准备", AiPhase.Lobby, 1, 1, "准备 <0|1>", SetReady);
        Add("离开房间", AiPhase.Lobby, 0, 0, "离开房间", LeaveRoom);

        // Battle
        Add("站立", AiPhase.Battle, 0, 0, "站立", Stand);
        Add("移动到位置", AiPhase.Battle, 2, 2, "移动到位置 <x> <z>", MoveTo);
        Add("攻击珊瑚", AiPhase.Battle, 0, 0, "攻击珊瑚", AttackCoral);
        Add("释放指定技能", AiPhase.Battle, 1, 1, "释放指定技能 <skillId>", CastSkill);
        Add("计算路径长度", AiPhase.Battle, 2, 2, "计算路径长度 <x> <z>", PathLength);
        Add("激活lua状态机", AiPhase.Battle, 0, 0, "激活lua状态机", ActivateLua);
        Add("停止lua状态机", AiPhase.Battle, 0, 0, "停止lua状态机", StopLua);
        Add("获取小地图实体", AiPhase.Battle, 0, 0, "获取小地图实体", MinimapEntities);
        Add("获取技能列表", AiPhase.Battle, 0, 0, "获取技能列表", SkillList);
        Add("设置技能栏", AiPhase.Battle, 0, -1, "设置技能栏 <skillId…>", SetSkillBar);
        Add("获取自身状态", AiPhase.Battle, 0, 0, "获取自身状态", SelfStatus);
        Add("获取守护点进度", AiPhase.Battle, 0, 0, "获取守护点进度", GuardProgress);
        Add("设置相机渲染", AiPhase.Battle, 1, 1, "设置相机渲染 <0 关|1 开>", SetCameraRender);
        Add("截屏", AiPhase.Battle, 0, 0, "截屏", Screenshot);
    }

    private void Add(string name, AiPhase phase, int minArgs, int maxArgs, string usage,
        Action<string[], Action<string>> handler)
    {
        commands.Add(new Command
        {
            name = name,
            phase = phase,
            minArgs = minArgs,
            maxArgs = maxArgs,
            usage = usage,
            handler = handler,
        });
    }

    private Command Find(string name)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            if (commands[i].name == name) return commands[i];
        }
        return null;
    }
    #endregion

    #region 通用命令
    private void SendChat(string[] args, Action<string> reply)
    {
        if (!int.TryParse(args[0], out int channel) || (channel != 0 && channel != 1))
        {
            reply("错误：频道只能是 0（全频道）或 1（阵营）");
            return;
        }
        if (!NetworkManager.CanSendWorldCommand)
        {
            reply("错误：未联网");
            return;
        }

        string content = Join(args, 1);
        Tool.NetworkManager.SendChat(new CSChat { channel = channel, content = content });

        // 自己的消息服务器不回送，本地补一条，保证「收发都要」
        chatLog.Add(new ChatRecord
        {
            kind = channel == 0 ? "全局" : "阵营",
            camp = SelfCampName(),
            name = ClientSelection.playerName,
            content = content,
        });
        TrimChatLog();
        reply("已发送");
    }

    private void ReadChat(string[] args, Action<string> reply)
    {
        if (!int.TryParse(args[0], out int count) || count <= 0)
        {
            reply("错误：数量必须是正整数");
            return;
        }
        if (chatLog.Count == 0)
        {
            reply("（无聊天记录）");
            return;
        }

        int from = Mathf.Max(0, chatLog.Count - count);
        var builder = new StringBuilder();
        for (int i = from; i < chatLog.Count; i++)
        {
            if (builder.Length > 0) builder.Append('\n');
            var r = chatLog[i];
            builder.Append('[').Append(r.kind).Append(']');
            if (r.kind != "系统") builder.Append('[').Append(r.camp).Append(']').Append(r.name).Append(": ");
            builder.Append(r.content);
        }
        reply(builder.ToString());
    }

    private void GetPlayerName(string[] args, Action<string> reply)
    {
        reply(string.IsNullOrEmpty(ClientSelection.playerName) ? "名称=（空）" : "名称=" + ClientSelection.playerName);
    }

    private void SetOptionalInterrupts(string[] args, Action<string> reply)
    {
        if (args.Length == 1 && args[0] == "all")
        {
            hub.SetAllOptional(true);
            reply(hub.DescribeEnabled());
            return;
        }
        if (args.Length == 1 && args[0] == "none")
        {
            hub.SetAllOptional(false);
            reply(hub.DescribeEnabled());
            return;
        }

        for (int i = 0; i < args.Length; i++)
        {
            if (!Enum.TryParse(args[i], out AiInterruptKind kind) || !AiInterruptHub.IsOptional(kind))
            {
                reply($"错误：{args[i]} 不是可选中断类型");
                return;
            }
            hub.SetEnabled(kind, true);
        }
        reply(hub.DescribeEnabled());
    }

    private void ListCommands(string[] args, Action<string> reply)
    {
        AiPhase phase = CurrentPhase;
        var builder = new StringBuilder();
        for (int i = 0; i < commands.Count; i++)
        {
            var cmd = commands[i];
            if ((cmd.phase & phase) == 0) continue;
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(cmd.usage);
        }

        var lua = AiLuaInterface.Current;
        if (lua != null && lua.CommandNames.Count > 0)
        {
            builder.Append("\n--- lua 拓展（用 执行lua拓展内容 调用）---");
            for (int i = 0; i < lua.CommandNames.Count; i++)
            {
                builder.Append('\n').Append(lua.CommandNames[i]);
            }
        }
        reply(builder.ToString());
    }

    private void RunLuaExtension(string[] args, Action<string> reply)
    {
        var lua = AiLuaInterface.Current;
        if (lua == null)
        {
            reply("错误：Lua 接口未就绪");
            return;
        }
        reply(lua.RunExtension(args[0], Join(args, 1)));
    }

    private void HandleWaitInterrupt(List<string> tokens, Action<string> onResult)
    {
        if (tokens.Count > 2)
        {
            onResult("错误：用法 等待中断 [秒]");
            return;
        }
        if (hub.HasWaiting)
        {
            onResult("错误：已有等待中断挂起");
            return;
        }
        if (hub.TryDrain(out string pending))
        {
            onResult(pending);
            return;
        }

        float timeout = 0f;
        if (tokens.Count == 2 && (!float.TryParse(tokens[1], out timeout) || timeout <= 0f))
        {
            onResult("错误：定时必须是正数秒");
            return;
        }

        // 中断到达或定时触发都会走这个包装，先撤掉定时器再回结果
        Action<string> wrapper = text =>
        {
            if (waitTimer != null)
            {
                waitTimer.Cancel();
                waitTimer = null;
            }
            onResult(text);
        };

        hub.BeginWait(wrapper);
        if (timeout <= 0f || !hub.HasWaiting) return;

        float seconds = timeout;
        waitTimer = Timer.AddTimer(this, _ =>
        {
            waitTimer = null;
            hub.Push(AiInterruptKind.Timeout, "等待超过 " + seconds + "s");
        }, seconds, 1, false);
    }
    #endregion

    #region Home / Lobby 命令
    private void SelectCharacter(string[] args, Action<string> reply)
    {
        if (!int.TryParse(args[0], out int attack) || attack < 0 || attack >= Config.attack_character_count)
        {
            reply($"错误：进攻下标范围 0~{Config.attack_character_count - 1}");
            return;
        }
        if (!int.TryParse(args[1], out int defense) || defense < 0 || defense >= Config.defense_character_count)
        {
            reply($"错误：防守下标范围 0~{Config.defense_character_count - 1}");
            return;
        }
        ClientSelection.selectedAttackIndex = attack;
        ClientSelection.selectedDefenseIndex = defense;
        reply($"已选择 进攻={attack} 防守={defense}");
    }

    private void Connect(string[] args, Action<string> reply)
    {
        if (connectTask != null)
        {
            reply("错误：上一次连接还没结束");
            return;
        }
        connectIp = args.Length == 1 ? args[0] : string.Empty;
        connectReply = reply;
        connectTask = Tool.NetworkManager.TryConnect(connectIp);
        reply("已发起连接：" + (connectIp.Length == 0 ? "本机" : connectIp));
    }

    private void TickConnect()
    {
        if (connectTask == null || !connectTask.IsCompleted) return;

        var task = connectTask;
        var reply = connectReply;
        connectTask = null;
        connectReply = null;

        if (task.IsFaulted)
        {
            string message = task.Exception != null ? task.Exception.GetBaseException().Message : "未知错误";
            Debug.LogError("[AI] 连接异常：" + message);
            reply("连接失败：" + message);
            return;
        }
        if (task.Result != NetworkManager.ConnectResult.Success)
        {
            reply("连接失败：" + task.Result);
            return;
        }

        Tool.NetworkManager.EnterWorld();
        reply("已连接，正在进入房间");
    }

    private void SetPlayerName(string[] args, Action<string> reply)
    {
        string name = Join(args, 0);
        ClientSelection.playerName = name;
        reply("名称已设为 " + name);
    }

    // 阵营与准备同包上报，本地记住每次准备状态，改阵营时不能把准备悄悄清掉
    private void SelectCamp(string[] args, Action<string> reply)
    {
        if (!int.TryParse(args[0], out int camp) || camp < 0 || camp > 2)
        {
            reply("错误：阵营只能是 0 进攻 / 1 防守 / 2 观战");
            return;
        }
        Tool.NetworkManager.SendLobbyUpdate(new CSLobbyUpdate { camp = camp, ready = lastReady });
        reply("已选择阵营 " + camp);
    }

    private void SetReady(string[] args, Action<string> reply)
    {
        if (!int.TryParse(args[0], out int ready) || (ready != 0 && ready != 1))
        {
            reply("错误：准备只能是 0 或 1");
            return;
        }
        lastReady = ready == 1;
        Tool.NetworkManager.SendLobbyUpdate(new CSLobbyUpdate { camp = -1, ready = lastReady });
        reply(lastReady ? "已准备" : "已取消准备");
    }

    private void LeaveRoom(string[] args, Action<string> reply)
    {
        lastReady = false;
        Tool.NetworkManager.ExitWorld();
        reply("已离开房间");
    }
    #endregion

    #region Battle 命令
    private void Stand(string[] args, Action<string> reply) => SendIntent(AiIntent.Stand, Vector2.zero, 0, reply);

    private void MoveTo(string[] args, Action<string> reply)
    {
        if (!float.TryParse(args[0], out float x) || !float.TryParse(args[1], out float z))
        {
            reply("错误：坐标必须是数字");
            return;
        }
        SendIntent(AiIntent.MoveTo, new Vector2(x, z), 0, reply);
    }

    private void AttackCoral(string[] args, Action<string> reply)
    {
        if (!TryFindNearestCoral(out ushort coralId))
        {
            reply("错误：小地图上没有珊瑚");
            return;
        }
        SendIntent(AiIntent.AttackTarget, Vector2.zero, coralId, reply);
    }

    private void CastSkill(string[] args, Action<string> reply)
    {
        if (!int.TryParse(args[0], out int skillId) || skillId < 0)
        {
            reply("错误：技能 id 必须是非负整数");
            return;
        }
        if (!SkillManager.TryGet(skillId, out _))
        {
            reply($"错误：技能 {skillId} 未注册");
            return;
        }
        if (!NetworkManager.CanSendWorldCommand)
        {
            reply("错误：未联网");
            return;
        }
        Tool.NetworkManager.SendAiSkillCast(skillId);
        reply("已释放技能 " + skillId);
    }

    private void PathLength(string[] args, Action<string> reply)
    {
        if (!float.TryParse(args[0], out float x) || !float.TryParse(args[1], out float z))
        {
            reply("错误：坐标必须是数字");
            return;
        }
        if (!TryGetSelfPos(out Vector3 from))
        {
            reply("错误：本地玩家实体未就绪");
            return;
        }

        Vector3 to = new Vector3(x, from.y, z);
        if (!NavMesh.SamplePosition(from, out var startHit, 2f, NavMesh.AllAreas))
        {
            reply("可达=否 原因=起点不在导航面上");
            return;
        }
        if (!NavMesh.SamplePosition(to, out var endHit, 2f, NavMesh.AllAreas))
        {
            reply("可达=否 原因=终点不在导航面上");
            return;
        }

        var path = new NavMeshPath();
        bool ok = NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path);
        if (!ok || path.corners.Length < 2)
        {
            reply("可达=否 原因=没有完整路径");
            return;
        }

        float length = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        reply($"可达=是 长度={length:0.##} 拐点={path.corners.Length}");
    }

    private void ActivateLua(string[] args, Action<string> reply)
    {
        var lua = AiLuaInterface.Current;
        if (lua == null)
        {
            reply("错误：Lua 接口未就绪");
            return;
        }
        reply(lua.ActivateStateMachine());
    }

    private void StopLua(string[] args, Action<string> reply)
    {
        var lua = AiLuaInterface.Current;
        if (lua == null)
        {
            reply("错误：Lua 接口未就绪");
            return;
        }
        reply(lua.StopStateMachine());
    }

    private void MinimapEntities(string[] args, Action<string> reply)
    {
        if (minimap.Count == 0)
        {
            reply("（小地图上没有实体）");
            return;
        }

        var ids = new List<ushort>(minimap.Keys);
        ids.Sort();
        var builder = new StringBuilder();
        for (int i = 0; i < ids.Count; i++)
        {
            var e = minimap[ids[i]];
            if (builder.Length > 0) builder.Append('\n');
            builder.Append("id=").Append(e.entityId)
                .Append(" 类型=").Append(e.type.category).Append(':').Append(e.type.value)
                .Append(" 阵营=").Append(e.camp)
                .Append(" 坐标=(").Append(e.posX.ToString("0.##")).Append(',').Append(e.posZ.ToString("0.##")).Append(')')
                .Append(" 标记=").Append(e.marked ? 1 : 0);
        }
        reply(builder.ToString());
    }

    private void SkillList(string[] args, Action<string> reply)
    {
        var skills = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.SkillVfx : null;
        if (skills == null)
        {
            reply("错误：技能管理器未就绪");
            return;
        }

        var builder = new StringBuilder();
        builder.Append("槽位=").Append(skills.SlotCapacity);
        var active = skills.ActiveSkills;
        for (int i = 0; i < active.Count; i++)
        {
            int skillId = active[i];
            builder.Append('\n').Append("槽位").Append(i)
                .Append(": id=").Append(skillId)
                .Append(" 进度=").Append(skills.Recharge.GetCycleProgress(skillId).ToString("0.##"))
                .Append(" 库存=").Append(skills.Recharge.GetStore(skillId));
        }

        builder.Append("\n槽位外持有:");
        bool any = false;
        var held = skills.HeldSkills;
        for (int i = 0; i < held.Count; i++)
        {
            if (ContainsSkill(active, held[i])) continue;
            builder.Append(' ').Append(held[i]);
            any = true;
        }
        if (!any) builder.Append(" 无");
        reply(builder.ToString());
    }

    private static bool ContainsSkill(IReadOnlyList<int> ids, int skillId)
    {
        for (int i = 0; i < ids.Count; i++)
        {
            if (ids[i] == skillId) return true;
        }
        return false;
    }

    private void SetSkillBar(string[] args, Action<string> reply)
    {
        var skills = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.SkillVfx : null;
        if (skills == null)
        {
            reply("错误：技能管理器未就绪");
            return;
        }

        var ids = new List<int>();
        for (int i = 0; i < args.Length; i++)
        {
            if (!int.TryParse(args[i], out int skillId) || skillId < 0)
            {
                reply("错误：技能 id 必须是非负整数");
                return;
            }
            if (ids.Contains(skillId))
            {
                reply("错误：技能 id 重复 " + skillId);
                return;
            }
            ids.Add(skillId);
        }
        if (!skills.TrySetActiveSkills(ids))
        {
            reply("错误：技能栏提交被拒（未持有或超过槽位数）");
            return;
        }
        reply("已提交技能栏，等服务器回推快照");
    }

    private void SelfStatus(string[] args, Action<string> reply)
    {
        var loading = ClientLoadingManager.Current;
        if (loading == null || !loading.HasSession)
        {
            reply("错误：没有战斗会话");
            return;
        }
        if (!TryGetSelfPos(out Vector3 pos))
        {
            reply("错误：本地玩家实体未就绪");
            return;
        }

        var builder = new StringBuilder();
        builder.Append("实体=").Append(loading.OwnEntityId)
            .Append(" 阵营=").Append(loading.Camp)
            .Append(" 血量=").Append(selfHealth).Append('/').Append(selfMaxHealth)
            .Append(" 坐标=(").Append(pos.x.ToString("0.##")).Append(',').Append(pos.z.ToString("0.##")).Append(')')
            .Append(" 朝向=").Append(SelfYaw().ToString("0.#"));

        builder.Append(" buff=");
        if (selfBuffs.Count == 0) builder.Append("无");
        for (int i = 0; i < selfBuffs.Count; i++)
        {
            if (i > 0) builder.Append(',');
            builder.Append(selfBuffs[i]);
        }
        reply(builder.ToString());
    }

    private void GuardProgress(string[] args, Action<string> reply)
    {
        if (guardProgress == null)
        {
            reply("错误：尚未收到守护点数据");
            return;
        }
        reply($"主={guardProgress.main} 副1={guardProgress.sub1} 副2={guardProgress.sub2} 副3={guardProgress.sub3}（0~100 被打掉的百分比）");
    }

    private void SetCameraRender(string[] args, Action<string> reply)
    {
        if (!int.TryParse(args[0], out int on) || (on != 0 && on != 1))
        {
            reply("错误：只能是 0 或 1");
            return;
        }
        var cam = Tool.CameraController != null ? Tool.CameraController.WorldCamera : null;
        if (cam == null)
        {
            reply("错误：相机未就绪");
            return;
        }
        cam.enabled = on == 1;
        reply(on == 1 ? "相机渲染已开启" : "相机渲染已关闭（截屏会自动开启一帧）");
    }

    private void Screenshot(string[] args, Action<string> reply)
    {
        var cam = Tool.CameraController != null ? Tool.CameraController.WorldCamera : null;
        if (cam == null)
        {
            reply("错误：相机未就绪");
            return;
        }
        if (!TryGetSelfPos(out Vector3 pos))
        {
            reply("错误：本地玩家实体未就绪");
            return;
        }

        string dir = Path.Combine(Application.dataPath, "..", ScreenshotFolder);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, "ai_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png");

        bool restore = !cam.enabled;
        if (restore) cam.enabled = true;
        ScreenCapture.CaptureScreenshot(file);
        if (restore) Timer.AddTimer(this, _ => RestoreCamera(cam), 0.2f, 1, false);

        reply($"文件={Path.GetFullPath(file)} 坐标=({pos.x:0.##},{pos.z:0.##}) 朝向={SelfYaw():0.#}");
    }

    private static void RestoreCamera(Camera cam)
    {
        if (cam != null) cam.enabled = false;
    }
    #endregion

    #region 意图
    public static bool TrySendIntent(AiIntent intent, Vector2 position, ushort targetId, out string error)
    {
        if (IntentSender == null)
        {
            error = "意图通道未接入（等待意图上行协议）";
            return false;
        }
        return IntentSender(intent, position, targetId, out error);
    }

    private void SendIntent(AiIntent intent, Vector2 position, ushort targetId, Action<string> reply)
    {
        if (!TrySendIntent(intent, position, targetId, out string error))
        {
            reply("错误：" + error);
            return;
        }
        reply("已下发 " + intent);
    }
    #endregion

    #region 事件
    private void OnChatMessage(SCChat chat)
    {
        chatLog.Add(new ChatRecord
        {
            kind = chat.channel == 0 ? "全局" : "阵营",
            camp = ((EntityCamp)chat.camp).ToString(),
            name = chat.name,
            content = chat.content,
        });
        TrimChatLog();

        string self = ClientSelection.playerName;
        if (string.IsNullOrEmpty(self)) return;
        if (chat.content == null || !chat.content.Contains("@" + self)) return;
        hub.Push(AiInterruptKind.ChatMention, "发送者=" + chat.name, "频道=" + chat.channel, "内容=" + chat.content);
    }

    private void OnBattleEvent(SCBattleEvent e)
    {
        if (e == null) return;

        switch (e.type)
        {
            case SCBattleEvent.Type.Nightfall:
                hub.Push(AiInterruptKind.DayNight, "夜晚", "进度=" + EnvironmentManager.Time01.ToString("0.##"));
                break;
            case SCBattleEvent.Type.Daybreak:
                hub.Push(AiInterruptKind.DayNight, "白天", "进度=" + EnvironmentManager.Time01.ToString("0.##"));
                break;
            case SCBattleEvent.Type.NeutralStatueRespawn:
                hub.Push(AiInterruptKind.NeutralSpawn, "中立单位已刷新");
                break;
            case SCBattleEvent.Type.SkillBroadcast:
                hub.Push(AiInterruptKind.SkillBroadcast, "技能=" + e.textId);
                break;
        }

        string text = ResolveEventText(e);
        if (text == null) return;
        chatLog.Add(new ChatRecord { kind = "系统", camp = string.Empty, name = string.Empty, content = text });
        TrimChatLog();
    }

    private void OnPrompt(int messageId)
    {
        chatLog.Add(new ChatRecord
        {
            kind = "系统",
            camp = string.Empty,
            name = string.Empty,
            content = ResolveText(messageId),
        });
        TrimChatLog();
        hub.Push(AiInterruptKind.Prompt, "id=" + messageId, ResolveText(messageId));
    }

    private void OnMinimapUpdate(SCMinimapEntity e)
    {
        if (e == null) return;
        minimap[e.entityId] = e;
        minimapSeen[e.entityId] = Time.time;
    }

    private void OnEntityDisplay(SCEntityDisplayInfo info)
    {
        var loading = ClientLoadingManager.Current;
        if (loading == null || info.entityId != loading.OwnEntityId) return;

        if (info.hasHealth)
        {
            selfHealth = info.health;
            selfMaxHealth = info.maxHealth;
        }
        if (!info.hasBuffs) return;

        selfBuffs.Clear();
        for (int i = 0; i < info.buffs.Count; i++)
        {
            selfBuffs.Add(info.buffs[i].type + ":" + info.buffs[i].level);
        }
    }

    private void OnGuardProgress(SCDefensePointProgress info) => guardProgress = info;

    // 他人信号：同阵营快捷消息（8 种），发送者名字按客户端名表解析
    private void OnQuickMessage(SCQuickMessage message)
    {
        if (message == null) return;

        var loading = ClientLoadingManager.Current;
        var fields = new List<string>
        {
            "类型=" + ((QuickMessageType)message.messageId).ToString(),
            "发送者=" + (loading != null ? loading.GetName(message.senderClientId) : "#" + message.senderClientId),
        };
        if (message.hasTargetPosition)
        {
            fields.Add($"坐标=({message.targetPosition.x:0.#},{message.targetPosition.z:0.#})");
        }
        hub.Push(AiInterruptKind.OtherSignal, fields.ToArray());
    }

    private void OnBattleStart(int camp) => hub.Push(AiInterruptKind.BattleStart, "阵营=" + (EntityCamp)camp);

    private void OnBattleEnd(int gameState) => hub.Push(AiInterruptKind.BattleEnd, "结算=" + gameState);

    // 只为去重：持有表增长才报「获得技能」，选栏与快照不报
    private void OnSkillBarRefresh()
    {
        var skills = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.SkillVfx : null;
        if (skills == null) return;

        var held = skills.HeldSkills;
        var added = new List<int>();
        for (int i = 0; i < held.Count; i++)
        {
            if (heldSnapshot.Contains(held[i])) continue;
            if (heldSnapshot.Count > 0) added.Add(held[i]);
        }

        heldSnapshot.Clear();
        for (int i = 0; i < held.Count; i++) heldSnapshot.Add(held[i]);
        if (added.Count == 0) return;

        var builder = new StringBuilder("新增=");
        for (int i = 0; i < added.Count; i++)
        {
            if (i > 0) builder.Append(',');
            builder.Append(added[i]);
        }
        hub.Push(AiInterruptKind.SkillGained, builder.ToString());
    }
    #endregion

    #region 工具
    private void TickMinimapExpire()
    {
        if (minimapSeen.Count == 0) return;

        minimapExpired.Clear();
        foreach (var pair in minimapSeen)
        {
            if (Time.time - pair.Value <= Config.minimap_entity_timeout) continue;
            minimapExpired.Add(pair.Key);
        }
        for (int i = 0; i < minimapExpired.Count; i++)
        {
            ushort id = minimapExpired[i];
            minimapSeen.Remove(id);
            minimap.Remove(id);
        }
    }

    // 遇敌：小地图上出现新的敌对单位；去抖 = 0.5s 检查间隔 + 中断列表同类型合并，离开视野再出现会重新报
    private const float EnemyCheckInterval = 0.5f;
    private float nextEnemyCheck;
    private readonly HashSet<ushort> knownEnemies = new();

    private void TickEnemySpot()
    {
        if (CurrentPhase != AiPhase.Battle)
        {
            knownEnemies.Clear();
            return;
        }
        if (Time.time < nextEnemyCheck) return;
        nextEnemyCheck = Time.time + EnemyCheckInterval;

        var loading = ClientLoadingManager.Current;
        if (loading == null || !loading.HasSession || loading.Camp == EntityCamp.None)
        {
            knownEnemies.Clear();
            return;
        }

        var added = new List<string>();
        var seen = new HashSet<ushort>();
        bool hasSelf = TryGetSelfPos(out Vector3 self);
        foreach (var pair in minimap)
        {
            var e = pair.Value;
            seen.Add(e.entityId);
            if (e.camp == loading.Camp || e.camp == EntityCamp.Neutral) continue;
            if (knownEnemies.Contains(e.entityId)) continue;

            if (hasSelf)
            {
                float dx = e.posX - self.x;
                float dz = e.posZ - self.z;
                added.Add($"id={e.entityId} 类型={e.type.category}:{e.type.value} 距离={Mathf.Sqrt(dx * dx + dz * dz):0.#}");
            }
            else
            {
                added.Add($"id={e.entityId} 类型={e.type.category}:{e.type.value}");
            }
        }
        knownEnemies.IntersectWith(seen);
        if (added.Count == 0) return;
        hub.Push(AiInterruptKind.EnemySpotted, added.ToArray());
    }

    private static List<string> SplitArgs(string line)
    {
        var tokens = new List<string>();
        var builder = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                quoted = !quoted;
                continue;
            }
            if (!quoted && (c == ' ' || c == '\t'))
            {
                if (builder.Length > 0)
                {
                    tokens.Add(builder.ToString());
                    builder.Clear();
                }
                continue;
            }
            builder.Append(c);
        }
        if (builder.Length > 0) tokens.Add(builder.ToString());
        if (tokens.Count == 0) tokens.Add(string.Empty);
        return tokens;
    }

    private static string Join(string[] args, int from)
    {
        if (from >= args.Length) return string.Empty;
        var builder = new StringBuilder(args[from]);
        for (int i = from + 1; i < args.Length; i++) builder.Append(' ').Append(args[i]);
        return builder.ToString();
    }

    private void TrimChatLog()
    {
        while (chatLog.Count > ChatLogCapacity) chatLog.RemoveAt(0);
    }

    private static string SelfCampName()
    {
        var loading = ClientLoadingManager.Current;
        return loading == null ? "未定" : loading.Camp.ToString();
    }

    public bool TryGetSelfPos(out Vector3 pos)
    {
        pos = default;
        var loading = ClientLoadingManager.Current;
        if (loading == null || !loading.HasSession) return false;
        if (Tool.ClientLogicManager == null) return false;
        return Tool.ClientLogicManager.EntityPlayers.TryGetEntityPosition(loading.OwnEntityId, out pos);
    }

    public float SelfYaw()
    {
        var loading = ClientLoadingManager.Current;
        if (loading == null || !loading.HasSession) return 0f;
        if (Tool.ClientLogicManager == null) return 0f;
        var view = Tool.ClientLogicManager.EntityPlayers.GetView(loading.OwnEntityId);
        if (view == null) return 0f;
        return view.transform.eulerAngles.y;
    }

    private bool TryFindNearestCoral(out ushort coralId)
    {
        coralId = 0;
        if (!TryGetSelfPos(out Vector3 self)) return false;

        float best = float.MaxValue;
        bool found = false;
        foreach (var pair in minimap)
        {
            var e = pair.Value;
            if (e.type.category != EntityCategory.Coral) continue;
            float dx = e.posX - self.x;
            float dz = e.posZ - self.z;
            float sqr = dx * dx + dz * dz;
            if (sqr >= best) continue;
            best = sqr;
            coralId = e.entityId;
            found = true;
        }
        return found;
    }

    private static string ResolveEventText(SCBattleEvent e)
    {
        switch (e.type)
        {
            case SCBattleEvent.Type.PlayerKill:
                return ResolveText(e.textId) + " 击败 " + ResolveText(e.textId2);
            case SCBattleEvent.Type.DeathUnattributed:
                return ResolveText(e.textId) + " 死亡";
            case SCBattleEvent.Type.PlayerRespawn:
                return ResolveText(e.textId) + " 复活";
            case SCBattleEvent.Type.NeutralStatueCaptured:
                return "中立雕像被击败";
            case SCBattleEvent.Type.NeutralStatueRespawn:
                return "中立雕像刷新";
            case SCBattleEvent.Type.Nightfall:
                return "天黑";
            case SCBattleEvent.Type.Daybreak:
                return "天亮";
            case SCBattleEvent.Type.StatueDestroyed:
                return "雕像被摧毁";
            case SCBattleEvent.Type.CrystalStatueDestroyed:
                return "水晶雕像被摧毁";
            case SCBattleEvent.Type.Notice:
                return ResolveText(e.textId);
            default:
                return null;
        }
    }

    private static string ResolveText(int id)
    {
        if (id >= SCBattleEvent.PlayerNameBase)
        {
            int clientId = id - SCBattleEvent.PlayerNameBase;
            var loading = ClientLoadingManager.Current;
            string name = loading != null ? loading.GetName(clientId) : null;
            return string.IsNullOrEmpty(name) ? "#" + clientId : name;
        }
        if (!ConfigManager.NoticeConfig.content.ContainsKey(id))
        {
            Debug.LogError($"[AI] notice.csv 缺少文本 id={id}");
            return "#" + id;
        }
        return ConfigManager.NoticeConfig.Get(id).text;
    }
    #endregion
}
