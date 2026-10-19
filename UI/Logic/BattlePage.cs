using Ros.UI.Main;
using System.Collections.Generic;
using Ros.Info;
using Ros.Transport;
using FairyGUI;
using UnityEngine;

public class BattlePage : PageBase
{
    private readonly UI_BattlePanel panel;
    private float battleStartTime;

    private readonly List<SCEntityDisplayInfo.SkillSlotRuntime> skillSummary = new(); // 渲染器按索引读取的技能摘要缓存（仅技能 id）
    private readonly Dictionary<int, float> localCdEnds = new(); // 本地冷却模拟：skillId → 结束时刻（含延迟余量）
    private float skillCdRefreshTimer;   // 冷却环重渲染节流（本地模拟需要周期刷新）
    private readonly List<int> skillOutlineIndex = new(); // 各技能槽的边框样式（0~4）：进入战斗时一次性随机，之后不再变
    private int skillSlotCount;                           // 技能栏条目数 = 本地角色技能槽位数（进入战斗时确定）
    private bool skillSlotsBuilt;                         // 技能栏骨架是否已搭好（搭好前渲染器不读数据）
    private readonly Dictionary<ushort, UI_PlayerName> nameLabels = new();
    private readonly Dictionary<ushort, UI_EntityBar> entityBars = new();
    private readonly Dictionary<ushort, int> barOwners = new();
    private readonly Dictionary<ushort, UI_MinimapItem> minimapItems = new();
    private readonly Dictionary<ushort, float> minimapLastReceived = new();
    private readonly List<ushort> s_expiredMinimapIds = new();
    private UI_MinimapItem selfMinimapItem; //自身点位：固定在雷达中心且永远置顶
    private float minimapViewRadius = Config.minimap_view_radius; //当前雷达显示半径（服务器权威，F 键切换）
    private float minimapItemScale = 1f; // 抵消 Minimap 等上层元素缩放（不含页面根 UiScale 适配），使图标按 FGUI 设计尺寸显示；界面初始化时算
    private GLoader minimapMapLoader;    // 遮罩内的地形底图：xy/尺寸每帧随玩家位置与雷达半径换算
    private NTexture minimapMapTexture;  // 缓存 NTexture：包一层 AssetsManager.MapTexture，重复 Enter 不重建
    private Texture2D mapAreaTexture;    // AssetsManager.MapArea 的 Texture2D 引用（贴图需开 Read/Write）
    private int localPlayerArea = -1;    // 本地玩家当前区域（EventBanner 页 index，-1=未知；死亡观战期间冻结）
    private readonly List<DamageLabelItem> damageLabels = new();
    private readonly List<EventEntry> eventEntries = new(); // 事件列表数据源（渲染器按索引读取）
    private int localPlayerCamp = -1; // 本地玩家阵营（随本地实体摘要更新；小地图敌我识别用）
    private bool spectatorMode;       // 纯观战方玩家（无战斗实体）
    private bool localPlayerAlive = true; // 本地玩家实体是否在场（死亡观战期间 false，显示观战下拉）
    private int spectateTargetClientId = -1;  // 当前观战目标玩家 id（-1 = 未观战）
    private int spectateTargetEntityId = -1;  // 观战目标实体 id（-1 = 等待其表现数据到达后绑定相机）
    private float spectatePendingSince;   // 发起观战的时刻（目标数据迟迟不到 = 目标已死，超时重选）
    private float onlookRefreshTimer;     // 观战下拉选项刷新节流
    private bool syncingOnlookCombo;      // 程序刷新下拉选项期间屏蔽 onChanged 回调
    private readonly Dictionary<int, int> playerEntities = new(); // 玩家 id → 实体 id（-1 = 无实体），来自 SCPlayerEntity
    private readonly Dictionary<int, int> entityOwners = new();   // 实体 id → 玩家 id（名牌反查用）

    private const float DamageLife = 0.8f;
    private const float EventLife = 3.5f;
    private const int SkillOutlineVariants = 5;    // UI_SkillListItem.m_randomOutline 的档位数（0~4）

    private static readonly Color CampAttackColor = new Color(1f, 0.45f, 0.4f);
    private static readonly Color CampDefenseColor = new Color(0.4f, 0.72f, 1f);

    // 本局会话数据全部来自加载期（不再读组队缓存）
    private static ClientLoadingManager Session => ClientLoadingManager.Current;
    private static bool HasSession => Session != null && Session.HasSession;
    private static ushort OwnEntityId => HasSession ? Session.OwnEntityId : (ushort)0;
    private bool IsOwnEntity(int entityId) => HasSession && OwnEntityId == entityId;

    // 战斗期名字一律取加载期下发的表（不再读组队期缓存）
    private static string GetUnitName(int clientId) => HasSession ? Session.GetName(clientId) : $"玩家{clientId}";

    private struct EventEntry
    {
        public int type;     // UI_EventItem 的 type 控制器：0 纯文字 / 1 图标+文字 / 2 文字+图标+文字
        public string text;  // type 0/1 的文本
        public int icon;     // type 1/2 的 EventIcon 档位
        public string left;  // type 2 左侧文本（击杀者）
        public string right; // type 2 右侧文本（受害者）
        public float time;   // 入列时间；超过 EventLife 的条目从列表头部移除
    }

    private struct DamageLabelItem
    {
        public UI_DamageLabel label;
        public float time;
        public Vector3 anchor;  // 世界锚点（不含散布），每帧据此重投影
        public Vector2 scatterPx; // 屏幕空间随机散布（创建时定死，避免每帧重掷抖动）
        public float risePx;    // 屏幕上升累计像素
    }

    public BattlePage(UI_BattlePanel panel) : base(panel)
    {
        this.panel = panel;
    }

    public override void Construct()
    {
        panel.m_btn_exit.onClick.Add(OnExitClicked); // 退出需二次确认（对局中退出不可逆）
        panel.m_showRegenerationBar.selectedIndex = 0; // 复活进度默认隐藏
        ComputeMinimapItemScale();
    }

    //界面初始化时算：抵消 Minimap 等上层元素（不含页面根 UiScale 适配）的缩放，使图标最终按 FGUI 设计尺寸显示
    private void ComputeMinimapItemScale()
    {
        float upper = 1f;
        for (GObject o = panel.m_Minimap; o != null && o != Root; o = o.parent)
        {
            if (o.scale.x > 0.0001f) upper *= o.scale.x;
        }
        minimapItemScale = upper > 0.0001f ? 1f / upper : 1f;
    }

