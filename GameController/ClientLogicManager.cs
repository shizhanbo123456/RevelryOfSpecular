using Ros.Transport;
using System.Collections.Generic;
using UnityEngine;

public enum TextColor
{
    White, Orange, Red, Green, Blue
}

public class ClientLogicManager : MonoBehaviour
{
    private bool inWorld;

    private readonly EntityPlayerManager entityPlayerManager = new();
    private readonly LabelPlayerManager labelPlayerManager = new();
    private readonly ClientInteractManager clientInteractManager = new();
    private readonly SettlementManager settlementManager = new();
    private readonly ClientStaminaManager staminaManager = new();
    private readonly ClientSkillManager skillManager = new();
    private readonly ClientBattleTimeManager battleTimeManager = new();
    private List<ISubManager> subManagers;

    [Header("伤害飘字配置（LabelPlayer_ 前缀 = LabelPlayerManager 使用）")]
    public AnimationCurve LabelPlayer_LabelAlphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    public float LabelPlayer_LabelExistTime = 1.2f;
    public AnimationCurve LabelPlayer_LabelScaleCurve = AnimationCurve.Constant(0f, 1f, 1f);
    public AnimationCurve LabelPlayer_LabelRiseCurve = AnimationCurve.Linear(0f, 0f, 1f, 72f);

    public SettlementManager SettlementManager => settlementManager;
    public SettleRewardInfo SettleReward => settlementManager.SettleReward;
    public RuntimeRewardInfo RuntimeReward => settlementManager.RuntimeReward;
    public BattleKillSummary KillSummary => settlementManager.KillSummary;
    public IReadOnlyDictionary<int, SCEntityDisplayInfo> EntityInfos => entityPlayerManager.InfoList;
    public IEnumerable<KeyValuePair<int, GameObject>> MaxExitPorts => clientInteractManager.MaxExitPorts;
    public IEnumerable<KeyValuePair<int, GameObject>> NormalExitPorts => clientInteractManager.NormalExitPorts;
    public IEnumerable<KeyValuePair<int, GameObject>> MinExitPorts => clientInteractManager.MinExitPorts;

    private void Awake()
    {
        if (Application.platform == RuntimePlatform.WindowsServer)
        {
            throw new System.Exception("Player Manager can't be used on server");
        }

        Tool.ClientLogicManager = this;
        subManagers = new List<ISubManager>
        {
            entityPlayerManager,
            labelPlayerManager,
            clientInteractManager,
            settlementManager,
            staminaManager,
            skillManager,
            battleTimeManager
        };

        EventManager.AddEvent(ClientEvent.OnEnterWorld, OnConnect);
        EventManager.AddEvent(ClientEvent.OnRestartGame, OnDisconnect);
    }

    private void OnConnect()
    {
        foreach (var manager in subManagers)
        {
            manager.OnInstall();
        }
        Debug.Log("install");
        inWorld = true;
    }

    private void Update()
    {
        if (!inWorld) return;
        foreach (var manager in subManagers)
        {
            manager.OnUpdate();
        }
    }

    private void OnDisconnect()
    {
        foreach (var manager in subManagers)
        {
            manager.OnUninstall();
        }
        inWorld = false;
    }

    public bool TryUseSkillSlot(int slot, bool hasDest, Vector3 dest) => skillManager.TryUseSkillSlot(slot, hasDest, dest);

    public bool TryGetLocalPlayerPosition(out Vector3 position)
        => entityPlayerManager.TryGetEntityPosition(NetworkManager.roomInfo.playerEntityId, out position);

    /// <summary>按实体 id 获取客户端模型的实时插值位置（转发给 EntityPlayerManager，供血条等 UI 跟随）。</summary>
    public bool TryGetEntityPosition(int id, out Vector3 position)
        => entityPlayerManager.TryGetEntityPosition(id, out position);

    /// <summary>在 center 周围 radius 内查找最近敌人（转发给 EntityPlayerManager）。</summary>
    public bool TryGetNearestEnemy(Vector3 center, float radius, out Vector3 enemyPos)
        => entityPlayerManager.TryGetNearestEnemy(center, radius, out enemyPos);

    public bool TryGetLocalPlayerDisplayInfo(out SCEntityDisplayInfo info)
        => entityPlayerManager.InfoList.TryGetValue(NetworkManager.roomInfo.playerEntityId, out info);

    public bool TryGetBattleStaminaInfo(out BattleStaminaInfo info)
        => staminaManager.TryGetBattleStaminaInfo(out info);

    public bool TryGetBattleSkillRuntimeInfo(int slot, out BattleSkillRuntimeInfo info)
        => skillManager.TryGetBattleSkillRuntimeInfo(slot, out info);

    public bool HasPickedUnlockProp(InteractablePropInfo.CharacterUnlockProp prop)
        => settlementManager.HasPickedUnlockProp(prop);
}

public interface ISubManager
{
    void OnInstall();
    void OnUninstall();
    void OnUpdate();
}

public class ClientBattleTimeManager : ISubManager
{
    private float enterTime;
    private int lastSeconds = -1;

    public void OnInstall()
    {
        enterTime = Time.time;
        lastSeconds = -1;
        PushTime(true);
    }

    public void OnUninstall()
    {
        lastSeconds = -1;
    }

    public void OnUpdate()
    {
        PushTime(false);
    }

    private void PushTime(bool force)
    {
        int seconds = Mathf.FloorToInt(Time.time - enterTime);
        if (!force && seconds == lastSeconds) return;
        lastSeconds = seconds;
        EventManager.TrigEvent(ClientEvent.UpdateBattleTime, seconds);
    }
}

public class ClientStaminaManager : ISubManager
{
    private bool staminaLost;
    private float lastPushedStamina = -1f;

    public float StaminaMax { get; private set; }
    public float LastStaminaValue { get; private set; }
    public float LastStaminaUpdateTime { get; private set; }

    public float CurrentStamina
    {
        get
        {
            if (StaminaMax <= 0f) return 0f;
            float elapsed = Mathf.Max(0f, Time.time - LastStaminaUpdateTime);
            return Mathf.Clamp(LastStaminaValue - elapsed * Config.stamina_decay_per_second, 0f, StaminaMax);
        }
    }

    public void OnInstall()
    {
        StaminaMax = GetPlayerMaxStamina();
        LastStaminaValue = StaminaMax;
        LastStaminaUpdateTime = Time.time;
        staminaLost = false;
        lastPushedStamina = -1f;
        PushStamina(true);
    }

    public void OnUninstall()
    {
        staminaLost = false;
        lastPushedStamina = -1f;
    }

    public void OnUpdate()
    {
        PushStamina(false);
        if (staminaLost) return;

        float stamina = CurrentStamina;
        if (stamina > 0f) return;

        staminaLost = true;
        LastStaminaValue = 0f;
        LastStaminaUpdateTime = Time.time;
        PushStamina(true);
        EventManager.TrigEvent(ClientEvent.ExitLost);
    }

    private void PushStamina(bool force)
    {
        float stamina = CurrentStamina;
        if (!force && Mathf.Abs(stamina - lastPushedStamina) < 0.05f) return;
        lastPushedStamina = stamina;
        EventManager.TrigEvent(ClientEvent.UpdateBattleStamina, new BattleStaminaInfo
        {
            stamina = stamina,
            maxStamina = StaminaMax
        });
    }

    public bool TryGetBattleStaminaInfo(out BattleStaminaInfo info)
    {
        info = new BattleStaminaInfo
        {
            stamina = CurrentStamina,
            maxStamina = StaminaMax
        };
        return StaminaMax > 0f;
    }

