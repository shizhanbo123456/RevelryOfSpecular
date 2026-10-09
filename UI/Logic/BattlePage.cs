using Ros.UI.Main;
using System.Collections.Generic;
using Ros.Info;
using Ros.Transport;
using FairyGUI;
using UnityEngine;

public class BattlePage : PageBase
{
    private readonly UI_BattlePanel panel;
    private UI_BattleResult resultPanel;
    private UI_BattleResultDetail resultDetail;
    private float battleStartTime;
    private bool settlePlaying; //结算动画播放中（跨页面存活，Exit 据此跳过清理）

    private readonly List<SCEntityDisplayInfo.SkillSlotRuntime> skillSummary = new(); // 渲染器按索引读取的技能摘要缓存
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
    private readonly List<DamageLabelItem> damageLabels = new();
    private readonly List<EventEntry> eventEntries = new(); // 事件列表数据源（渲染器按索引读取）
    private readonly List<UI_BattleResultDetailItem> settleItems = new();
    private readonly List<string> settleRows = new(); // 渲染器按索引读取的明细行文本
    private Timer.TransitionHandle settleTransition;
    private int localPlayerCamp = -1; // 本地玩家阵营（随本地实体摘要更新；小地图敌我识别用）

    private const float DamageLife = 0.8f;
    private const float EventLife = 3.5f;
    private const float SettleAutoClose = 5f;
    private const float DetailStartDelay = 0.5f;   // 面板转场后首条细节出现
    private const float DetailItemInterval = 0.3f; // 相邻两条细节的间隔
    private const float DetailHoldDelay = 0.6f;    // 最后一条出现后的停留
    private const float DetailFadeDuration = 0.5f; // 全部细节同步淡出时长
    private const int SkillOutlineVariants = 5;    // UI_SkillListItem.m_randomOutline 的档位数（0~4）

    private static readonly Color CampAttackColor = new Color(1f, 0.45f, 0.4f);
    private static readonly Color CampDefenseColor = new Color(0.4f, 0.72f, 1f);

    private struct EventEntry
    {
        public int type;     // UI_EventItem 的 type 控制器：0 纯文字 / 1 图标+文字 / 2 文字+图标+文字
        public string text;  // type 0/1 的文本
        public Color color;  // type 0 的文本颜色
        public int icon;     // type 1/2 的 EventIcon 档位
        public string left;  // type 2 左侧文本（击杀者）
        public string right; // type 2 右侧文本（受害者）
        public float time;   // 入列时间；超过 EventLife 的条目从列表头部移除
    }

    private struct DamageLabelItem
    {
        public UI_DamageLabel label;
        public float time;
        public Vector3 anchor;  // 世界锚点（命中点 + 水平随机散布），每帧据此重投影
        public float risePx;    // 屏幕上升累计像素
    }

    public BattlePage(UI_BattlePanel panel) : base(panel)
    {
        this.panel = panel;
    }

    public override void Construct()
    {
        panel.m_btn_exit.onClick.Add(() => { if (Tool.NetworkManager != null) Tool.NetworkManager.ExitWorld(); }); // 主动退出：NetworkManager 触发 OnExitWorld，UIManager 切回主界面
        panel.m_showRegenerationBar.selectedIndex = 0; // 复活进度默认隐藏
    }

    public override void Enter(ShowParam param)
    {
        base.Enter(param);
        EventManager.AddEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
        EventManager.AddEvent<int>(ClientEvent.OnEntityDisplayRemove, OnEntityDisplayRemove);
        EventManager.AddEvent<SCMinimapEntity>(ClientEvent.OnMinimapUpdate, OnMinimapUpdate);
        EventManager.AddEvent<float>(ClientEvent.OnMinimapRadiusUpdate, OnMinimapRadiusUpdate);
        EventManager.AddEvent<SCScoreInfo>(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.AddEvent<SettlementResult>(ClientEvent.OnSettlementResult, OnSettlementResult);
        EventManager.AddEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.AddEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        if (Tool.CameraController != null) Tool.CameraController.OnCameraUpdated += OnCameraUpdated;

        battleStartTime = Time.time;
        localPlayerCamp = -1;
        minimapViewRadius = Config.minimap_view_radius; //每局回到默认档，与服务器 ClearBattleState 对齐
        // 首页/大厅展示的选角预览模型只属于那两个界面，进战斗前清掉（否则会残留在地图的预览锚点上）
        if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.HomePreview != null)
            Tool.ClientLogicManager.HomePreview.Hide();
        ResetBeacons();
        ClearEntityBars();
        ClearMinimap();
        ClearEventItems();
        BuildSkillSlots(NetworkManager.battleInfo != null ? NetworkManager.battleInfo.camp : EntityCamp.None);
        HideSettlement();
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
        EventManager.RemoveEvent<SettlementResult>(ClientEvent.OnSettlementResult, OnSettlementResult);
        EventManager.RemoveEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.RemoveEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        if (Tool.CameraController != null) Tool.CameraController.OnCameraUpdated -= OnCameraUpdated;
        if (!settlePlaying) HideSettlement(); //结算切页走正常流程不清理；断线等异常切页时取消动画并隐藏面板
    }

