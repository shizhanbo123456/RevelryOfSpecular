// AI 操控的 Lua 接口：脚本加载与重载、状态机驱动、给 Lua 的读写 API（LuaEnv 本体在 LuaManager，此处只借用）
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ros.Transport;
using UnityEngine;
using XLua;

public sealed class AiLuaInterface : ClientSubManager
{
    public static AiLuaInterface Current { get; private set; }

    private const string ScriptFolder = "AiInterface/Lua";
    private const string StateMachineScript = "state_machine.lua";
    private const string ExtensionScript = "extensions.lua";

    private LuaEnv env;          // 当前 LuaEnv 快照，LuaManager 重建后自动切换到新实例
    private bool loaderAdded;
    private bool missingLogged;
    private LuaFunction onTick;
    private bool active;
    private bool extensionsLoaded;
    private readonly object[] tickArgs = new object[1];

    private readonly AiLuaApi api;
    private readonly Dictionary<string, LuaFunction> extensions = new();
    private readonly List<string> commandNames = new();
    private readonly List<SCMinimapEntity> mapBuffer = new();

    public AiLuaInterface()
    {
        api = new AiLuaApi(this);
    }

    public bool Active => active;

    public IReadOnlyList<string> CommandNames => commandNames;

    private static string ScriptRoot => Path.Combine(Application.dataPath, "Scripts", ScriptFolder);

    public override void Init(ClientLogicManager owner)
    {
        base.Init(owner);
        Current = this;
    }

    public override void Tick(float deltaTime)
    {
        if (!EnsureEnv()) return;
        if (!active || onTick == null) return;

        tickArgs[0] = deltaTime;
        try
        {
            onTick.Call(tickArgs);
        }
        catch (Exception e)
        {
            Fail("状态机 on_tick", e);
            StopStateMachineInternal(true);
        }
    }

    protected override void UnbindEvents()
    {
        ResetScriptState();
        if (Current == this) Current = null;
    }

    // LuaEnv 由 LuaManager 持有；实例被重建（场景重载 / 退出）时旧脚本句柄全部作废重来
    private bool EnsureEnv()
    {
        var manager = Tool.LuaManager;
        if (manager == null)
        {
            if (!missingLogged)
            {
                missingLogged = true;
                Debug.LogError("[AI][Lua] 场景缺少 LuaManager，Lua 接口不可用");
            }
            return false;
        }
        if (env == manager.Env) return env != null;

        ResetScriptState();
        missingLogged = false;
        env = manager.Env;
        if (env == null) return false;

        if (!loaderAdded)
        {
            env.AddLoader(Loader);
            loaderAdded = true;
        }
        env.Global.Set("ai", api);
        return true;
    }

    private void ResetScriptState()
    {
        active = false;
        extensionsLoaded = false;
        if (onTick != null)
        {
            onTick.Dispose();
            onTick = null;
        }
        foreach (var pair in extensions) pair.Value.Dispose();
        extensions.Clear();
        commandNames.Clear();
    }

    #region 状态机
    // 激活即接管：先整份重载（状态机 + 拓展），再从 on_tick 开始逐帧驱动
    public string ActivateStateMachine()
    {
        if (!EnsureEnv()) return "错误：LuaManager 未就绪";
        ResetScriptState();
        if (!LoadExtensions()) return "错误：拓展脚本加载失败（详见控制台）";
        if (!LoadStateMachine()) return "错误：状态机脚本加载失败（详见控制台）";

        active = true;
        return "状态机已激活";
    }

    public string StopStateMachine()
    {
        if (!EnsureEnv()) return "错误：LuaManager 未就绪";
        StopStateMachineInternal(true);
        return "状态机已停止（已下发站立）";
    }