    private static float GetPlayerMaxStamina()
    {
        if (NetworkManager.playerInfo == null || Tool.InfoManager == null) return 0f;
        var type = NetworkManager.playerInfo.type;
        if (type.category != EntityCategory.Character) return 0f;
        int characterIndex = type.value;
        if (characterIndex < 0 || characterIndex >= Tool.InfoManager.CharacterInfoList.Count) return 0f;
        return Tool.InfoManager.CharacterInfoList[characterIndex].GetAttribute(NetworkManager.playerInfo.level).endurance;
    }
}

public class ClientSkillManager : ISubManager
{
    private readonly List<int> skillRemainingUseCounts = new();
    private readonly List<float> skillCooldownEndTimes = new();
    private int selectedSkillSlot;

    /// <summary>当前选中的技能槽位（滚轮切换由本管理器处理，输入只从 InputManager 读取）。</summary>
    public int SelectedSkillSlot => selectedSkillSlot;

    public void OnInstall()
    {
        selectedSkillSlot = 0;
        ResetSkillRuntime();
        PushAllSkillRuntime();
    }

    public void OnUninstall()
    {
        selectedSkillSlot = 0;
        skillRemainingUseCounts.Clear();
        skillCooldownEndTimes.Clear();
    }

    public void OnUpdate()
    {
        UpdateSkillSelection();
        UpdateSkillUse();
        PushAllSkillRuntime();
    }

    /// <summary>读取 InputManager 滚轮输入，切换选中技能槽位。</summary>
    private void UpdateSkillSelection()
    {
        var input = Tool.InputManager;
        if (input == null) return;
        float scroll = input.SkillScroll;
        if (Mathf.Approximately(scroll, 0f)) return;

        int count = GetSkillCount();
        if (count <= 0)
        {
            selectedSkillSlot = 0;
            return;
        }

        int direction = scroll > 0f ? Config.scroll_up_skill_slot_direction : Config.scroll_down_skill_slot_direction;
        selectedSkillSlot = (selectedSkillSlot + direction + count) % count;
    }

    /// <summary>读取 InputManager 左/右键输入，按相机模式决定目标并释放选中技能。</summary>
    private void UpdateSkillUse()
    {
        var input = Tool.InputManager;
        if (input == null) return;
        bool left = input.LeftSkillPressed;
        bool right = input.RightSkillPressed;
        if (!left && !right) return;

        if (TryGetAttackDest(left, right, out var dest))
        {
            TryUseSelectedSkill(true, dest);
        }
    }

    /// <summary>
    /// 决定技能释放目标点：
    /// 第三人称：左键=屏幕中央；右键=最近敌人(10m)，无敌人则与左键一致（屏幕中央）。
    /// 俯视：左/右键均=最近敌人(10m)，无敌人则触发提示事件"周围无有效攻击目标"。
    /// </summary>
    private bool TryGetAttackDest(bool left, bool right, out Vector3 dest)
    {
        dest = Vector3.zero;
        bool topDown = Tool.CameraController != null &&
                       Tool.CameraController.Mode == CameraController.CameraMode.TopDown;

        // 俯视：一律锁定最近敌人
        if (topDown)
        {
            if (TryGetNearestEnemyPos(out dest)) return true;
            EventManager.TrigEvent<string>(ClientEvent.ShowNotice, "周围无有效攻击目标");
            return false;
        }

        // 第三人称：右键优先最近敌人；无论左键/右键，未锁定敌人时都走屏幕中央
        if (right && TryGetNearestEnemyPos(out dest)) return true;

        var input = Tool.InputManager;
        if (input != null && input.TryGetCenterSkillPosition(out dest)) return true;
        return false;
    }

    /// <summary>获取本地玩家 10m 内最近敌人位置。</summary>
    private bool TryGetNearestEnemyPos(out Vector3 dest)
    {
        dest = Vector3.zero;
        var logic = Tool.ClientLogicManager;
        if (logic == null || !logic.TryGetLocalPlayerPosition(out var playerPos)) return false;
        return logic.TryGetNearestEnemy(playerPos, 10f, out dest);
    }

    private void TryUseSelectedSkill(bool hasDest, Vector3 dest)
    {
        var skills = NetworkManager.playerInfo?.skills;
        if (skills == null || selectedSkillSlot < 0 || selectedSkillSlot >= skills.Count) return;
        TryUseSkillSlot(selectedSkillSlot, hasDest, dest);
    }

    private int GetSkillCount()
    {
        var info = NetworkManager.playerInfo;
        if (info != null && info.skills != null)
        {
            return info.skills.Count;
        }
        return Config.skill_slot_count;
    }

    public bool TryUseSkillSlot(int slot, bool hasDest, Vector3 dest)
    {
        var skills = NetworkManager.playerInfo?.skills;
        if (skills == null || slot < 0 || slot >= skills.Count) return false;

        var skill = GetSkillAt(skills, slot);
        int skillId = skill.Key;

        EnsureSkillRuntimeSize(skills.Count);
        if (skillRemainingUseCounts[slot] <= 0) return false;
        if (Time.time < skillCooldownEndTimes[slot]) return false;

        if (hasDest) Tool.NetworkManager?.SendUseSkillRequest(skillId, dest);
        else Tool.NetworkManager?.SendUseSkillRequest(skillId);

        skillRemainingUseCounts[slot]--;
        skills[skillId] = skillRemainingUseCounts[slot];

        float cd = SkillManager.GetSkillCD(skillId);
        skillCooldownEndTimes[slot] = Time.time + cd;
        PushSkillRuntime(slot);
        return true;
    }

    private int GetSkillRemainingUseCount(int slot)
    {
        if (slot < 0 || slot >= skillRemainingUseCounts.Count) return 0;
        return skillRemainingUseCounts[slot];
    }

    private float GetSkillCooldownRate(int slot)
    {
        var skills = NetworkManager.playerInfo?.skills;
        if (skills == null || slot < 0 || slot >= skills.Count) return 0f;

        int skillId = GetSkillAt(skills, slot).Key;

        float cd = SkillManager.GetSkillCD(skillId);
        if (cd <= 0f || slot >= skillCooldownEndTimes.Count) return 0f;
        return Mathf.Clamp01((skillCooldownEndTimes[slot] - Time.time) / cd);
    }

    private void ResetSkillRuntime()
    {
        skillRemainingUseCounts.Clear();
        skillCooldownEndTimes.Clear();

        var skills = NetworkManager.playerInfo?.skills;
        if (skills == null) return;

        EnsureSkillRuntimeSize(skills.Count);
        for (int i = 0; i < skills.Count; i++)
        {
            var skill = GetSkillAt(skills, i);
            skillRemainingUseCounts[i] = skill.Value;
            skillCooldownEndTimes[i] = 0f;
        }
    }

    private void EnsureSkillRuntimeSize(int count)
    {
        while (skillRemainingUseCounts.Count < count) skillRemainingUseCounts.Add(0);
        while (skillCooldownEndTimes.Count < count) skillCooldownEndTimes.Add(0f);
    }

    private void PushAllSkillRuntime()
    {
        var skills = NetworkManager.playerInfo?.skills;
        if (skills == null) return;
        EnsureSkillRuntimeSize(skills.Count);
        for (int i = 0; i < skills.Count; i++)
        {
            PushSkillRuntime(i);
        }
    }

    private void PushSkillRuntime(int slot)
    {
        if (!TryGetBattleSkillRuntimeInfo(slot, out var info)) return;
        EventManager.TrigEvent(ClientEvent.UpdateBattleSkillRuntime, info);
    }