    public override void Enter(ShowParam param)
    {
        base.Enter(param);
        EventManager.AddEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
        EventManager.AddEvent<int>(ClientEvent.OnEntityDisplayRemove, OnEntityDisplayRemove);
        EventManager.AddEvent<SCMinimapEntity>(ClientEvent.OnMinimapUpdate, OnMinimapUpdate);
        EventManager.AddEvent<float>(ClientEvent.OnMinimapRadiusUpdate, OnMinimapRadiusUpdate);
        EventManager.AddEvent<SCScoreInfo>(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.AddEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.AddEvent<SCDamage>(ClientEvent.OnDamageDisplay, OnDamageDisplay);
        EventManager.AddEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.AddEvent<SCCampAliveCount>(ClientEvent.OnCampAliveCount, OnCampAliveCount);
        EventManager.AddEvent<SCPlayerEntity>(ClientEvent.OnPlayerEntityUpdate, OnPlayerEntityUpdate);
        playerEntities.Clear();
        entityOwners.Clear();
        if (Tool.CameraController != null) Tool.CameraController.OnCameraUpdated += OnCameraUpdated;

        battleStartTime = Time.time;
        localPlayerCamp = -1;
        spectatorMode = Session != null && Session.IsSpectator;
        localPlayerAlive = !spectatorMode;
        localPlayerArea = -1;
        panel.m_onlook.selectedIndex = spectatorMode ? 1 : 0; // onlook=1：FGUI 自动隐藏血条/技能等非观战元素
        if (panel.m_onlookComboBox != null) panel.m_onlookComboBox.onChanged.Set(OnOnlookComboBoxChanged);
        RefreshCampAliveCount(NetworkManager.LatestCampAliveCount); // 进入战斗先填一次缓存值，随后由变更包刷新
        minimapViewRadius = Config.minimap_view_radius; //每局回到默认档，与服务器 ClearBattleState 对齐
        // 首页/大厅展示的选角预览模型只属于那两个界面，进战斗前清掉（否则会残留在地图的预览锚点上）
        if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.HomePreview != null)
            Tool.ClientLogicManager.HomePreview.Hide();
        ResetBeacons();
        ClearEntityBars();
        ClearMinimap();
        EnsureMinimapMapLoader();
        ClearEventItems();
        BuildSkillSlots(HasSession ? Session.Camp : EntityCamp.None);
        RefreshTimeIcon();
    }