    // 停决策后必须让实体站住：行为层「无请求 = 保持当前动作」，否则角色会一直跑
    private void StopStateMachineInternal(bool sendStand)
    {
        active = false;
        if (onTick != null)
        {
            onTick.Dispose();
            onTick = null;
        }
        if (!sendStand) return;

        if (!AiCommandLine.TrySendIntent(AiIntent.Stand, Vector2.zero, 0, out string error))
        {
            Debug.LogError("[AI][Lua] 停止状态机后站立下发失败：" + error);
        }
    }

    private bool LoadStateMachine()
    {
        string text = ReadScript(StateMachineScript);
        if (text == null) return false;

        try
        {
            env.DoString(text, StateMachineScript);
        }
        catch (Exception e)
        {
            Fail(StateMachineScript, e);
            return false;
        }

        if (onTick != null) onTick.Dispose();
        onTick = env.Global.Get<LuaFunction>("on_tick");
        if (onTick != null) return true;

        Debug.LogError("[AI][Lua] 状态机脚本必须定义全局函数 on_tick(dt)");
        return false;
    }
    #endregion

    #region 拓展命令
    public void RegisterCommand(string name, LuaFunction fn)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("[AI][Lua] register_command 命令名为空");
            return;
        }
        if (fn == null)
        {
            Debug.LogError("[AI][Lua] register_command 处理函数为空：" + name);
            return;
        }

        if (extensions.TryGetValue(name, out var old))
        {
            old.Dispose();
            extensions.Remove(name);
            commandNames.Remove(name);
        }
        extensions.Add(name, fn);
        commandNames.Add(name);
    }

    public string RunExtension(string name, string argText)
    {
        if (!EnsureEnv()) return "错误：LuaManager 未就绪";
        if (!extensionsLoaded && !LoadExtensions()) return "错误：拓展脚本加载失败（详见控制台）";
        if (!extensions.TryGetValue(name, out var fn))
        {
            return "错误：没有名为 " + name + " 的拓展命令（用 获取命令列表 查看）";
        }

        object[] result;
        try
        {
            result = fn.Call(argText);
        }
        catch (Exception e)
        {
            Fail("拓展命令 " + name, e);
            return "错误：拓展命令 " + name + " 异常：" + e.Message;
        }

        if (result == null || result.Length == 0 || result[0] == null) return "（无返回）";
        return result[0].ToString();
    }

    private bool LoadExtensions()
    {
        string text = ReadScript(ExtensionScript);
        if (text == null) return false;

        try
        {
            env.DoString(text, ExtensionScript);
            extensionsLoaded = true;
            return true;
        }
        catch (Exception e)
        {
            Fail(ExtensionScript, e);
            return false;
        }
    }
    #endregion

    #region 环境
    // 重载不清 package.loaded：两个入口脚本每次全量重新执行，require 的用户模块沿用缓存
    private byte[] Loader(ref string filepath)
    {
        string path = Path.Combine(ScriptRoot, filepath.Replace('.', '/') + ".lua");
        if (!File.Exists(path)) return null;
        return File.ReadAllBytes(path);
    }

    private string ReadScript(string file)
    {
        string path = Path.Combine(ScriptRoot, file);
        if (!File.Exists(path))
        {
            Debug.LogError("[AI][Lua] 找不到脚本：" + path);
            return null;
        }
        return File.ReadAllText(path, Encoding.UTF8);
    }

    private void Fail(string chunk, Exception e)
    {
        Debug.LogError($"[AI][Lua] {chunk} 执行失败：{e.Message}");

        var commands = AiCommandLine.Current;
        if (commands == null) return;
        commands.Interrupts.Push(AiInterruptKind.LuaError, "脚本=" + chunk, "错误=" + e.Message);
    }

    internal List<SCMinimapEntity> MapBuffer => mapBuffer;

    internal void CopyMinimap()
    {
        var commands = AiCommandLine.Current;
        if (commands == null)
        {
            mapBuffer.Clear();
            return;
        }
        commands.CopyMinimap(mapBuffer);
    }
    #endregion
}

// 暴露给 lua 的 API 表：读状态零成本可每帧调，写动作经去重后上行（攻击意图例外，它是持续出招的节拍）
public sealed class AiLuaApi
{
    private readonly AiLuaInterface owner;