    public bool TryGetBattleSkillRuntimeInfo(int slot, out BattleSkillRuntimeInfo info)
    {
        var skills = NetworkManager.playerInfo?.skills;
        info = default;
        if (skills == null || slot < 0 || slot >= skills.Count) return false;
        EnsureSkillRuntimeSize(skills.Count);
        info = new BattleSkillRuntimeInfo
        {
            slot = slot,
            remainingCount = GetSkillRemainingUseCount(slot),
            cooldownRate = GetSkillCooldownRate(slot),
            selected = slot == selectedSkillSlot
        };
        return true;
    }

    private static KeyValuePair<int, int> GetSkillAt(Dictionary<int, int> skills, int slot)
    {
        int index = 0;
        foreach (var skill in skills)
        {
            if (index == slot) return skill;
            index++;
        }
        return default;
    }
}

public class EntityPlayerManager : ISubManager
{
    private readonly Dictionary<int, SCEntityDisplayInfo> infoList = new();
    private readonly Dictionary<int, float> infoReachTime = new();
    private readonly Dictionary<int, Transform> graphics = new();
    private readonly HashSet<int> toRemove = new();
    private const float surviveTime = 1.2f;

    public IReadOnlyDictionary<int, SCEntityDisplayInfo> InfoList => infoList;

    public void OnInstall()
    {
        EventManager.AddEvent<SCEntityDisplayInfo>(ClientEvent.UpdateEntityDisplayInfo, UpdateInfo);
        EventManager.AddEvent<int>(ClientEvent.RemoveEntityDisplayInfo, RemoveInfo);
    }

    public void OnUninstall()
    {
        EventManager.RemoveEvent<SCEntityDisplayInfo>(ClientEvent.UpdateEntityDisplayInfo, UpdateInfo);
        EventManager.RemoveEvent<int>(ClientEvent.RemoveEntityDisplayInfo, RemoveInfo);
        foreach (var id in infoList.Keys)
        {
            if (!graphics.TryGetValue(id, out var graphic) || graphic == null) continue;
            EntityPool.ReturnGraphic(graphic.gameObject);
        }
        infoList.Clear();
        infoReachTime.Clear();
        graphics.Clear();
        toRemove.Clear();
    }

    public void OnUpdate()
    {
        toRemove.Clear();
        float threshold = Time.time - surviveTime;
        foreach (var pair in infoReachTime)
        {
            if (pair.Value < threshold) toRemove.Add(pair.Key);
        }
        foreach (var id in toRemove) RemoveInfo(id);

        foreach (var id in infoList.Keys)
        {
            var graphic = graphics[id];
            var time = Time.time - infoReachTime[id];
            var info = infoList[id];
            graphic.transform.rotation = Quaternion.Euler(0, info.yaw + time * info.angularSpeed, 0);
            var front = graphic.transform.forward;
            var targetPosition = new Vector3(info.x, info.y, info.z) + info.speed * time * front;
            graphic.transform.position = ConstrainToWall(graphic, targetPosition);

            if (!graphic.TryGetComponent<EntityPlayer>(out var entityPlayer))
            {
                entityPlayer = graphic.gameObject.AddComponent<EntityPlayer>();
            }
            entityPlayer.RefreshAnimation(info.type, info.speed, HasNearbyPlayer(id, graphic.transform.position));
        }

        // 所有角色位置更新完成，通知相机等下游系统（相机更新后再触发 OnPostCameraControllerUpdate 给血条等 UI）
        EventManager.TrigEvent(ClientEvent.OnPostEntityPlayerUpdate);
    }

    private static readonly RaycastHit[] s_constrainHits = new RaycastHit[16];

    /// <summary>
    /// 客户端移动防穿墙：角色/NPC 模型（带 CapsuleCollider）从当前位置向目标位置做水平 SphereCast，
    /// 命中墙体则停在碰撞点前（球面贴墙），不进入墙内。自身碰撞体与其它角色/NPC 模型不视为障碍
    /// （服务器权威位置本身不穿墙，此处仅修正插值预测瞬间的越界，保证视觉表现正常）。
    /// 无碰撞体（植物/矿石等）或未发生水平移动时原样返回。
    /// </summary>
    private static Vector3 ConstrainToWall(Transform graphic, Vector3 target)
    {
        if (!graphic.TryGetComponent<CapsuleCollider>(out var capsule)) return target;
        Vector3 origin = graphic.position;
        Vector3 horizontal = new Vector3(target.x - origin.x, 0f, target.z - origin.z);
        float hDist = horizontal.magnitude;
        if (hDist <= 0.0001f) return target;

        Vector3 hDir = horizontal / hDist;
        Vector3 castOrigin = origin + capsule.center;   // 胶囊中心高度
        // 排除实体层：自身与其它角色/NPC 均在该层，不视为障碍；仅检测场景墙体等障碍物
        LayerMask mask = Tool.InfoManager == null
            ? Physics.DefaultRaycastLayers
            : ~(1 << Tool.InfoManager.EntityLayer);
        int count = Physics.SphereCastNonAlloc(castOrigin, capsule.radius, hDir, s_constrainHits,
            hDist + capsule.radius, mask, QueryTriggerInteraction.Ignore);

        float allowed = hDist;
        for (int i = 0; i < count; i++)
        {
            float d = s_constrainHits[i].distance - capsule.radius;   // 球面贴墙时的允许前进距离
            if (d < allowed) allowed = d;
        }
        if (allowed >= hDist) return target;

        Vector3 stopped = origin + hDir * Mathf.Max(0f, allowed);
        stopped.y = target.y;   // 垂直方向跟随插值，仅约束水平越界
        return stopped;
    }

    private void UpdateInfo(SCEntityDisplayInfo info)
    {
        int id = info.id;
        if (infoList.ContainsKey(id))
        {
            infoList[id] = info;
            infoReachTime[id] = Time.time;
        }
        else
        {
            infoList.Add(id, info);
            infoReachTime.Add(id, Time.time);
            Vector3 pos = new(info.x, info.y, info.z);
            var graphic = EntityPool.GetGraphic(info.type, pos);
            graphics.Add(id, graphic.transform);
            graphic.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, info.yaw, 0));
        }

        if (id == NetworkManager.roomInfo.playerEntityId)
        {
            EventManager.TrigEvent(ClientEvent.UpdateBattleHealth, new BattleHealthInfo
            {
                health = info.health,
                maxHealth = info.maxHealth
            });
        }
    }

    private void RemoveInfo(int id)
    {
        if (!infoList.ContainsKey(id)) return;
        infoList.Remove(id);
        infoReachTime.Remove(id);
        if (!graphics.TryGetValue(id, out var graphic)) return;
        graphics.Remove(id);
        if (graphic != null) EntityPool.ReturnGraphic(graphic.gameObject);
    }

    public bool TryGetEntityPosition(int id, out Vector3 position)
    {
        position = Vector3.zero;
        if (!graphics.TryGetValue(id, out var graphic) || graphic == null) return false;
        position = graphic.position;
        return true;
    }

    /// <summary>
    /// 在 center 周围 radius 内查找最近的敌人（排除本地玩家与纯资源型实体：植物/矿石）。
    /// 找到返回 true，并输出敌人位置；否则返回 false。
    /// </summary>
    public bool TryGetNearestEnemy(Vector3 center, float radius, out Vector3 enemyPos)
    {
        enemyPos = center;
        int localId = NetworkManager.roomInfo.playerEntityId;
        float sqrRadius = radius * radius;
        float bestSqr = sqrRadius;
        bool found = false;
        foreach (var pair in infoList)
        {
            if (pair.Key == localId) continue;
            var info = pair.Value;
            // 纯资源型实体不算敌人
            if (info.type.IsPlant() || info.type.IsOre()) continue;

            Vector3 pos = new(info.x, info.y, info.z);
            float sqr = (pos - center).sqrMagnitude;
            if (sqr > bestSqr) continue;
            bestSqr = sqr;
            enemyPos = pos;
            found = true;
        }
        return found;
    }

    private bool HasNearbyPlayer(int selfId, Vector3 position)
    {
        const float sqrRange = Config.npc_attack_range * Config.npc_attack_range;
        foreach (var pair in infoList)
        {
            if (pair.Key == selfId || !pair.Value.type.IsCharacter()) continue;
            Vector3 playerPos = new(pair.Value.x, pair.Value.y, pair.Value.z);
            if ((playerPos - position).sqrMagnitude <= sqrRange) return true;
        }
        return false;
    }
}