    public override void Exit()
    {
        base.Exit();
        EventManager.RemoveEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
        EventManager.RemoveEvent<int>(ClientEvent.OnEntityDisplayRemove, OnEntityDisplayRemove);
        EventManager.RemoveEvent<SCMinimapEntity>(ClientEvent.OnMinimapUpdate, OnMinimapUpdate);
        EventManager.RemoveEvent<float>(ClientEvent.OnMinimapRadiusUpdate, OnMinimapRadiusUpdate);
        EventManager.RemoveEvent<SCScoreInfo>(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.RemoveEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.RemoveEvent<SCDamage>(ClientEvent.OnDamageDisplay, OnDamageDisplay);
        EventManager.RemoveEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.RemoveEvent<SCCampAliveCount>(ClientEvent.OnCampAliveCount, OnCampAliveCount);
        EventManager.RemoveEvent<SCPlayerEntity>(ClientEvent.OnPlayerEntityUpdate, OnPlayerEntityUpdate);
        playerEntities.Clear();
        entityOwners.Clear();
        if (Tool.CameraController != null) Tool.CameraController.OnCameraUpdated -= OnCameraUpdated;
        spectatorMode = false;
        localPlayerAlive = true;
        spectateTargetClientId = -1;
        spectateTargetEntityId = -1;
        if (panel.m_onlook != null) panel.m_onlook.selectedIndex = 0;
    }

    public override void Tick(float deltaTime)
    {
        //剩余时间（本地估算，精确值以服务器 SCScoreInfo 为准）
        if (HasSession && panel.m_label_time_left != null)
        {
            float remain = Config.battle_duration - (Time.time - battleStartTime);
            panel.m_label_time_left.text = FormatTime(Mathf.Max(0f, remain));
        }
        RefreshTimeIcon();
        TickDamageLabels(deltaTime);
        TickEventItems();
        TickMinimapTimeout();
        TickSelfMinimapRotation();
        TickMinimapMapImage();
        TickSpectator(deltaTime);
        TickLocalSkillCd(deltaTime);
    }

    // 本地冷却模拟：按下技能键即开始（时长 = 服务器 CD + 延迟余量），周期重渲染冷却环
    private void TickLocalSkillCd(float deltaTime)
    {
        if (!skillSlotsBuilt || spectatorMode) return;
        for (int i = 0; i < Config.skill_slot_keys.Length; i++)
        {
            if (!Input.GetKeyDown(Config.skill_slot_keys[i])) continue;
            int skillId = i < skillSummary.Count && skillSummary[i] != null ? skillSummary[i].skillId : -1;
            if (skillId >= 0)
                localCdEnds[skillId] = Time.time + SkillManager.GetSkillCD(skillId) + Mathf.Max(0f, EnsInstance.LocalClientDelay / 1000f);
        }
        skillCdRefreshTimer -= deltaTime;
        if (skillCdRefreshTimer > 0f) return;
        skillCdRefreshTimer = 0.1f;
        var list = panel.m_skillList != null ? panel.m_skillList.m_content : null;
        if (list != null) list.numItems = skillSlotCount; // 触发原地重渲染：冷却环按本地模拟推进
    }

    #region 事件处理
    private void OnEntityDisplayUpdate(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        if (info.type.category == EntityCategory.Beacon)
        {
            OnBeaconDisplay(info);
        }
        else if (info.type.category == EntityCategory.Character_Attack ||
                 info.type.category == EntityCategory.Character_Defense)
        {
            UpdateEntityBar(info);
        }

        if (IsOwnEntity(info.entityId))
        {
            localPlayerCamp = (int)info.camp;
            localPlayerAlive = true;
            spectateTargetClientId = -1; // 本地实体出现（复活/正常出战）：服务器已退出观战，视角回自身
            spectateTargetEntityId = -1;
            UpdateLocalPlayerBar(info);
            OnLocalSkillBarUpdate(info);
        }
    }

    private void OnEntityDisplayRemove(int entityId)
    {
        RemoveEntityBar((ushort)entityId);
        RemoveMinimapItem((ushort)entityId);

        if (IsOwnEntity(entityId))
            localPlayerAlive = false; // 本地死亡：显示观战下拉，进入死亡观战
    }

    private void OnPlayerEntityUpdate(SCPlayerEntity info)
    {
        if (info == null) return;
        // 旧实体关系清理（复活的玩家会拿到新实体 id）
        if (playerEntities.TryGetValue(info.clientId, out var old) && old != -1
            && entityOwners.TryGetValue(old, out var oc) && oc == info.clientId)
            entityOwners.Remove(old);
        playerEntities[info.clientId] = info.entityId;
        if (info.entityId != -1) entityOwners[info.entityId] = info.clientId;

        // 正在观战的目标关系变化：-1 = 目标死亡（纯观战自动重选 / 死亡观战冻结原地），新 id = 重绑相机
        if (spectateTargetClientId == info.clientId)
        {
            spectateTargetEntityId = info.entityId;
            if (info.entityId == -1) spectateTargetClientId = -1;
        }
    }

    private int EntityOwnerOf(ushort entityId) => entityOwners.TryGetValue(entityId, out var c) ? c : -1;

    #region 观战
    private void TickSpectator(float deltaTime)
    {
        var box = panel.m_onlookComboBox;
        if (box == null) return;
        box.visible = spectatorMode || !localPlayerAlive; // 观战目标下拉：纯观战/死亡观战可见

        // 纯观战未选目标（进场自动选 / 目标死亡后自动重选）：随机挑一名攻/守成员
        if (spectatorMode && spectateTargetClientId == -1)
        {
            SpectateTo(PickRandomCombatMember());
            return;
        }
        // 目标表现数据迟迟不到 = 目标已死亡（服务器静默拒绝），超时重选/复位
        if (spectateTargetClientId != -1 && spectateTargetEntityId == -1
            && spectatePendingSince > 0f && Time.time - spectatePendingSince > 3f)
        {
            spectateTargetClientId = -1;
            spectatePendingSince = 0f;
        }
        // 目标表现数据已到达：绑定相机到其视图锚点（锚点由视图逐帧刷新）
        if (spectateTargetClientId != -1 && spectateTargetEntityId == -1
            && playerEntities.TryGetValue(spectateTargetClientId, out var eid) && eid != -1)
        {
            spectateTargetEntityId = eid;
            spectatePendingSince = 0f;
        }
        if (spectateTargetEntityId != -1 && Tool.CameraController != null
            && Tool.ClientLogicManager?.EntityPlayers != null)
        {
            var anchor = Tool.ClientLogicManager.EntityPlayers.GetOrCreateCameraAnchor((ushort)spectateTargetEntityId);
            if (anchor != null && Tool.CameraController.lookTarget != anchor)
                Tool.CameraController.SetLookTarget(anchor);
        }

        onlookRefreshTimer -= deltaTime;
        if (onlookRefreshTimer <= 0f)
        {
            onlookRefreshTimer = 0.5f;
            RefreshOnlookComboBox();
        }
    }

    private void SpectateTo(int targetClientId)
    {
        if (targetClientId < 0) return;
        spectateTargetClientId = targetClientId;
        spectateTargetEntityId = playerEntities.TryGetValue(targetClientId, out var eid) ? eid : -1;
        spectatePendingSince = spectateTargetEntityId == -1 ? Time.time : 0f;
        NetworkManager.RequestSpectate(targetClientId);
    }

    private int PickRandomCombatMember()
    {
        if (!HasSession) return -1;
        var units = Session.GetCombatants();
        if (units == null || units.Count == 0) return -1;
        return units[Random.Range(0, units.Count)].clientId;
    }

    private void RefreshOnlookComboBox()
    {
        var box = panel.m_onlookComboBox;
        if (box == null) return;
        var names = new List<string>();
        var values = new List<string>();
        if (HasSession)
        {
            var units = Session.GetCombatants();
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null) continue;
                if (unit.clientId == EnsInstance.LocalClientId) continue; // 自己不进观战列表
                names.Add(Session.GetName(unit.clientId));
                values.Add(unit.clientId.ToString());
            }
        }
        syncingOnlookCombo = true;
        box.values = values.ToArray();
        box.items = names.ToArray();
        box.selectedIndex = values.IndexOf(spectateTargetClientId.ToString());
        syncingOnlookCombo = false;
    }

    private void OnOnlookComboBoxChanged()
    {
        if (syncingOnlookCombo) return;
        var box = panel.m_onlookComboBox;
        if (box == null || box.selectedIndex < 0 || box.values == null
            || box.selectedIndex >= box.values.Length) return;
        if (int.TryParse(box.values[box.selectedIndex], out var target)) SpectateTo(target);
    }
    #endregion

    private void OnLocalSkillBarUpdate(SCEntityDisplayInfo info)
    {
        if (!info.hasSkills) return; // 无技能变化的包不重建摘要（保留本地冷却状态）
        var list = panel.m_skillList != null ? panel.m_skillList.m_content : null;
        if (list == null) return;
        skillSummary.Clear();
        if (info.skills != null) skillSummary.AddRange(info.skills);
        if (!skillSlotsBuilt && HasSession) BuildSkillSlots(Session.Camp); // 兜底：进战斗时若槽位数取不到（理论不会），首包摘要时再搭一次
        // 数量与边框样式已定，这里重新设置 numItems 只是按新数据原地重渲染（FGUI 规定的刷新方式，不改变条目数）
        list.numItems = skillSlotCount;
    }

    private void BuildSkillSlots(EntityCamp camp)
    {
        var list = panel.m_skillList != null ? panel.m_skillList.m_content : null;
        if (list == null) return;
        int capacity = ResolveSkillSlotCapacity(camp);
        if (capacity <= 0) capacity = Mathf.Max(1, skillSummary.Count); // 取不到属性时退化为"已持有技能数"，至少露出一格
        skillSlotCount = capacity;
        skillOutlineIndex.Clear();
        for (int i = 0; i < capacity; i++) skillOutlineIndex.Add(Random.Range(0, SkillOutlineVariants));
        list.itemRenderer = RenderSkillSlot;
        list.numItems = capacity; // 非虚拟列表会同步建出全部条目并回调渲染器
        RefreshSkillListWidth(list);
        skillSlotsBuilt = true;
    }

    private static void RefreshSkillListWidth(GList list)
    {
        float width = 0f;
        for (int i = 0; i < list.numChildren; i++)
        {
            var child = list.GetChildAt(i);
            if (child != null) width += child.width;
        }
        list.width = width;
    }

    private void RenderSkillSlot(int index, GObject obj)
    {
        if (obj is not UI_SkillListItem slot) return;
        // 边框样式：进入战斗时随好，之后每次刷新都用同一个
        if (slot.m_randomOutline != null)
            slot.m_randomOutline.selectedIndex = index < skillOutlineIndex.Count ? skillOutlineIndex[index] : 0;
        // 键位：槽位顺序即 Config.skill_slot_keys（U I O L H Y）
        if (slot.m_key != null)
            slot.m_key.text = index < Config.skill_slot_keys.Length ? Config.skill_slot_keys[index].ToString() : "";

        // 该槽位还没拿到技能（服务器摘要还没下发，或这个槽位本来就是空的）
        var data = index < skillSummary.Count ? skillSummary[index] : null;
        bool empty = data == null || data.skillId < 0;
        if (slot.m_empty != null) slot.m_empty.selectedIndex = empty ? 1 : 0;
        if (empty) ClearSkillSlot(slot);
        else
        {
            RefreshSkillSlot(slot, data);
        }
    }

    private static void ClearSkillSlot(UI_SkillListItem slot)
    {
        if (slot.m_loader_icon != null)
        {
            slot.m_loader_icon.texture = null;
            slot.m_loader_icon.visible = false;
            slot.m_loader_icon.fillAmount = 1f;
        }
        if (slot.m_loader_iconBase != null)
        {
            slot.m_loader_iconBase.texture = null;
            slot.m_loader_iconBase.visible = false;
        }
        if (slot.m_store != null) slot.m_store.text = "";
    }

    private void OnBeaconDisplay(SCEntityDisplayInfo info)
    {
        UI_DefensivePointBar bar;
        if (info.type == EntityType.CoreBeacon)
        {
            bar = panel.m_progressMain;
        }
        else
        {
            int index = info.type.value; // 外围守护点：Beacon(0~2) → Sub1~3
            if (index < 0 || index > 2) return;
            bar = index == 0 ? panel.m_progressSub1 : index == 1 ? panel.m_progressSub2 : panel.m_progressSub3;
        }
        if (bar == null) return;
        bool destroyed = info.health <= 0;
        bar.m_destroyed.selectedIndex = destroyed ? 1 : 0;
        bar.m_fill.scale = new Vector2(destroyed ? 1f : (info.maxHealth > 0 ? Mathf.Clamp01((float)info.health / info.maxHealth) : 1f), bar.m_fill.scale.y);
    }

    private void ResetBeacons()
    {
        ResetBeacon(panel.m_progressMain);
        ResetBeacon(panel.m_progressSub1);
        ResetBeacon(panel.m_progressSub2);
        ResetBeacon(panel.m_progressSub3);
    }

    private static void ResetBeacon(UI_DefensivePointBar bar)
    {
        if (bar == null) return;
        bar.m_destroyed.selectedIndex = 0;
        bar.m_fill.scale = new Vector2(0f, bar.m_fill.scale.y);
    }

    // 守护点被摧毁：据事件携带的 beacon 标识定位血条，切到已摧毁外观并清空填充
    // beaconId：-1=中心守护点(main)，0~2=外围 Sub1~3（与 OnBeaconDisplay 的 index 映射一致）
    private void SetBeaconDestroyed(int textId)
    {
        UI_DefensivePointBar bar = textId == 19 ? panel.m_progressMain
            : textId == 24 ? panel.m_progressSub1
            : textId == 25 ? panel.m_progressSub2 : textId == 26 ? panel.m_progressSub3 : null;
        if (bar == null) return;
        bar.m_destroyed.selectedIndex = 1;
        bar.m_fill.scale = new Vector2(1f, bar.m_fill.scale.y);
    }

    private void UpdateLocalPlayerBar(SCEntityDisplayInfo info)
    {
        var bar = panel.m_PlayerBar;
        if (bar == null) return;
        if (bar.m_bar_fill.data == null) bar.m_bar_fill.data = bar.m_bar_fill.width; // 首次记录满血宽度
        float fullWidth = bar.m_bar_fill.data is float w ? w : bar.m_bar_fill.width;
        bar.m_bar_fill.width = fullWidth * (info.maxHealth > 0 ? Mathf.Clamp01((float)info.health / info.maxHealth) : 0f);
        if (bar.m_label_health != null) bar.m_label_health.text = $"{info.health}/{info.maxHealth}";
        if (bar.m_label_level != null)
        {
            // 本地玩家等级：按自己阵营取所选角色的局外等级（只显示数字，不带 "Lv"）
            int saveIndex = GetLocalSaveIndex(info.camp);
            int level = saveIndex < 0 || Tool.SaveManager == null ? 1 : Tool.SaveManager.GetCharacterLevel(saveIndex);
            bar.m_label_level.text = level.ToString();
        }
    }

    private static int GetLocalSaveIndex(EntityCamp camp)
    {
        if (camp == EntityCamp.Attack) return ClientSelection.selectedAttackIndex;
        if (camp == EntityCamp.Defense) return Config.attack_character_count + ClientSelection.selectedDefenseIndex;
        return -1;
    }

    private static int ResolveSkillSlotCapacity(EntityCamp camp)
    {
        int saveIndex = GetLocalSaveIndex(camp);
        if (saveIndex < 0 || Tool.InfoManager == null || Tool.SaveManager == null) return 0;
        var characterInfo = Tool.InfoManager.GetPlayerCharacterInfo(saveIndex);
        if (characterInfo == null) return 0;
        return characterInfo.GetAttribute(Tool.SaveManager.GetCharacterLevel(saveIndex)).weaponSlotCount;
    }

    private void OnScoreUpdate(SCScoreInfo info)
    {
        if (info == null) return;
        if (panel.m_label_time_left != null) panel.m_label_time_left.text = FormatTime(Mathf.Max(0f, info.remainTime));
    }

    private void OnCampAliveCount(SCCampAliveCount info) => RefreshCampAliveCount(info);

    private void RefreshCampAliveCount(SCCampAliveCount info)
    {
        if (info == null) return;
        if (panel.m_attacker_member_count != null)
            panel.m_attacker_member_count.text = $"{info.attackerAlive}/{info.attackerTotal}";
        if (panel.m_defense_member_count != null)
            panel.m_defense_member_count.text = $"{info.defenseAlive}/{info.defenseTotal}";
        if (panel.m_spectater_member_count != null)
            panel.m_spectater_member_count.text = info.spectatorCount.ToString();
    }

    private void OnMinimapUpdate(SCMinimapEntity e)
    {
        if (e == null) return;
        // 分帧累积：仅记录收到时间并按需显示，不再每帧清空（离屏/夜间停传由超时剔除处理）
        minimapLastReceived[e.entityId] = Time.time;
        UpsertMinimapEntity(e);
    }

    //F 键切换雷达显示半径：服务器权威回应，客户端换用新半径做裁剪与缩放（旧点位在后续包到达时自然重排）
    private void OnMinimapRadiusUpdate(float radius)
    {
        if (radius <= 0f) return;
        minimapViewRadius = radius;
    }

    private void UpsertMinimapEntity(SCMinimapEntity entity)
    {
        var mapBase = panel.m_Minimap != null ? panel.m_Minimap.m_mapBase : null;
        if (mapBase == null) return;

        // 雷达式小地图：以本地玩家为中心；拿不到自身位置就无法换算，跳过本次
        if (!HasSession || Tool.ClientLogicManager == null
            || Tool.ClientLogicManager.EntityPlayers == null
            || !Tool.ClientLogicManager.EntityPlayers.TryGetEntityPosition(OwnEntityId, out var myPos)) return;

        float dx = entity.posX - myPos.x, dz = entity.posZ - myPos.z;
        // 只画雷达半径内：超出显示半径的点位直接移除（服务器圈外本就过滤，回到圈内会重建）
        float cullRadiusSq = minimapViewRadius * minimapViewRadius;
        if (dx * dx + dz * dz > cullRadiusSq)
        {
            RemoveMinimapItem(entity.entityId);
            minimapLastReceived.Remove(entity.entityId);
            return;
        }

        bool isSelf = IsOwnEntity(entity.entityId);
        bool isNew = !minimapItems.TryGetValue(entity.entityId, out var item);
        if (isNew)
        {
            item = UI_MinimapItem.CreateInstance();
            item.scale = new Vector2(minimapItemScale, minimapItemScale); // 抵消上层缩放，恢复 FGUI 设计尺寸
            panel.m_Minimap.AddChild(item); //GGraph 不是容器，点位挂在 Minimap 面板上
            minimapItems[entity.entityId] = item;
            if (isSelf) selfMinimapItem = item;
        }
        item.m_type.selectedIndex = GetMinimapType(entity);
        item.m_highlight.selectedIndex = entity.marked ? 1 : 0; //白眼标记高亮（服务器随包下发，出现/消失都靠每包同步）
        //雷达坐标：上方=世界Z+、右侧=世界X+，当前雷达显示半径铺满 mapBase；减半宽高让图标几何居中
        float scale = mapBase.width * 0.5f / minimapViewRadius;
        item.SetXY(mapBase.x + mapBase.width * 0.5f + dx * scale - item.width * 0.5f,
            mapBase.y + mapBase.height * 0.5f - dz * scale - item.height * 0.5f);
        RaiseSelfItemToTop(); //新点加入可能盖住自身图标，每次都校一次层级
    }

    //自身图标永远置顶，不被其它点位遮挡
    private void RaiseSelfItemToTop()
    {
        if (selfMinimapItem == null || selfMinimapItem.isDisposed) return;
        panel.m_Minimap.SetChildIndex(selfMinimapItem, panel.m_Minimap.numChildren - 1);
    }

    //自身图标随朝向旋转：地图不转，图标转（每帧读本地玩家推演朝向）
    private void TickSelfMinimapRotation()
    {
        if (selfMinimapItem == null || selfMinimapItem.isDisposed) return;
        if (!HasSession || Tool.ClientLogicManager == null
            || Tool.ClientLogicManager.EntityPlayers == null) return;
        if (!Tool.ClientLogicManager.EntityPlayers.TryGetEntityTransform(OwnEntityId, out _, out var rot)) return;
        selfMinimapItem.rotation = rot.eulerAngles.y; //FGUI 顺时针角度与 Unity yaw 同向（图标默认朝上时）
    }

    private int GetMinimapType(SCMinimapEntity entity)
    {
        switch (entity.type.category)
        {
            case EntityCategory.Character_Attack:
            case EntityCategory.Character_Defense:
                if (IsOwnEntity(entity.entityId)) return 0;
                return entity.camp == (EntityCamp)localPlayerCamp ? 1 : 2;
            case EntityCategory.PlagueTree: return 3;
            case EntityCategory.Crystal: return 4;
            case EntityCategory.Tower: return 5;
            case EntityCategory.Zombie: return 6;
            case EntityCategory.EliteZombie: return 7;
            case EntityCategory.Beacon: return entity.type == EntityType.CoreBeacon ? 8 : 9;
            default:
                Debug.LogError($"[Minimap] 未处理的实体类别：{entity.type.category}");
                return 7;
        }
    }

    private void RemoveMinimapItem(ushort entityId)
    {
        if (minimapItems.TryGetValue(entityId, out var item))
        {
            if (item != null) item.Dispose();
            minimapItems.Remove(entityId);
            if (item == selfMinimapItem) selfMinimapItem = null;
        }
    }

    private void ClearMinimap()
    {
        foreach (var item in minimapItems.Values)
        {
            if (item != null) item.Dispose();
        }
        minimapItems.Clear();
        minimapLastReceived.Clear();
        selfMinimapItem = null;
    }

    // 遮罩内创建地形底图 Loader：只建一次，包被重建时自动补建；图片显示由 TickMinimapMapImage 每帧驱动
    private void EnsureMinimapMapLoader()
    {
        var minimap = panel.m_Minimap;
        if (minimap?.m_mask == null) return;
        if (minimapMapLoader != null && !minimapMapLoader.isDisposed && minimapMapLoader.parent == minimap.m_mask) return;
        minimapMapLoader = new GLoader();
        minimapMapLoader.fill = FillType.Scale; // 地形图与目标区域同为正方形，直接拉伸铺满
        minimap.m_mask.AddChild(minimapMapLoader);
        if (minimapMapTexture == null && Tool.AssetsManager != null && Tool.AssetsManager.MapTexture != null)
            minimapMapTexture = new NTexture(Tool.AssetsManager.MapTexture);
        minimapMapLoader.texture = minimapMapTexture;
    }

    // 地形底图对位：与点位同一套雷达换算（mapBase 半宽 = 显示半径米数），图左下=世界(0,0)、上=+Z
    private void TickMinimapMapImage()
    {
        var minimap = panel.m_Minimap;
        var mapBase = minimap != null ? minimap.m_mapBase : null;
        if (minimapMapLoader == null || minimapMapLoader.isDisposed || mapBase == null) return;
        if (!HasSession || Tool.ClientLogicManager == null
            || Tool.ClientLogicManager.EntityPlayers == null
            || !Tool.ClientLogicManager.EntityPlayers.TryGetEntityPosition(OwnEntityId, out var myPos)) return;
        TickLocalPlayerArea(myPos);

        float ppm = mapBase.width * 0.5f / minimapViewRadius; // 像素/米
        float size = Config.map_terrain_size * ppm;
        float maskX = minimap.m_mask != null ? minimap.m_mask.x : 0f; // Loader 挂在遮罩里，坐标需换算成遮罩局部
        float maskY = minimap.m_mask != null ? minimap.m_mask.y : 0f;
        // 图左上角对应世界 (0, 1000)：先按雷达坐标算它在 Minimap 坐标系的位置，再减遮罩偏移
        minimapMapLoader.SetXY(
            mapBase.x + mapBase.width * 0.5f - myPos.x * ppm - maskX,
            mapBase.y + mapBase.height * 0.5f - (Config.map_terrain_size - myPos.z) * ppm - maskY);
        minimapMapLoader.SetSize(size, size);
    }

    // EventBanner.m_type 控制器页序：0 城市 / 1 工厂区 / 2 野外 / 3 郊区 / 4 日出 / 5 日落
    private const int BannerCity = 0, BannerFactory = 1, BannerWild = 2, BannerSuburb = 3, BannerSunrise = 4, BannerSunset = 5;

    // 区域判定：采样 MapArea 贴图（图左下=世界(0,0)，上=+Z）；alpha<128=野外，否则 RGB 最大者 = r 郊区 / g 城区 / b 工厂区
    private void TickLocalPlayerArea(Vector3 myPos)
    {
        if (spectatorMode || !localPlayerAlive) return; // 死亡观战期间冻结区域，不更新
        if (mapAreaTexture == null && Tool.AssetsManager != null)
            mapAreaTexture = Tool.AssetsManager.MapArea as Texture2D;
        if (mapAreaTexture == null) return;

        float u = Mathf.Clamp01(myPos.x / Config.map_terrain_size);
        float v = Mathf.Clamp01(myPos.z / Config.map_terrain_size);
        Color c = mapAreaTexture.GetPixelBilinear(u, v);
        int area = c.a * 255f < 128f ? BannerWild
            : c.g >= c.r && c.g >= c.b ? BannerCity
            : c.r >= c.b ? BannerSuburb
            : BannerFactory;
        if (area == localPlayerArea) return;
        localPlayerArea = area;
        ShowEventBanner(area);
    }

    // 切 EventBanner 档位并播 BattlePanel 的显隐转场
    private void ShowEventBanner(int page)
    {
        var banner = panel.m_eventBanner;
        if (banner == null || banner.m_type == null) return;
        banner.m_type.selectedIndex = page;
        if (panel.m_showBanner != null) panel.m_showBanner.Play();
    }

    // 超时剔除：长时间未收到某实体点位包（离屏不再发送 / 夜间·致盲停传）则隐藏其小地图点
    private void TickMinimapTimeout()
    {
        if (minimapLastReceived.Count == 0) return;
        float now = Time.time;
        s_expiredMinimapIds.Clear();
        foreach (var kv in minimapLastReceived)
        {
            if (now - kv.Value > Config.minimap_entity_timeout) s_expiredMinimapIds.Add(kv.Key);
        }
        foreach (var id in s_expiredMinimapIds)
        {
            RemoveMinimapItem(id);
            minimapLastReceived.Remove(id);
        }
    }

    // 事件类型 → (EventIcon 档位, 条目版式)：FGUI 表现映射，协议里不传表现字段
    private void OnBattleEvent(SCBattleEvent e)
    {
        if (e == null) return;
        byte icon;
        byte layout;
        switch (e.type)
        {
            case SCBattleEvent.Type.PlayerKill: icon = 6; layout = 2; break;           // 文字+击败图标+文字
            case SCBattleEvent.Type.DeathUnattributed: icon = 0; layout = 1; break;    // 无源死亡
            case SCBattleEvent.Type.PlayerRespawn: icon = 2; layout = 1; break;        // 玩家复活
            case SCBattleEvent.Type.PlagueTreeCaptured: icon = 1; layout = 1; break;   // 瘟疫树被击败
            case SCBattleEvent.Type.PlagueTreeRespawn: icon = 3; layout = 1; break;    // 瘟疫树刷新
            case SCBattleEvent.Type.Nightfall: icon = 4; layout = 1; break;            // 天黑
            case SCBattleEvent.Type.Daybreak: icon = 5; layout = 1; break;             // 天亮
            case SCBattleEvent.Type.BeaconDestroyed: icon = 7; layout = 1; break;      // 其它（守护点）
            case SCBattleEvent.Type.TowerDestroyed: icon = 7; layout = 1; break;       // 其它（防御塔）
            case SCBattleEvent.Type.Notice: icon = 0; layout = 0; break;               // 纯文字
            default:
                Debug.LogError($"[BattlePage] 未处理的战斗事件类型：{e.type}");
                return;
        }

        var entry = new EventEntry
        {
            type = layout,
            icon = icon,
            text = ResolveEventText(e.textId),
        };
        if (layout == 2) // 文字+图标+文字
        {
            entry.left = entry.text;
            entry.right = ResolveEventText(e.textId2);
        }
        AddEventEntry(entry);
        if (e.type == SCBattleEvent.Type.BeaconDestroyed) SetBeaconDestroyed(e.textId);
        else if (e.type == SCBattleEvent.Type.Nightfall) ShowEventBanner(BannerSunset);       // 日落横幅
        else if (e.type == SCBattleEvent.Type.Daybreak) ShowEventBanner(BannerSunrise);       // 日出横幅
    }

    // 事件文本统一按数字 id 解析：>=PlayerNameBase 映射为玩家名，否则查 NoticeMessageMap
    private static string ResolveEventText(int id)
    {
        if (id >= SCBattleEvent.PlayerNameBase) return GetUnitName(id - SCBattleEvent.PlayerNameBase);
        return NoticeMessageMap.Get(id);
    }

    private void OnDamageDisplay(SCDamage d)
    {
        if (d == null) return;
        ShowDamage(d.value, (ushort)d.targetId, d.hasHitPos, d.hitPos);
    }

    private void OnReviveProgressUpdate(SCReviveInfo info)
    {
        if (info == null) return;
        bool isLocal = IsOwnEntity(info.entityId);
        if (!isLocal) return;
        panel.m_showRegenerationBar.selectedIndex = info.ready ? 0 : 1;
        if (info.ready) return;
        if (panel.m_regeneration_progressbar != null)
            panel.m_regeneration_progressbar.fillAmount = Mathf.Clamp01(info.progress);
    }
    #endregion

    #region//Local
    private void UpdateEntityBar(SCEntityDisplayInfo info)
    {
        bool isAttack = info.camp == EntityCamp.Attack;
        Color campColor = isAttack ? CampAttackColor : CampDefenseColor;

        if (!nameLabels.TryGetValue(info.entityId, out var nameItem))
        {
            nameItem = UI_PlayerName.CreateInstance();
            Root.AddChild(nameItem);
            nameLabels[info.entityId] = nameItem;
        }
        nameItem.visible = true;
        if (nameItem.m_num != null)
        {
            nameItem.m_num.text = GetUnitName(EntityOwnerOf(info.entityId));
            nameItem.m_num.color = campColor;
        }

        if (!entityBars.TryGetValue(info.entityId, out var barItem))
        {
            barItem = UI_EntityBar.CreateInstance();
            Root.AddChild(barItem);
            entityBars[info.entityId] = barItem;
        }
        barItem.visible = true;
        barItem.m_fill.fillAmount = info.maxHealth > 0 ? Mathf.Clamp01((float)info.health / info.maxHealth) : 0f;
        barItem.m_fill.color = campColor;
        if (barItem.m_label != null) barItem.m_label.text = $"{info.health}/{info.maxHealth}";
        barOwners[info.entityId] = EntityOwnerOf(info.entityId);
    }

    private Camera GetProjectionCamera()
    {
        return (Tool.CameraController != null && Tool.CameraController.WorldCamera != null)
            ? Tool.CameraController.WorldCamera : Camera.main;
    }

    private void UpdateEntityBarPositions()
    {
        var cam = GetProjectionCamera();
        if (cam == null) return;
        foreach (var pair in entityBars)
        {
            var bar = pair.Value;
            if (bar == null) continue;
            var headPos = Vector3.zero;
            bool visible = false;
            if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.EntityPlayers != null)
                visible = Tool.ClientLogicManager.EntityPlayers.TryGetAnchorPos(pair.Key, EntityAnchor.Bar, out headPos);
            Vector2 local = Vector2.zero;
            if (visible)
            {
                var screen = cam.WorldToScreenPoint(headPos);
                visible = screen.z > 0f;
                // Unity 屏幕 y 轴向上、FGUI 局部 y 轴向下，需翻转；UiScale 把屏幕像素换算成本页面板设计像素
                if (visible) local = new Vector2(screen.x / UiScale, (Screen.height - screen.y) / UiScale);
            }
            bar.visible = visible;
            if (visible) bar.xy = local - new Vector2(bar.width * 0.5f, 0f); // 血条正中在头顶正上方
        }
        foreach (var pair in nameLabels)
        {
            var name = pair.Value;
            if (name == null) continue;
            //名字挂在血条上方；血条不可见（实体消失/背面）时名字一并隐藏
            bool visible = entityBars.TryGetValue(pair.Key, out var bar) && bar != null && bar.visible;
            name.visible = visible;
            //名字与血条同轴：以血条中心为准再补上两者宽度差的一半，名字的中间对齐血条中间
            if (visible) name.xy = bar.xy + new Vector2((bar.width - name.width) * 0.5f, -26f);
        }
    }

    private void RemoveEntityBar(ushort entityId)
    {
        if (nameLabels.TryGetValue(entityId, out var name))
        {
            if (name != null) name.Dispose();
            nameLabels.Remove(entityId);
        }
        if (entityBars.TryGetValue(entityId, out var bar))
        {
            if (bar != null) bar.Dispose();
            entityBars.Remove(entityId);
        }
        barOwners.Remove(entityId);
    }

    private void ClearEntityBars()
    {
        foreach (var name in nameLabels.Values)
        {
            if (name != null) name.Dispose();
        }
        foreach (var bar in entityBars.Values)
        {
            if (bar != null) bar.Dispose();
        }
        nameLabels.Clear();
        entityBars.Clear();
        barOwners.Clear();
    }

    private void ShowDamage(int encoded, ushort targetId, bool hasHitPos, Vector3 hitPos)
    {
        Vector3 anchor;
        if (hasHitPos)
        {
            anchor = hitPos;
        }
        else
        {
            anchor = Vector3.zero;
            if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.EntityPlayers != null)
                Tool.ClientLogicManager.EntityPlayers.TryGetAnchorPos(targetId, EntityAnchor.LabelFallBack, out anchor);
        }

        // 相机背后的点投影后 x/y 会镜像翻转，显示出来就是屏幕上"莫名其妙的位置"——直接不显示
        var cam = GetProjectionCamera();
        if (cam != null && cam.WorldToScreenPoint(anchor).z <= 0f) return;

        // 散布走屏幕空间（水平±50/垂直±10px）：世界空间散布随距离表现异常（远则仍叠、近则偏移过大）
        var scatterPx = new Vector2(Random.Range(-50f, 50f), Random.Range(-10f, 10f));

        var label = UI_DamageLabel.CreateInstance();
        if (encoded == 0)
        {
            label.m_type.selectedIndex = 2; // 无效文本已在 FGUI 中配置
        }
        else if (encoded > 0)
        {
            label.m_type.selectedIndex = 0;
            label.m_num_common.text = encoded.ToString();
        }
        else
        {
            label.m_type.selectedIndex = 1;
            label.m_num_strike.text = (-encoded).ToString();
        }
        Root.AddChild(label);
        //组件轴心是左上角（FGUI 里 xy 即左上角），减去半个宽度让飘字正中在锚点位置
        label.xy = WorldToPanel(anchor, cam) + scatterPx - new Vector2(label.width * 0.5f, 0f);
        damageLabels.Add(new DamageLabelItem { label = label, time = Time.time, anchor = anchor, scatterPx = scatterPx, risePx = 0f });
    }

    // 相机插值收尾回调：用最终相机变换重投影血条/名字/飘字，消除与渲染差一帧的震颤
    private void OnCameraUpdated()
    {
        UpdateEntityBarPositions();
        ReprojectDamageLabels();
    }

    // 飘字按世界锚点重投影（上升量仍走屏幕像素），使出生点与相机同步、不再滞后一帧
    private void ReprojectDamageLabels()
    {
        var cam = GetProjectionCamera();
        if (cam == null) return;
        for (int i = 0; i < damageLabels.Count; i++)
        {
            var item = damageLabels[i];
            if (item.label == null) continue;
            // 锚点转到相机背后时投影会镜像错位，直接隐藏（与 ShowDamage 的剔除一致）
            if (cam.WorldToScreenPoint(item.anchor).z <= 0f) { item.label.visible = false; continue; }
            item.label.visible = true;
            item.label.xy = WorldToPanel(item.anchor) + item.scatterPx - new Vector2(item.label.width * 0.5f, 0f) + new Vector2(0f, -item.risePx);
        }
    }

    private void TickDamageLabels(float deltaTime)
    {
        for (int i = damageLabels.Count - 1; i >= 0; i--)
        {
            var item = damageLabels[i];
            float age = Time.time - item.time;
            if (age > DamageLife)
            {
                if (item.label != null) item.label.Dispose();
                damageLabels.RemoveAt(i);
                continue;
            }
            if (item.label == null)
            {
                damageLabels.RemoveAt(i);
                continue;
            }
            item.risePx += 60f * deltaTime;
            damageLabels[i] = item;
            if (age > DamageLife * 0.5f)
                item.label.alpha = Mathf.Clamp01(1f - (age - DamageLife * 0.5f) / (DamageLife * 0.5f));
        }
    }

    private void AddEventEntry(EventEntry entry)
    {
        entry.time = Time.time;
        eventEntries.Add(entry);
        RefreshEventItems();
    }

    private void RefreshEventItems()
    {
        var list = panel.m_EventList != null ? panel.m_EventList.m_EventItemContainer : null;
        if (list == null) return;
        list.itemRenderer = RenderEventItem;
        list.numItems = eventEntries.Count;
    }

    private void RenderEventItem(int index, GObject obj)
    {
        if (obj is not UI_EventItem item || index >= eventEntries.Count) return;
        EventEntry entry = eventEntries[index];
        item.m_type.selectedIndex = entry.type;
        switch (entry.type)
        {
            case 0:
                item.m_type0_label.text = entry.text;
                break;
            case 1:
                item.m_type1_loader.m_type.selectedIndex = entry.icon;
                item.m_type1_label.text = entry.text;
                break;
            default:
                item.m_type2_loader.m_type.selectedIndex = entry.icon;
                item.m_type2_label1.text = entry.left;
                item.m_type2_label2.text = entry.right;
                break;
        }
    }

    private void AddTextEvent(string text)
    {
        AddEventEntry(new EventEntry { type = 0, text = text });
    }

    private void TickEventItems()
    {
        int expired = 0;
        while (expired < eventEntries.Count && Time.time - eventEntries[expired].time > EventLife) expired++;
        if (expired == 0) return;
        eventEntries.RemoveRange(0, expired);
        RefreshEventItems();
    }

    private void ClearEventItems()
    {
        eventEntries.Clear();
        RefreshEventItems();
    }

    private void RefreshTimeIcon()
    {
        if (panel.m_icon_day_night == null) return;
        panel.m_icon_day_night.rotation = 180f - EnvironmentManager.Time01 * 180f;
    }

    private static string FormatTime(float seconds)
    {
        int s = Mathf.CeilToInt(seconds);
        return $"{s / 60:D2}:{s % 60:D2}";
    }

    private void RefreshSkillSlot(UI_SkillListItem item, SCEntityDisplayInfo.SkillSlotRuntime slot)
    {
        var info = slot.skillId >= 0 && Tool.InfoManager != null ? Tool.InfoManager.GetSkillInfo(slot.skillId) : null;

        var iconSprite = info != null ? info.icon : null;
        if (item.m_loader_icon != null)
        {
            item.m_loader_icon.texture = iconSprite != null ? new NTexture(iconSprite) : null;
            item.m_loader_icon.visible = iconSprite != null;
        }
        //底图与技能图标一并设置（同一张图，底图在后/图标在前由 prefab 层级保证）
        if (item.m_loader_iconBase != null)
        {
            item.m_loader_iconBase.texture = iconSprite != null ? new NTexture(iconSprite) : null;
            item.m_loader_iconBase.visible = iconSprite != null;
        }


        //CD：客户端本地模拟（TickLocalSkillCd），服务器只下发技能 id 列表，不携带冷却进度
        float cdRemain = localCdEnds.TryGetValue(slot.skillId, out var cdEnd) ? cdEnd - Time.time : 0f;
        float cdTotal = SkillManager.GetSkillCD(slot.skillId);
        bool cooling = cdRemain > 0f && cdTotal > 0f;
        item.m_loader_icon.fillAmount = cooling ? 1f - Mathf.Clamp01(cdRemain / cdTotal) : 1f;
    }

    // 退出确认：确认后 ExitWorld → NetworkManager 触发 OnExitWorld → UIManager 切回主界面
    private void OnExitClicked()
    {
        if (Tool.UIManager != null) Tool.UIManager.ShowConfirm("退出将放弃本局，确定退出吗？", DoExit);
        else DoExit();
    }

    private static void DoExit()
    {
        if (Tool.NetworkManager != null) Tool.NetworkManager.ExitWorld();
    }
    #endregion
}