    private AiIntent lastIntent;
    private Vector2 lastPoint;
    private ushort lastTarget;
    private bool hasLast;

    public AiLuaApi(AiLuaInterface owner) { this.owner = owner; }

    #region 读：会话与自身
    public string phase() => AiCommandLine.CurrentPhase.ToString();

    public bool connected() => NetworkManager.CanSendWorldCommand;

    public int self_id()
    {
        var loading = ClientLoadingManager.Current;
        return loading != null && loading.HasSession ? loading.OwnEntityId : 0;
    }

    public string self_camp()
    {
        var loading = ClientLoadingManager.Current;
        return loading != null && loading.HasSession ? loading.Camp.ToString() : "None";
    }

    public bool self_alive()
    {
        var loading = ClientLoadingManager.Current;
        return loading != null && loading.HasSession && loading.OwnEntityId != 0;
    }

    public int self_hp()
    {
        var commands = AiCommandLine.Current;
        return commands != null ? commands.SelfHealth : 0;
    }

    public int self_max_hp()
    {
        var commands = AiCommandLine.Current;
        return commands != null ? commands.SelfMaxHealth : 0;
    }

    public float self_x()
    {
        var commands = AiCommandLine.Current;
        return commands != null && commands.TryGetSelfPos(out Vector3 pos) ? pos.x : 0f;
    }

    public float self_z()
    {
        var commands = AiCommandLine.Current;
        return commands != null && commands.TryGetSelfPos(out Vector3 pos) ? pos.z : 0f;
    }

    public float self_yaw()
    {
        var commands = AiCommandLine.Current;
        return commands != null ? commands.SelfYaw() : 0f;
    }

    public int buff_count()
    {
        var commands = AiCommandLine.Current;
        return commands != null ? commands.SelfBuffs.Count : 0;
    }

    public string buff(int index)
    {
        var commands = AiCommandLine.Current;
        if (commands == null || index < 0 || index >= commands.SelfBuffs.Count) return "";
        return commands.SelfBuffs[index];
    }
    #endregion

    #region 读：小地图
    public int map_count()
    {
        owner.CopyMinimap();
        return owner.MapBuffer.Count;
    }

    public int map_id(int index)
    {
        var list = owner.MapBuffer;
        return index >= 0 && index < list.Count ? list[index].entityId : 0;
    }

    public int map_cat(int index)
    {
        var list = owner.MapBuffer;
        return index >= 0 && index < list.Count ? (int)list[index].type.category : -1;
    }

    public int map_val(int index)
    {
        var list = owner.MapBuffer;
        return index >= 0 && index < list.Count ? list[index].type.value : -1;
    }

    public int map_camp(int index)
    {
        var list = owner.MapBuffer;
        return index >= 0 && index < list.Count ? (int)list[index].camp : 0;
    }

    public float map_x(int index)
    {
        var list = owner.MapBuffer;
        return index >= 0 && index < list.Count ? list[index].posX : 0f;
    }

    public float map_z(int index)
    {
        var list = owner.MapBuffer;
        return index >= 0 && index < list.Count ? list[index].posZ : 0f;
    }

    public bool map_marked(int index)
    {
        var list = owner.MapBuffer;
        return index >= 0 && index < list.Count && list[index].marked;
    }
    #endregion

    #region 读：技能
    public int slot_count()
    {
        var skills = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.SkillVfx : null;
        return skills != null ? skills.ActiveSkills.Count : 0;
    }

    public int slot_skill(int slot)
    {
        var skills = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.SkillVfx : null;
        return skills != null ? skills.GetSlotSkillId(slot) : -1;
    }

    public float slot_progress(int slot)
    {
        var skills = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.SkillVfx : null;
        if (skills == null) return 0f;
        int skillId = skills.GetSlotSkillId(slot);
        return skillId < 0 ? 0f : skills.Recharge.GetCycleProgress(skillId);
    }