/// <summary>
/// 飘字渲染数据：一帧内单个飘字的完整显示信息。
/// worldPos 为世界坐标，由 UI 层（BattlePage）转换为界面坐标。
/// 动画曲线（透明度/缩放/上飘）来自 ClientLogicManager 的 LabelPlayer_ 配置，随数据一并传递。
/// </summary>
public struct SceneLabelData
{
    public Vector3 worldPos;
    public string content;
    public Color color;
    public float createTime;
    public float lifeTime;
    public AnimationCurve alphaCurve;   // 透明度变化（x=进度 0~1，y=alpha 0~1）
    public AnimationCurve scaleCurve;   // 缩放变化（x=进度 0~1，y=缩放系数，乘世界距离缩放）
    public AnimationCurve riseCurve;    // y 上飘距离（x=进度 0~1，y=上飘高度）
}

/// <summary>
/// 血条渲染数据：一帧内单个血条的完整显示信息。
/// worldPos 为世界坐标，由 UI 层（BattlePage）转换为界面坐标。
/// </summary>
public struct SceneEntityBarData
{
    public Vector3 worldPos;
    public int health;
    public int maxHealth;
    public int level;
}

public class LabelPlayerManager : ISubManager
{
    /// <summary>
    /// 每帧飘字数据列表事件（param = List&lt;SceneLabelData&gt;）。
    /// LabelPlayerManager 每帧处理完飘字后触发，BattlePage 接收并批量刷新。
    /// </summary>
    public const int UpdateSceneLabelsEvent = 10308;
    /// <summary>
    /// 每帧血条数据列表事件（param = List&lt;SceneEntityBarData&gt;）。
    /// LabelPlayerManager 每帧处理完血条后触发，BattlePage 接收并批量刷新。
    /// </summary>
    public const int UpdateSceneEntityBarsEvent = 10309;
    private static readonly Dictionary<TextColor, Color> colorMap = new()
    {
        { TextColor.White, Color.white },
        { TextColor.Orange, new Color(0.9f, 0.6f, 0.3f) },
        { TextColor.Red, Color.red },
        { TextColor.Green, Color.green },
        { TextColor.Blue, Color.blue },
    };

    private struct BattleLabelItem
    {
        public Vector3 worldPos;
        public string content;
        public Color color;
        public float createTime;
    }

    private const float labelLifeTime = 1.2f;   // 兜底存在时间（ClientLogicManager 配置为空时）
    private readonly List<BattleLabelItem> labelList = new();
    // 复用的飘字数据列表（事件同步触发，回调消费完后清空重填，避免每帧 GC）
    private readonly List<SceneLabelData> m_labelData = new();
    // 复用的血条数据列表
    private readonly List<SceneEntityBarData> m_entityBarData = new();

    public void OnInstall()
    {
        ClearBars();
        ClearLabels();
        EventManager.AddEvent<(string, Vector3, TextColor)>(ClientEvent.ShowSceneLabel, ShowLabel);
        // 相机位置更新完成后刷新血条：此时角色插值位置与相机均为本帧最新值，世界→屏幕转换不再滞后一帧
        EventManager.AddEvent(ClientEvent.OnPostCameraControllerUpdate, OnPostCameraControllerUpdate);
    }

    public void OnUninstall()
    {
        EventManager.RemoveEvent<(string, Vector3, TextColor)>(ClientEvent.ShowSceneLabel, ShowLabel);
        EventManager.RemoveEvent(ClientEvent.OnPostCameraControllerUpdate, OnPostCameraControllerUpdate);
        ClearBars();
        ClearLabels();
    }

    public void OnUpdate()
    {
        if (Tool.UIManager == null || Tool.UIManager.BattlePage == null) return;
        // 血条由 OnPostCameraControllerUpdate 事件驱动（相机更新后），此处只处理飘字
        RefreshLabels();
    }

    /// <summary>
    /// 相机位置更新完成后回调：刷新全部血条（用本帧相机做世界→屏幕转换）。
    /// </summary>
    private void OnPostCameraControllerUpdate()
    {
        if (Tool.UIManager == null || Tool.UIManager.BattlePage == null) return;
        RefreshEntityBars();
    }

    /// <summary>
    /// 每帧处理血条：把本帧所有实体血条数据打包为列表，通过事件批量传递给 BattlePage。
    /// UI 层收到后统一用 Tool.ActiveFor 激活/禁用 EntityBar，无需在此逐帧操控 GameObject。
    /// </summary>
    private void RefreshEntityBars()
    {
        var logic = Tool.ClientLogicManager;
        if (logic == null) return;

        m_entityBarData.Clear();
        foreach (var pair in logic.EntityInfos)
        {
            var info = pair.Value;
            m_entityBarData.Add(new SceneEntityBarData
            {
                worldPos = GetEntityBarWorldPos(info),
                health = info.health,
                maxHealth = info.maxHealth,
                level = info.level,
            });
        }
        EventManager.TrigEvent(UpdateSceneEntityBarsEvent, m_entityBarData);
    }

    /// <summary>
    /// 每帧处理飘字：清理过期项后，把本帧所有活跃飘字打包为数据列表，通过事件批量传递给 BattlePage。
    /// 动画参数（存在时间/透明度/缩放/上飘曲线）取自 ClientLogicManager 的 LabelPlayer_ 配置。
    /// UI 层收到后统一用 Tool.ActiveFor 激活/禁用 Label，无需在此逐帧操控 GameObject。
    /// </summary>
    private void RefreshLabels()
    {
        var logic = Tool.ClientLogicManager;
        float life = logic == null ? labelLifeTime : logic.LabelPlayer_LabelExistTime;

        for (int i = labelList.Count - 1; i >= 0; i--)
        {
            if (Time.time - labelList[i].createTime >= life) labelList.RemoveAt(i);
        }

        var alpha = logic == null ? null : logic.LabelPlayer_LabelAlphaCurve;
        var scale = logic == null ? null : logic.LabelPlayer_LabelScaleCurve;
        var rise = logic == null ? null : logic.LabelPlayer_LabelRiseCurve;
        m_labelData.Clear();
        foreach (var item in labelList)
        {
            m_labelData.Add(new SceneLabelData
            {
                worldPos = item.worldPos,
                content = item.content,
                color = item.color,
                createTime = item.createTime,
                lifeTime = life,
                alphaCurve = alpha,
                scaleCurve = scale,
                riseCurve = rise,
            });
        }
        EventManager.TrigEvent(UpdateSceneLabelsEvent, m_labelData);
    }

    private void ShowLabel((string, Vector3, TextColor) param)
    {
        Color color = colorMap.TryGetValue(param.Item3, out var mapped) ? mapped : Color.white;
        var item = new BattleLabelItem
        {
            worldPos = GetLabelStartWorldPos(param.Item2),
            content = param.Item1,
            color = color,
            createTime = Time.time,
        };
        labelList.Add(item);
    }