    public override void Tick(float deltaTime)
    {
        //剩余时间（本地估算，精确值以服务器 SCScoreInfo 为准）
        if (NetworkManager.battleInfo != null && panel.m_label_time_left != null)
        {
            float remain = Config.battle_duration - (Time.time - battleStartTime);
            panel.m_label_time_left.text = FormatTime(Mathf.Max(0f, remain));
        }
        RefreshTimeIcon();
        TickDamageLabels(deltaTime);
        TickEventItems();
        TickMinimapTimeout();
        TickSelfMinimapRotation();
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

        if (NetworkManager.battleInfo != null && info.entityId == NetworkManager.battleInfo.playerEntityId)
        {
            localPlayerCamp = (int)info.camp;
            UpdateLocalPlayerBar(info);
            OnLocalSkillBarUpdate(info);
        }
    }

    private void OnEntityDisplayRemove(int entityId)
    {
        RemoveEntityBar((ushort)entityId);
        RemoveMinimapItem((ushort)entityId);
    }

    private void OnLocalSkillBarUpdate(SCEntityDisplayInfo info)
    {
        var list = panel.m_skillList != null ? panel.m_skillList.m_content : null;
        if (list == null) return;
        skillSummary.Clear();
        if (info.skills != null) skillSummary.AddRange(info.skills);
        if (!skillSlotsBuilt) BuildSkillSlots(info.camp); // 兜底：进战斗时若槽位数取不到（理论不会），首包摘要时再搭一次
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
        if (slot.m_starList != null)
        {
            slot.m_starList.itemRenderer = (_, _) => { };
            slot.m_starList.numItems = 0;
        }
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
        bar.m_fill.fillAmount = destroyed ? 1f : (info.maxHealth > 0 ? Mathf.Clamp01((float)info.health / info.maxHealth) : 1f);
        LogDefensiveBarSize(destroyed ? "OnBeaconDisplay(destroyed)" : "OnBeaconDisplay", bar);
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
        bar.m_fill.fillAmount = 0f;
    }

    // 临时调试：打印防御点血条尺寸信息，排查进度条与背景尺寸不匹配（确认后删除）
    private static void LogDefensiveBarSize(string tag, UI_DefensivePointBar bar)
    {
        if (bar == null) return;
        GObject bg = bar.GetChildAt(0);
        Debug.Log($"[DefensivePointBar] {tag}: bar={bar.width}x{bar.height} bg={bg.width}x{bg.height} " +
                  $"fill={bar.m_fill.width}x{bar.m_fill.height} source={bar.m_fill.sourceWidth}x{bar.m_fill.sourceHeight} " +
                  $"fillAmount={bar.m_fill.fillAmount} fillMethod={bar.m_fill.fillMethod}");
    }