    public int slot_store(int slot)
    {
        var skills = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.SkillVfx : null;
        if (skills == null) return 0;
        int skillId = skills.GetSlotSkillId(slot);
        return skillId < 0 ? 0 : skills.Recharge.GetStore(skillId);
    }

    public int held_count()
    {
        var skills = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.SkillVfx : null;
        return skills != null ? skills.HeldSkills.Count : 0;
    }

    public int held_skill(int index)
    {
        var skills = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.SkillVfx : null;
        if (skills == null || index < 0 || index >= skills.HeldSkills.Count) return -1;
        return skills.HeldSkills[index];
    }
    #endregion

    #region 读：守护点与昼夜
    public int guard_progress(int index)
    {
        var commands = AiCommandLine.Current;
        if (commands == null || commands.GuardSnapshot == null) return -1;
        var info = commands.GuardSnapshot;
        switch (index)
        {
            case 0: return info.main;
            case 1: return info.sub1;
            case 2: return info.sub2;
            case 3: return info.sub3;
            default: return -1;
        }
    }

    public bool is_night() => !EnvironmentManager.IsDay;

    public float day_progress() => EnvironmentManager.Time01;
    #endregion

    #region 写：意图与动作
    public bool stand() => Send(AiIntent.Stand, Vector2.zero, 0);

    public bool stop() => Send(AiIntent.Stop, Vector2.zero, 0);

    public bool move_to(float x, float z) => Send(AiIntent.MoveTo, new Vector2(x, z), 0);

    public bool attack(int entityId)
    {
        if (entityId <= 0) return false;
        return Send(AiIntent.AttackTarget, Vector2.zero, (ushort)entityId);
    }

    public bool cast_skill(int skillId)
    {
        if (skillId < 0 || !NetworkManager.CanSendWorldCommand) return false;
        Tool.NetworkManager.SendAiSkillCast(skillId); // AI 专用通道，不受键盘拦截
        return true;
    }

    public bool signal(int messageId)
    {
        if (!NetworkManager.CanSendWorldCommand) return false;
        Tool.NetworkManager.SendQuickMessage(new CSQuickMessage { messageId = messageId, hasTargetPosition = false });
        return true;
    }

    public bool signal_at(int messageId, float x, float z)
    {
        if (!NetworkManager.CanSendWorldCommand) return false;
        Tool.NetworkManager.SendQuickMessage(new CSQuickMessage
        {
            messageId = messageId,
            hasTargetPosition = true,
            targetPosition = new Vector3(x, 0f, z),
        });
        return true;
    }

    public void interrupt(string text)
    {
        var commands = AiCommandLine.Current;
        if (commands == null) return;
        commands.Interrupts.Push(AiInterruptKind.LuaTrigger, string.IsNullOrEmpty(text) ? "lua 触发" : text);
    }

    // 移动/站立同值不重发；攻击每次都发 —— 行为层靠重复请求维持「持续出招」的节拍
    private bool Send(AiIntent intent, Vector2 position, ushort targetId)
    {
        bool same = hasLast && intent == lastIntent && targetId == lastTarget
            && (intent != AiIntent.MoveTo || (position - lastPoint).sqrMagnitude < 0.01f);
        if (same) return true;

        if (!AiCommandLine.TrySendIntent(intent, position, targetId, out string error))
        {
            Debug.LogError("[AI][Lua] 意图下发失败：" + error);
            return false;
        }

        lastIntent = intent;
        lastPoint = position;
        lastTarget = targetId;
        hasLast = true;
        return true;
    }
    #endregion

    #region 写：拓展命令注册
    public void register_command(string name, LuaFunction fn)
    {
        owner.RegisterCommand(name, fn);
    }

    public int command_count() => owner.CommandNames.Count;

    public string command_name(int index)
    {
        return index >= 0 && index < owner.CommandNames.Count ? owner.CommandNames[index] : "";
    }
    #endregion
}