    private Vector3 GetLabelStartWorldPos(Vector3 fallbackPos)
    {
        var logic = Tool.ClientLogicManager;
        if (logic == null) return fallbackPos;

        EntityType type = default;
        float sqrDistance = float.MaxValue;
        bool found = false;
        foreach (var info in logic.EntityInfos.Values)
        {
            Vector3 entityPos = new(info.x, info.y, info.z);
            float distance = Vector3.SqrMagnitude(entityPos - fallbackPos);
            if (distance >= sqrDistance) continue;
            sqrDistance = distance;
            type = info.type;
            fallbackPos = entityPos;
            found = true;
        }
        if (!found || Tool.InfoManager == null) return fallbackPos;
        return fallbackPos + Vector3.up * Tool.InfoManager.GetEntityBarYOffset(type);
    }

    /// <summary>
    /// 计算血条世界坐标：优先取客户端模型的实时插值位置（graphic.position，与角色模型刚性绑定，避免快照跳变）；
    /// 模型不存在时回退到服务器快照坐标。
    /// </summary>
    private static Vector3 GetEntityBarWorldPos(SCEntityDisplayInfo info)
    {
        float yOffset = Tool.InfoManager == null ? 2f : Tool.InfoManager.GetEntityBarYOffset(info.type);
        var logic = Tool.ClientLogicManager;
        if (logic != null && logic.TryGetEntityPosition(info.id, out var position))
        {
            return position + Vector3.up * yOffset;
        }
        return new Vector3(info.x, info.y, info.z) + Vector3.up * yOffset;
    }

    /// <summary>
    /// 世界坐标 → UI 父节点局部坐标 + 缩放（供血条与飘字共用；飘字由 BattlePage 调用）。
    /// </summary>
    public static bool TryWorldToParentPoint(Vector3 worldPos, RectTransform target, out Vector2 layerPos, out float scale)
    {
        layerPos = Vector2.zero;
        scale = 1f;
        var camera = Camera.main;
        var parentRect = target == null ? null : target.parent as RectTransform;
        if (camera == null || parentRect == null) return false;

        Vector3 screenPos = camera.WorldToScreenPoint(worldPos);
        if (screenPos.z <= 0f) return false;

        Camera uiCamera = null;
        Canvas battleCanvas = parentRect.GetComponentInParent<Canvas>();
        if (battleCanvas != null && battleCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = battleCanvas.worldCamera;
        }
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, uiCamera, out layerPos))
        {
            return false;
        }

        float distance = Vector3.Distance(camera.transform.position, worldPos);
        float rate = Mathf.InverseLerp(Config.battle_ui_min_scale_distance, Config.battle_ui_max_scale_distance, distance);
        scale = Mathf.Lerp(Config.battle_ui_max_scale, Config.battle_ui_min_scale, rate);
        return true;
    }

    private void ClearBars()
    {
        // 推送空列表，让 BattlePage 通过 ActiveFor 禁用全部血条
        m_entityBarData.Clear();
        EventManager.TrigEvent(UpdateSceneEntityBarsEvent, m_entityBarData);
    }

    private void ClearLabels()
    {
        labelList.Clear();
        // 推送空列表，让 BattlePage 通过 ActiveFor 禁用全部飘字
        m_labelData.Clear();
        EventManager.TrigEvent(UpdateSceneLabelsEvent, m_labelData);
    }
}

public class ClientInteractManager : ISubManager
{
    private readonly Dictionary<InteractablePropInfo.CharacterUnlockProp, GameObject> characterUnlockProps = new();
    private readonly Dictionary<int, GameObject> maxExitPort = new();
    private readonly Dictionary<int, GameObject> normalExitPort = new();
    private readonly Dictionary<int, GameObject> minExitPort = new();
    private InteractablePropInfo interactablePropInfo;

    public IEnumerable<KeyValuePair<int, GameObject>> MaxExitPorts => maxExitPort;
    public IEnumerable<KeyValuePair<int, GameObject>> NormalExitPorts => normalExitPort;
    public IEnumerable<KeyValuePair<int, GameObject>> MinExitPorts => minExitPort;

    public void OnInstall()
    {
        EventManager.AddEvent<InteractablePropInfo>(ClientEvent.UpdateInteractablePropInfo, LoadInfo);
    }

    public void OnUninstall()
    {
        EventManager.RemoveEvent<InteractablePropInfo>(ClientEvent.UpdateInteractablePropInfo, LoadInfo);
        ClearList(characterUnlockProps);
        ClearList(maxExitPort);
        ClearList(normalExitPort);
        ClearList(minExitPort);
    }

    public void OnUpdate()
    {
        InteractablePropInfo.CharacterUnlockProp pickedProp = InteractablePropInfo.CharacterUnlockProp.None;
        foreach (var pair in characterUnlockProps)
        {
            if (!InPickArea(pair.Value.transform.position)) continue;
            pickedProp = pair.Key;
            break;
        }
        if (pickedProp == InteractablePropInfo.CharacterUnlockProp.None) return;

        interactablePropInfo.characterUnlockProp &= ~pickedProp;
        EventManager.TrigEvent(ClientEvent.PickUnlockProp, pickedProp);
        RefreshCharacterUnlockProps(interactablePropInfo.characterUnlockProp);
    }

    private void LoadInfo(InteractablePropInfo info)
    {
        interactablePropInfo = info;
        RefreshCharacterUnlockProps(info.characterUnlockProp);
        RefreshExitPorts(maxExitPort, Tool.AssetsManager.PortMax, Landscape.instance.ExitPortList, info.maxExitPort);
        RefreshExitPorts(normalExitPort, Tool.AssetsManager.PortNormal, Landscape.instance.ExitPortList, info.normalExitPort);
        RefreshExitPorts(minExitPort, Tool.AssetsManager.PortMin, Landscape.instance.ExitPortList, info.minExitPort);
    }

    private void RefreshCharacterUnlockProps(InteractablePropInfo.CharacterUnlockProp info)
    {
        RefreshCharacterUnlockProps(characterUnlockProps, Tool.AssetsManager.CharacterUnlockProp, Landscape.instance.CharacterUnlockPropPos, info);
    }

    private static bool InPickArea(Vector3 pos)
    {
        var logic = Tool.ClientLogicManager;
        if (logic == null || !logic.TryGetLocalPlayerPosition(out var playerPos)) return false;
        return (pos - playerPos).sqrMagnitude < 1f;
    }

    private static void RefreshCharacterUnlockProps(Dictionary<InteractablePropInfo.CharacterUnlockProp, GameObject> active, GameObject source,
        List<Transform> anchors, InteractablePropInfo.CharacterUnlockProp info)
    {
        int index = 0;
        foreach (InteractablePropInfo.CharacterUnlockProp e in System.Enum.GetValues(typeof(InteractablePropInfo.CharacterUnlockProp)))
        {
            if (e == InteractablePropInfo.CharacterUnlockProp.None) continue;
            if (index >= anchors.Count) break;
            bool shouldShow = info.HasFlag(e) &&
                              Tool.SaveManager.characterLevels[index] < Config.min_entity_level &&
                              (Tool.ClientLogicManager == null || !Tool.ClientLogicManager.HasPickedUnlockProp(e));
            if (shouldShow)
            {
                if (!active.ContainsKey(e))
                {
                    var t = anchors[index];
                    var obj = Object.Instantiate(source, t.position, t.rotation);
                    active.Add(e, obj);
                }
            }
            else if (active.TryGetValue(e, out var obj))
            {
                Object.Destroy(obj);
                active.Remove(e);
            }
            index++;
        }
    }