    // 守护点被摧毁：据事件携带的 beacon 标识定位血条，切到已摧毁外观并清空填充
    // beaconId：-1=中心守护点(main)，0~2=外围 Sub1~3（与 OnBeaconDisplay 的 index 映射一致）
    private void SetBeaconDestroyed(int beaconId)
    {
        UI_DefensivePointBar bar = beaconId < 0 ? panel.m_progressMain
            : beaconId == 0 ? panel.m_progressSub1
            : beaconId == 1 ? panel.m_progressSub2 : panel.m_progressSub3;
        if (bar == null) return;
        bar.m_destroyed.selectedIndex = 1;
        bar.m_fill.fillAmount = 1f;
        LogDefensiveBarSize("SetBeaconDestroyed", bar);
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

    private void OnSettlementResult(SettlementResult r)
    {
        //先清战斗表现，防过时内容残留
        if (Tool.ClientLogicManager != null) Tool.ClientLogicManager.EntityPlayers.ClearAll();

        if (resultPanel == null)
        {
            resultPanel = UI_BattleResult.CreateInstance();
            GRoot.inst.AddChild(resultPanel); //挂全局层：结算要跨页面盖在组队界面之上
            resultPanel.visible = false;
        }
        var battle = NetworkManager.battleInfo;
        EntityCamp camp = battle != null ? battle.camp : EntityCamp.None;
        resultPanel.m_title.text = GetEndText(r.gameState, camp);
        resultPanel.m_title.color = GetEndColor(r.gameState);
        resultPanel.m_content.text = $"战斗用时：{Mathf.RoundToInt(Mathf.Max(0f, Config.battle_duration - r.remainTime))} 秒";

        // 结算细节：BattleResultDetail 列表承载（位置/尺寸由 FGUI 包决定，代码不做适配）
        if (resultDetail == null)
        {
            resultDetail = UI_BattleResultDetail.CreateInstance();
            resultPanel.AddChild(resultDetail);
        }
        BuildResultRows(resultDetail, r);

        resultPanel.visible = true;
        resultPanel.m_t0.Play();
        settlePlaying = true;
        ScheduleDetailAnimations(); //含播完自动关闭并恢复组队面板

        //立即切回组队界面（战斗 HUD 随 Exit 隐藏），再隐藏组队面板等结算播完
        if (Tool.UIManager != null)
        {
            Tool.UIManager.TurnPage(PageType.Lobby);
            Tool.UIManager.SetLobbyPanelVisible(false);
        }
    }

    private void BuildResultRows(UI_BattleResultDetail detail, SettlementResult r)
    {
        settleRows.Clear();
        settleItems.Clear();
        var battle = NetworkManager.battleInfo;
        EntityCamp camp = battle != null ? battle.camp : EntityCamp.None;
        settleRows.Add($"战斗分数：进攻方 {(int)r.attackScore} ／ 防守方 {(int)r.defenseScore}");
        settleRows.Add($"击杀数：{r.killScore}");
        settleRows.Add(camp == EntityCamp.Attack
            ? $"对防守点伤害：{(int)r.expGain}"
            : $"防守点剩余血量：{(int)r.beaconHealth}");
        settleRows.Add($"获得的角色经验：+{r.expGain}");
        settleRows.Add($"获得的玩家经验：+{r.expGain}");
        if (r.characterLevelAfter > r.characterLevelBefore)
            settleRows.Add($"角色升级：{r.characterLevelBefore} → {r.characterLevelAfter}");
        if (r.playerLevelAfter > r.playerLevelBefore)
            settleRows.Add($"玩家升级：{r.playerLevelBefore} → {r.playerLevelAfter}");

        var list = detail.m_resultList;
        list.itemRenderer = RenderResultRow;
        list.numItems = settleRows.Count; // 非虚拟列表同步建条目并回调渲染器
    }

    private void RenderResultRow(int index, GObject obj)
    {
        if (obj is not UI_BattleResultDetailItem item) return;
        item.alpha = 0f; // 播放自身动画前保持透明
        item.m_content.text = index < settleRows.Count ? settleRows[index] : string.Empty;
        if (!settleItems.Contains(item)) settleItems.Add(item); // 动画序列按此顺序逐条播放
    }

    private void ScheduleDetailAnimations()
    {
        CancelDetailAnimations();
        //总时长固定为面板自动关闭时间：明细播完后留白到 5s，transition 走完即关闭结算并恢复组队面板
        int shown = 0;
        float fadeStart = DetailStartDelay + settleItems.Count * DetailItemInterval + DetailHoldDelay;
        settleTransition = Timer.AddTransition(0, SettleAutoClose, (_, t01) =>
        {
            if (t01 >= 1f)
            {
                settleTransition = null; //自身已结束，避免在回调内 Cancel 自身
                CloseSettlement();
                return;
            }
            if (settleItems.Count == 0) return;
            float elapsed = t01 * SettleAutoClose;
            // 到达出现时刻的条目逐条播放自身动画（shown 指针保证每条只播一次）
            while (shown < settleItems.Count && elapsed >= DetailStartDelay + shown * DetailItemInterval)
            {
                var item = settleItems[shown++];
                if (item != null && item.displayObject != null) item.m_t0.Play();
            }
            // 全部出现后同步淡出直至消失
            if (elapsed >= fadeStart)
            {
                float alpha = 1f - Mathf.Clamp01((elapsed - fadeStart) / DetailFadeDuration);
                foreach (var item in settleItems)
                {
                    if (item != null && item.displayObject != null) item.alpha = alpha;
                }
            }
        });
    }

    private void CancelDetailAnimations()
    {
        if (settleTransition != null) settleTransition.Cancel();
        settleTransition = null;
    }

    //结算动画播完：隐藏结算、恢复组队界面面板（切页与实体清理已在结算开始时做掉）
    private void CloseSettlement()
    {
        settlePlaying = false;
        CancelDetailAnimations();
        if (resultPanel != null) resultPanel.visible = false;
        if (Tool.UIManager != null) Tool.UIManager.SetLobbyPanelVisible(true);
    }

    private void HideSettlement()
    {
        settlePlaying = false;
        CancelDetailAnimations();
        if (resultPanel != null) resultPanel.visible = false;
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
        if (NetworkManager.battleInfo == null || Tool.ClientLogicManager == null
            || Tool.ClientLogicManager.EntityPlayers == null
            || !Tool.ClientLogicManager.EntityPlayers.TryGetEntityPosition(
                (ushort)NetworkManager.battleInfo.playerEntityId, out var myPos)) return;

        float dx = entity.posX - myPos.x, dz = entity.posZ - myPos.z;
        // 只画雷达半径内：超出显示半径的点位直接移除（服务器圈外本就过滤，回到圈内会重建）
        float cullRadiusSq = minimapViewRadius * minimapViewRadius;
        if (dx * dx + dz * dz > cullRadiusSq)
        {
            RemoveMinimapItem(entity.entityId);
            minimapLastReceived.Remove(entity.entityId);
            return;
        }

        bool isSelf = entity.entityId == NetworkManager.battleInfo.playerEntityId;
        if (!minimapItems.TryGetValue(entity.entityId, out var item))
        {
            item = UI_MinimapItem.CreateInstance();
            panel.m_Minimap.AddChild(item); //GGraph 不是容器，点位挂在 Minimap 面板上
            minimapItems[entity.entityId] = item;
            if (isSelf) selfMinimapItem = item;
        }
        item.m_type.selectedIndex = GetMinimapType(entity);
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
        if (NetworkManager.battleInfo == null || Tool.ClientLogicManager == null
            || Tool.ClientLogicManager.EntityPlayers == null) return;
        if (!Tool.ClientLogicManager.EntityPlayers.TryGetEntityTransform(
            (ushort)NetworkManager.battleInfo.playerEntityId, out _, out var rot)) return;
        selfMinimapItem.rotation = rot.eulerAngles.y; //FGUI 顺时针角度与 Unity yaw 同向（图标默认朝上时）
    }

    private int GetMinimapType(SCMinimapEntity entity)
    {
        switch (entity.type.category)
        {
            case EntityCategory.Character_Attack:
            case EntityCategory.Character_Defense:
                if (NetworkManager.battleInfo != null && entity.entityId == NetworkManager.battleInfo.playerEntityId) return 0;
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

    private void OnBattleEvent(SCBattleEvent e)
    {
        if (e == null) return;
        switch (e.type)
        {
            case SCBattleEvent.Type.Damage:
                ShowDamage(e.value, (ushort)e.targetId, e.hasHitPos, e.hitPos);
                break;
            case SCBattleEvent.Type.Kill:
            {
                //「玩家A (图标) 玩家B」：value = 击杀者客户端 id（-1 无归属），targetId = 受害实体（名字按归属反查）
                int killerId = e.value;
                string victimName = barOwners.TryGetValue((ushort)e.targetId, out var owner)
                    ? NetworkManager.GetMemberName(owner) : "玩家";
                string killerName = killerId >= 0 ? NetworkManager.GetMemberName(killerId) : "玩家";
                AddEventEntry(new EventEntry
                {
                    type = 2, // 文字+图标+文字
                    icon = 6, // 玩家间击败
                    left = killerName,
                    right = victimName,
                });
                break;
            }
            case SCBattleEvent.Type.BeaconDestroyed:
                AddTextEvent("守护点被摧毁！", CampAttackColor);
                SetBeaconDestroyed(e.value);
                break;
            case SCBattleEvent.Type.CrystalCollected:
                AddTextEvent("采集水晶，获得收益", new Color(0.42f, 0.85f, 0.55f));
                break;
            case SCBattleEvent.Type.CrystalBroken:
                AddTextEvent("该水晶已被感染，无产出", new Color(1f, 0.62f, 0.28f));
                break;
            case SCBattleEvent.Type.PlagueTreeCaptured:
                AddEventEntry(new EventEntry
                {
                    type = 1, // 图标+文字
                    icon = 1, // 瘟疫树被击败
                    text = "攻占瘟疫树！获得瘟疫祝福",
                });
                break;
            case SCBattleEvent.Type.ShowText:
                AddTextEvent(NoticeMessageMap.Get(e.value), Color.white);
                break;
        }
    }

    private void OnReviveProgressUpdate(SCReviveInfo info)
    {
        if (info == null) return;
        bool isLocal = NetworkManager.battleInfo != null && info.entityId == NetworkManager.battleInfo.playerEntityId;
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
            nameItem.m_num.text = NetworkManager.GetMemberName(info.ownerClientId);
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
        barOwners[info.entityId] = info.ownerClientId;
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
                visible = Tool.ClientLogicManager.EntityPlayers.TryGetEntityHeadPos(pair.Key, out headPos);
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
            // 1m 范围水平随机散布，避免连续命中的飘字叠在一起
            Vector2 rand = Random.insideUnitCircle;
            anchor = hitPos + new Vector3(rand.x, 0f, rand.y);
        }
        else
        {
            anchor = Vector3.zero;
            if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.EntityPlayers != null)
                Tool.ClientLogicManager.EntityPlayers.TryGetEntityHeadPos(targetId, out anchor);
        }

        // 相机背后的点投影后 x/y 会镜像翻转，显示出来就是屏幕上"莫名其妙的位置"——直接不显示
        var cam = GetProjectionCamera();
        if (cam != null && cam.WorldToScreenPoint(anchor).z <= 0f) return;

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
        label.xy = WorldToPanel(anchor, cam) - new Vector2(label.width * 0.5f, 0f);
        damageLabels.Add(new DamageLabelItem { label = label, time = Time.time, anchor = anchor, risePx = 0f });
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
            item.label.xy = WorldToPanel(item.anchor) - new Vector2(item.label.width * 0.5f, 0f) + new Vector2(0f, -item.risePx);
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
                item.m_type0_label.color = entry.color;
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

    private void AddTextEvent(string text, Color color)
    {
        AddEventEntry(new EventEntry { type = 0, text = text, color = color });
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

    private static Color GetEndColor(int gameState)
    {
        switch (gameState)
        {
            case 1: return CampAttackColor;
            case 2: return CampDefenseColor;
            default:
                Debug.LogError($"[BattlePage] 未处理的结算状态：{gameState}");
                return Color.white;
        }
    }

    private static string FormatTime(float seconds)
    {
        int s = Mathf.CeilToInt(seconds);
        return $"{s / 60:D2}:{s % 60:D2}";
    }

    private static string GetEndText(int gameState, EntityCamp camp)
    {
        bool win = (gameState == 1 && camp == EntityCamp.Attack)
                || (gameState == 2 && camp == EntityCamp.Defense);
        return win ? "胜利" : "结束";
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
        if (item.m_store != null) item.m_store.text = slot.store >= 0 ? $"x{slot.store}" : "";

        //CD：填充比例随冷却进度增长（0 → 100）
        bool cooling = slot.cdTotal > 0f && slot.cdRemain > 0f;
        item.m_loader_icon.fillAmount = cooling ? 1f - Mathf.Clamp01(slot.cdRemain / slot.cdTotal) : 1f;

        //技能经验：exp 与星星数量 1:1（numItems 需要 itemRenderer 非空，给空实现）
        item.m_starList.itemRenderer = (_, _) => { };
        item.m_starList.numItems = Mathf.Max(0, slot.exp);
    }
    #endregion
}