    private static void RefreshExitPorts(Dictionary<int, GameObject> active, GameObject source, List<Transform> anchors, InteractablePropInfo.ExitPort info)
    {
        for (int index = 0; index < anchors.Count && index < 16; index++)
        {
            bool enabled = ((int)info & (1 << index)) != 0;
            if (enabled)
            {
                if (!active.ContainsKey(index))
                {
                    var t = anchors[index];
                    var obj = Object.Instantiate(source, t.position, t.rotation);
                    active.Add(index, obj);
                }
            }
            else if (active.TryGetValue(index, out var obj))
            {
                Object.Destroy(obj);
                active.Remove(index);
            }
        }
    }

    private static void ClearList<T>(Dictionary<T, GameObject> list)
    {
        foreach (var pair in list)
        {
            if (pair.Value != null) Object.Destroy(pair.Value);
        }
        list.Clear();
    }
}

public class SettlementManager : ISubManager
{
    public enum ExitMode
    {
        Unknown, Max, Normal, Min, Lost
    }

    private readonly HashSet<InteractablePropInfo.CharacterUnlockProp> pickedUnlockProps = new();
    private readonly Dictionary<int, int> pvpRunes = new();
    private readonly List<EntityType> pvpVictimTypes = new();
    private float enterTime;
    private float exitProcess;
    private bool settled;

    public ExitMode exitMode = ExitMode.Unknown;
    public SettleRewardInfo SettleReward { get; private set; } = new();
    public RuntimeRewardInfo RuntimeReward { get; private set; } = new();
    public BattleKillSummary KillSummary { get; private set; }
    public IReadOnlyDictionary<int, int> PvpRunes => pvpRunes;
    public IReadOnlyList<EntityType> PvpVictimTypes => pvpVictimTypes;

    public void OnInstall()
    {
        KillSummary = default;
        SettleReward = new SettleRewardInfo();
        RuntimeReward = new RuntimeRewardInfo();
        pickedUnlockProps.Clear();
        pvpRunes.Clear();
        pvpVictimTypes.Clear();
        exitMode = ExitMode.Unknown;
        enterTime = Time.time;
        exitProcess = 0f;
        settled = false;
        EventManager.TrigEvent(ClientEvent.UpdateBattleKillSummary, KillSummary);

        EventManager.AddEvent<byte>(NetworkEvent.KillPlayer, OnKillPlayer);
        EventManager.AddEvent<byte>(NetworkEvent.KillZombie, OnKillZombie);
        EventManager.AddEvent<byte>(NetworkEvent.KillPlant, OnKillPlant);
        EventManager.AddEvent<byte>(NetworkEvent.KillInfectedPlant, OnKillInfectedPlant);
        EventManager.AddEvent<byte>(NetworkEvent.KillOre, OnKillOre);
        EventManager.AddEvent<byte>(NetworkEvent.KillInfectedOre, OnKillInfectedOre);
        EventManager.AddEvent<byte>(NetworkEvent.KillInfection, OnKillInfection);
        EventManager.AddEvent<SCPvpKillRewardInfo>(ClientEvent.PvpKillReward, OnPvpKillReward);
        EventManager.AddEvent<InteractablePropInfo.CharacterUnlockProp>(ClientEvent.PickUnlockProp, PickUnlockProp);
        EventManager.AddEvent(ClientEvent.ExitLost, OnExitLost);
    }

    public void OnUninstall()
    {
        EventManager.RemoveEvent<byte>(NetworkEvent.KillPlayer, OnKillPlayer);
        EventManager.RemoveEvent<byte>(NetworkEvent.KillZombie, OnKillZombie);
        EventManager.RemoveEvent<byte>(NetworkEvent.KillPlant, OnKillPlant);
        EventManager.RemoveEvent<byte>(NetworkEvent.KillInfectedPlant, OnKillInfectedPlant);
        EventManager.RemoveEvent<byte>(NetworkEvent.KillOre, OnKillOre);
        EventManager.RemoveEvent<byte>(NetworkEvent.KillInfectedOre, OnKillInfectedOre);
        EventManager.RemoveEvent<byte>(NetworkEvent.KillInfection, OnKillInfection);
        EventManager.RemoveEvent<SCPvpKillRewardInfo>(ClientEvent.PvpKillReward, OnPvpKillReward);
        EventManager.RemoveEvent<InteractablePropInfo.CharacterUnlockProp>(ClientEvent.PickUnlockProp, PickUnlockProp);
        EventManager.RemoveEvent(ClientEvent.ExitLost, OnExitLost);
    }

    public void OnUpdate()
    {
        if (settled) return;

        ExitMode nextMode = GetCurrentExitMode();
        if (nextMode == ExitMode.Unknown)
        {
            exitMode = ExitMode.Unknown;
            exitProcess = 0f;
            EventManager.TrigEvent(ClientEvent.UpdateExitProcess, exitProcess);
            return;
        }

        if (exitMode != nextMode)
        {
            exitMode = nextMode;
            exitProcess = 0f;
        }

        float readTime = exitMode switch
        {
            ExitMode.Max => Config.exit_max_read_time,
            ExitMode.Normal => Config.exit_normal_read_time,
            ExitMode.Min => Config.exit_min_read_time,
            _ => Config.exit_min_read_time
        };
        exitProcess = Mathf.Clamp01(exitProcess + Time.deltaTime / readTime);
        EventManager.TrigEvent(ClientEvent.UpdateExitProcess, exitProcess);
        if (exitProcess < 1f) return;

        if (exitMode == ExitMode.Max) EventManager.TrigEvent(ClientEvent.ExitMax);
        else if (exitMode == ExitMode.Normal) EventManager.TrigEvent(ClientEvent.ExitNormal);
        else EventManager.TrigEvent(ClientEvent.ExitMin);
        Settle(exitMode);
    }

    public bool HasPickedUnlockProp(InteractablePropInfo.CharacterUnlockProp prop)
        => pickedUnlockProps.Contains(prop);

    public void Settle(ExitMode mode)
    {
        if (settled) return;
        settled = true;

        SettleReward.timeCount = Mathf.FloorToInt(Time.time - enterTime);
        AddExitModeReward(mode, SettleReward.timeCount, SettleReward);
        WriteSave(mode, SettleReward);
        EventManager.TrigEvent(ClientEvent.UpdateSettlementReward, SettleReward);
        Tool.NetworkManager.ExitWorld();
        Tool.UIManager.ShowPage(PageType.Award);
    }

    private void OnExitLost()
    {
        Settle(ExitMode.Lost);
    }

    /// <summary>
    /// 结算奖励：
    /// 1) 角色经验 = floor(存活秒/60)*5 + 本局实时击杀经验，再 × 结算倍率（初级1/中级1.5/高级2/迷失0.3）。
    /// 2) 撤离信物奖励（中级 30% 触发 0~2；高级 100% 触发 2~4，类型 14 角色均匀随机）。
    /// 3) 基础印记：从全部印记池随机 X 类，每类发放 Y 经验。
    /// </summary>
    private static void AddExitModeReward(ExitMode mode, int surviveSeconds, SettleRewardInfo reward)
    {
        float multiplier = mode switch
        {
            ExitMode.Max => Config.exit_exp_multiplier_max,
            ExitMode.Normal => Config.exit_exp_multiplier_normal,
            ExitMode.Min => Config.exit_exp_multiplier_min,
            _ => Config.exit_exp_multiplier_lost
        };
        int baseExp = Mathf.FloorToInt(surviveSeconds / 60f) * Config.character_exp_per_survive_minute;
        reward.GetExp(Mathf.FloorToInt((baseExp + reward.exp) * multiplier));

        AddExitModeTokenReward(mode, reward);
        AddExitModeImprintReward(mode, reward);
    }

    /// <summary>撤离结算信物：中级 30% 触发 0~2（0:50/1:35/2:15）；高级 100% 触发 2~4（2:60/3:30/4:10）；迷失/初级不获得。</summary>
    private static void AddExitModeTokenReward(ExitMode mode, SettleRewardInfo reward)
    {
        float[] weights;
        float trigger = 1f;
        switch (mode)
        {
            case ExitMode.Max:
                weights = Config.exit_token_high_count_weights;
                break;
            case ExitMode.Normal:
                weights = Config.exit_token_mid_count_weights;
                trigger = Config.exit_token_mid_trigger_rate;
                break;
            default:
                return;
        }
        if (Random.value >= trigger) return;
        int count = RollWeightedIndex(weights);
        for (int i = 0; i < count; i++)
        {
            reward.GetCharacterToken(Random.Range(0, Config.character_count), 1);
        }
    }

    /// <summary>按权重数组随机返回索引（索引即数量）。</summary>
    private static int RollWeightedIndex(float[] weights)
    {
        float total = 0f;
        foreach (var w in weights) total += w;
        if (total <= 0f) return 0;
        float roll = Random.value * total;
        for (int i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0f) return i;
        }
        return weights.Length - 1;
    }

    /// <summary>基础印记：从全部印记池随机 X 类（不重复），每类发放 Y 经验。</summary>
    private static void AddExitModeImprintReward(ExitMode mode, SettleRewardInfo reward)
    {
        int typeCount, expEach;
        switch (mode)
        {
            case ExitMode.Max: typeCount = Config.exit_max_imprint_type_count; expEach = Config.exit_max_imprint_exp_each; break;
            case ExitMode.Normal: typeCount = Config.exit_normal_imprint_type_count; expEach = Config.exit_normal_imprint_exp_each; break;
            case ExitMode.Min: typeCount = Config.exit_min_imprint_type_count; expEach = Config.exit_min_imprint_exp_each; break;
            default: typeCount = Config.exit_lost_imprint_type_count; expEach = Config.exit_lost_imprint_exp_each; break;
        }
        if (expEach <= 0) return;

        int count = Mathf.Min(typeCount, Config.imprint_count);
        var chosen = new HashSet<int>();
        while (chosen.Count < count)
        {
            chosen.Add(Random.Range(0, Config.imprint_count));
        }
        foreach (var idx in chosen)
        {
            reward.GetImprint(idx, expEach);
        }
    }

    private void WriteSave(ExitMode mode, SettleRewardInfo reward)
    {
        var save = Tool.SaveManager;
        save.AddCharacterExp(GetSelectedCharacterIndex(), reward.exp);

        for (int i = 0; i < reward.note.Count; i++)
        {
            save.AddNote(i, reward.note[i]);
        }

        for (int i = 0; i < reward.imprint.Count; i++)
        {
            save.AddImprintExp(i, reward.imprint[i]);
        }

        foreach (var characterIndex in reward.characterUnlocks)
        {
            save.UnlockCharacter(characterIndex);
        }

        if (mode != ExitMode.Lost)
        {
            for (int charIndex = 0; charIndex < reward.characterToken.Count; charIndex++)
            {
                save.AddCharacterToken(charIndex, reward.characterToken[charIndex]);
            }

            Dictionary<int, int> carriedRunes = new();
            if (NetworkManager.playerInfo != null && NetworkManager.playerInfo.skills != null)
            {
                AddRunes(carriedRunes, NetworkManager.playerInfo.skills);
            }
            AddRunes(carriedRunes, pvpRunes);
            for (int skillId = 0; skillId < reward.rune.Count; skillId++)
            {
                int count = reward.rune[skillId];
                if (count > 0) AddRune(carriedRunes, skillId, count);
            }
            // 撤离带出负重裁剪（上限 初级60/中级120/高级200），超重按优先级丢弃
            TrimCarriedRunesByWeight(carriedRunes, GetExitCarryWeightLimit(mode));
            save.ApplySuccessfulExtraction(carriedRunes);
        }

        save.Save();
    }

    private static int GetSelectedCharacterIndex()
    {
        if (NetworkManager.playerInfo == null) return HomePage.currentSelectedCharacter;
        var type = NetworkManager.playerInfo.type;
        return type.category == EntityCategory.Character ? type.value : HomePage.currentSelectedCharacter;
    }

    private ExitMode GetCurrentExitMode()
    {
        var logic = Tool.ClientLogicManager;
        if (logic == null || !logic.TryGetLocalPlayerPosition(out var playerPos)) return ExitMode.Unknown;

        if (IsInAnyExitPort(playerPos, logic.MaxExitPorts)) return ExitMode.Max;
        if (IsInAnyExitPort(playerPos, logic.NormalExitPorts)) return ExitMode.Normal;
        if (IsInAnyExitPort(playerPos, logic.MinExitPorts)) return ExitMode.Min;
        return ExitMode.Unknown;
    }

    private static bool IsInAnyExitPort(Vector3 playerPos, IEnumerable<KeyValuePair<int, GameObject>> ports)
    {
        float sqrRadius = Config.exit_area_radius * Config.exit_area_radius;
        foreach (var pair in ports)
        {
            if (pair.Value == null) continue;
            if ((pair.Value.transform.position - playerPos).sqrMagnitude < sqrRadius) return true;
        }
        return false;
    }

    private void OnKillPlayer(byte value)
    {
        KillSummary = AddKills(KillSummary, player: value);
        for (int i = 0; i < value; i++)
        {
            SettleReward.GetExp(Config.kill_player_exp);
            AddRandomImprintExp(Config.kill_player_exp);
        }
        PushKillSummary();
    }

    private void OnKillZombie(byte value)
    {
        KillSummary = AddKills(KillSummary, zombie: value);
        for (int i = 0; i < value; i++)
        {
            SettleReward.GetExp(Config.kill_zombie_exp);
            SettleReward.GetNote(0, 1);
            TryAddRuneDrop(SkillInfo.Quality.B, Config.zombie_drop_b_rune_rate, false);
            TryAddRuneDrop(SkillInfo.Quality.A, Config.zombie_drop_a_rune_rate, false);
            TryAddRuneDrop(SkillInfo.Quality.S, Config.zombie_drop_s_rune_rate, false);
            AddRandomImprintExp(Config.kill_zombie_exp);
        }
        PushKillSummary();
    }

    private void OnKillPlant(byte value)
    {
        KillSummary = AddKills(KillSummary, plant: value);
        for (int i = 0; i < value; i++)
        {
            if (Random.value < Config.plant_fruit_drop_rate) RuntimeReward.GetFruit(1);
            if (Random.value < Config.plant_note_drop_rate) SettleReward.GetNote(1, 1);
        }
        PushKillSummary();
    }

    private void OnKillInfectedPlant(byte value)
    {
        KillSummary = AddKills(KillSummary, infectedPlant: value);
        for (int i = 0; i < value; i++)
        {
            if (Random.value < Config.infected_plant_fruit_drop_rate) RuntimeReward.GetInfectedFruit(1);
            SettleReward.GetNote(1, 1);
            if (Random.value < Config.infected_plant_token_drop_rate) AddRandomCharacterToken();
            TryAddInfectionRuneDrop(Config.infected_plant_rune_drop_rate);
        }
        PushKillSummary();
    }

    private void OnKillOre(byte value)
    {
        KillSummary = AddKills(KillSummary, ore: value);
        for (int i = 0; i < value; i++)
        {
            RuntimeReward.GetFragment(GetOreFragmentDrop(false));
            if (Random.value < Config.plant_note_drop_rate) SettleReward.GetNote(2, 1);
        }
        PushKillSummary();
    }

    private void OnKillInfectedOre(byte value)
    {
        KillSummary = AddKills(KillSummary, infectedOre: value);
        for (int i = 0; i < value; i++)
        {
            RuntimeReward.GetInfectedFragment(GetOreFragmentDrop(true));
            SettleReward.GetNote(2, 1);
            if (Random.value < Config.infected_ore_token_drop_rate) AddRandomCharacterToken();
            TryAddInfectionRuneDrop(Config.infected_ore_rune_drop_rate);
        }
        PushKillSummary();
    }

    private void OnKillInfection(byte value)
    {
        KillSummary = AddKills(KillSummary, infection: value);
        for (int i = 0; i < value; i++)
        {
            SettleReward.GetExp(Config.kill_infection_exp);
            SettleReward.GetNote(3, 1);
            AddRandomImprintExp(Config.kill_infection_exp);
            TryAddRuneDrop(Random.value < Config.infection_drop_a_rune_rate ? SkillInfo.Quality.A : SkillInfo.Quality.S, 1f, true);
            if (Random.value < Config.infection_token_drop_rate_without_level) AddRandomCharacterToken();
        }
        PushKillSummary();
    }

    private void OnPvpKillReward(SCPvpKillRewardInfo info)
    {
        pvpVictimTypes.Add(info.victimType);
        if (info.victimType.category == EntityCategory.Character)
        {
            SettleReward.GetCharacterToken(info.victimType.value, 1);
        }
        foreach (var rune in info.runes)
        {
            if (rune.Value <= 0) continue;
            AddRune(pvpRunes, rune.Key, rune.Value);
        }
    }

    private static void AddRunes(Dictionary<int, int> target, IReadOnlyDictionary<int, int> source)
    {
        foreach (var rune in source)
        {
            AddRune(target, rune.Key, rune.Value);
        }
    }

    private static void AddRune(Dictionary<int, int> target, int skillId, int count)
    {
        if (count <= 0) return;
        target.TryGetValue(skillId, out var current);
        target[skillId] = current + count;
    }

    /// <summary>撤离带出负重上限：迷失 0 / 初级 60 / 中级 120 / 高级 200。</summary>
    private static int GetExitCarryWeightLimit(ExitMode mode) => mode switch
    {
        ExitMode.Max => Config.exit_max_carry_weight_limit,
        ExitMode.Normal => Config.exit_normal_carry_weight_limit,
        ExitMode.Min => Config.exit_min_carry_weight_limit,
        _ => 0
    };

    /// <summary>
    /// 按撤离负重上限裁剪携带符文。超重时逐份丢弃，丢弃优先级：
    /// ①品质更低 ②同品质数量更少 ③同品质同数量 skillId 更小。
    /// </summary>
    private static void TrimCarriedRunesByWeight(Dictionary<int, int> carriedRunes, int weightLimit)
    {
        if (weightLimit <= 0)
        {
            carriedRunes.Clear();
            return;
        }
        int total = 0;
        foreach (var kv in carriedRunes) total += SkillManager.GetSkillWeight(kv.Key) * kv.Value;
        if (total <= weightLimit) return;

        while (total > weightLimit && carriedRunes.Count > 0)
        {
            int bestId = -1;
            foreach (var kv in carriedRunes)
            {
                if (kv.Value <= 0) continue;
                if (bestId < 0) { bestId = kv.Key; continue; }
                var qa = SkillManager.GetSkillQuality(kv.Key);
                var qb = SkillManager.GetSkillQuality(bestId);
                if (qa < qb) { bestId = kv.Key; continue; }
                if (qa > qb) continue;
                if (kv.Value < carriedRunes[bestId]) { bestId = kv.Key; continue; }
                if (kv.Value == carriedRunes[bestId] && kv.Key < bestId) bestId = kv.Key;
            }
            if (bestId < 0) break;

            carriedRunes[bestId]--;
            total -= SkillManager.GetSkillWeight(bestId);
            if (carriedRunes[bestId] <= 0) carriedRunes.Remove(bestId);
        }
    }

    private void PickUnlockProp(InteractablePropInfo.CharacterUnlockProp prop)
    {
        pickedUnlockProps.Add(prop);
        int index = GetUnlockPropIndex(prop);
        if (index >= 0) SettleReward.GetCharacterUnlock(index);
    }

    private void PushKillSummary()
    {
        EventManager.TrigEvent(ClientEvent.UpdateBattleKillSummary, KillSummary);
    }

    private static BattleKillSummary AddKills(BattleKillSummary summary, int player = 0, int zombie = 0, int plant = 0,
        int infectedPlant = 0, int ore = 0, int infectedOre = 0, int infection = 0)
    {
        summary.player += player;
        summary.zombie += zombie;
        summary.plant += plant;
        summary.infectedPlant += infectedPlant;
        summary.ore += ore;
        summary.infectedOre += infectedOre;
        summary.infection += infection;
        return summary;
    }

    private void AddRandomImprintExp(int count)
    {
        if (count <= 0 || Config.imprint_count <= 0) return;
        SettleReward.GetImprint(Random.Range(0, Config.imprint_count), Mathf.CeilToInt(count * Config.kill_imprint_exp_rate));
    }

    private void AddRandomCharacterToken()
    {
        SettleReward.GetCharacterToken(Random.Range(0, Config.character_count), 1);
    }

    private void TryAddRuneDrop(SkillInfo.Quality quality, float rate, bool infectionOnly)
    {
        if (rate <= 0f || Random.value >= rate) return;
        int skillId = GetRandomSkillId(quality, infectionOnly);
        if (skillId >= 0) SettleReward.GetRune(skillId, 1);
    }

    private void TryAddInfectionRuneDrop(float rate)
    {
        if (rate <= 0f || Random.value >= rate) return;
        int skillId = GetRandomInfectionSkillId();
        if (skillId >= 0) SettleReward.GetRune(skillId, 1);
    }

    private static int GetRandomSkillId(SkillInfo.Quality quality, bool infectionOnly)
    {
        var skills = Tool.InfoManager == null ? null : Tool.InfoManager.SkillInfoList;
        if (skills == null) return -1;

        List<int> candidates = new();
        for (int i = 0; i < skills.Count && i < Config.skill_count; i++)
        {
            var info = skills[i];
            if (info == null || info.quality != quality) continue;
            if (infectionOnly && !info.infectionSkill) continue;
            candidates.Add(i);
        }
        return candidates.Count == 0 ? -1 : candidates[Random.Range(0, candidates.Count)];
    }

    private static int GetRandomInfectionSkillId()
    {
        var skills = Tool.InfoManager == null ? null : Tool.InfoManager.SkillInfoList;
        if (skills == null) return -1;

        List<int> candidates = new();
        for (int i = 0; i < skills.Count && i < Config.skill_count; i++)
        {
            if (skills[i] != null && skills[i].infectionSkill) candidates.Add(i);
        }
        return candidates.Count == 0 ? -1 : candidates[Random.Range(0, candidates.Count)];
    }

    private static int GetOreFragmentDrop(bool infected)
    {
        float roll = Random.value;
        if (infected)
        {
            if (roll < 0.4f) return 0;
            return roll < 0.8f ? 1 : 2;
        }
        if (roll < 0.5f) return 0;
        return roll < 0.85f ? 1 : 2;
    }

    private static int GetUnlockPropIndex(InteractablePropInfo.CharacterUnlockProp prop)
    {
        if (prop == InteractablePropInfo.CharacterUnlockProp.None) return -1;
        int value = (int)prop;
        for (int i = 0; i < Config.character_count; i++)
        {
            if (value == (1 << i)) return i;
        }
        return -1;
    }
}
