using Ros.UI.Main;
using System.Collections.Generic;
using Ros.Skill.Utils;
using Ros.Transport;
using FairyGUI;
using UnityEngine;

public class BattlePage : PageBase
{
    private readonly UI_BattlePanel panel;
    private float battleStartTime;

    private readonly List<SCEntityDisplayInfo.SkillSlotRuntime> skillSummary = new(); // 渲染器按索引读取的技能摘要缓存（仅技能 id）
    private readonly SkillRecharge skillRecharge = new(); // 本地冷却/库存推演：服务器只下发技能 id，不发冷却进度
    private float skillCdRefreshTimer;   // 冷却环重渲染节流（本地推演需要周期刷新）
    private int skillSlotCount;                           // 技能栏条目数 = 本地角色技能槽位数（进入战斗时确定）
    private bool skillSlotsBuilt;                         // 技能栏骨架是否已搭好（搭好前渲染器不读数据）
    private readonly Dictionary<ushort, UI_EntityBar> entityBars = new();
    private readonly Dictionary<ushort, List<SCEntityDisplayInfo.BuffRuntime>> entityBuffs = new(); // 各实体血条上的 buff 图标数据源
    private readonly List<SCEntityDisplayInfo.BuffRuntime> localBuffs = new();                      // 本地玩家左上角血条的 buff 数据源
    private readonly float barDefaultSizeDistance = ConfigManager.GlobalVariableConfig.distance_of_bar_default_size; // 血条 scale=(1,1) 时的相机距离
    private readonly Dictionary<ushort, UI_MinimapItem> minimapItems = new();
    private readonly Dictionary<ushort, float> minimapLastReceived = new();
    private readonly Dictionary<ushort, Vector2> minimapWorldPos = new(); // 各点位最后一次已知世界坐标（x/z），包到达时写入
    private readonly List<ushort> s_culledMinimapIds = new();             // 逐帧裁剪的临时列表（遍历中不能改字典）
    private readonly List<ushort> s_expiredMinimapIds = new();
    private UI_MinimapItem selfMinimapItem; //自身点位：固定在雷达中心且永远置顶
    private float minimapViewRadius = ConfigManager.GlobalVariableConfig.minimap_view_radius[0]; //当前雷达显示半径（服务器权威，F 键切换；首档即默认档）
    private float minimapItemScale = 1f; // 抵消 Minimap 等上层元素缩放（不含页面根 UiScale 适配），使图标按 FGUI 设计尺寸显示；界面初始化时算
    private GLoader minimapMapLoader;    // 遮罩内的地形底图：xy/尺寸每帧随玩家位置与雷达半径换算
    private NTexture minimapMapTexture;  // 缓存 NTexture：包一层 AssetsManager.MapTexture，重复 Enter 不重建
    private Texture2D mapAreaTexture;    // AssetsManager.MapArea 的 Texture2D 引用（贴图需开 Read/Write）
    private int localPlayerArea = -1;    // 本地玩家当前区域（EventBanner 页 index，-1=未知；死亡观战期间冻结）
    private readonly List<CombatLabelItem> combatLabels = new();
    private UI_SkillTip skillTip;     // 技能悬浮提示（技能栏条目）
    private UI_BuffTip buffTip;       // buff 悬浮提示（任意 buff 图标）
    private int localPlayerCamp = -1; // 本地玩家阵营（随本地实体摘要更新；小地图敌我识别用）
    private bool spectatorMode;       // 纯观战方玩家（无战斗实体）
    private bool localPlayerAlive = true; // 本地玩家实体是否在场（死亡观战期间 false，显示观战下拉）
    private int spectateTargetClientId = -1;  // 当前观战目标玩家 id（-1 = 未观战）
    private int spectateTargetEntityId = -1;  // 观战目标实体 id（-1 = 等待其表现数据到达后绑定相机）
    private float spectatePendingSince;   // 发起观战的时刻（目标数据迟迟不到 = 目标已死，超时重选）
    private float onlookRefreshTimer;     // 观战下拉选项刷新节流
    private bool syncingOnlookCombo;      // 程序刷新下拉选项期间屏蔽 onChanged 回调

    private const float CombatLabelLife = 0.8f;
    private const int CombatLabelPageCount = 12; // 与 FGUI UI_DamageLabel 控制器页数一致，枚举加值须同步加页
    private const int HitVfxIndexBuff = 0;   // 命中特效列表下标：buff/DoT 类（贴实体中心）
    private const int HitVfxIndexDirect = 1; // 命中特效列表下标：直接伤害（普通/暴击/护盾）
    private const float HitVfxLife = 2f;
    private readonly Queue<string> importantBannerQueue = new(); // 待播的特殊技能播报文案，按到达顺序逐条播
    private bool importantBannerPlaying;                          // 当前 banner 动画是否在播
    private const string ImportantBannerPrefix = "特殊能力启动-";
    private readonly List<int> settingTabKeys = new();   // 页签 → 配表 key（按 key 排序）
    private readonly List<string> settingTabTexts = new(); // 每个页签对应的说明文本
    private int settingTab; // 当前选中的页签下标

    // 本局会话数据全部来自加载期（不再读组队缓存）
    private static ClientLoadingManager Session => ClientLoadingManager.Current;
    private static bool HasSession => Session != null && Session.HasSession;
    private static ushort OwnEntityId => HasSession ? Session.OwnEntityId : (ushort)0;
    private bool IsOwnEntity(int entityId) => HasSession && OwnEntityId == entityId;

    private struct CombatLabelItem
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
        panel.m_batleInfoPanel.m_btn_setting.onClick.Add(ShowSettings);
        panel.m_settings_panel.m_panrl.m_close.onClick.Add(HideSettings);
        panel.m_settings_panel.m_btn_exit.onClick.Add(OnExitClicked); // 退出需二次确认（对局中退出不可逆）
        BuildSettingTips();
        panel.m_showRegenerationBar.selectedIndex = 0; // 复活进度默认隐藏
        panel.m_PlayerBar.m_buffList.m_list.itemRenderer = (i, obj) => RenderBuffItem((UI_buffItem)obj, localBuffs[i]);
        ComputeMinimapItemScale();
    }

    // 设置面板常驻战斗界面，开关只改显隐与点击拦截
    private void ShowSettings()
    {
        panel.m_settings_panel.visible = true;
        panel.m_settings_panel.touchable = true;
    }

    private void HideSettings()
    {
        panel.m_settings_panel.visible = false;
        panel.m_settings_panel.touchable = false;
    }

    // 说明来自 setting_tips：页签列表按配表行生成，点页签把该行内容填进说明列表的唯一条目
    private void BuildSettingTips()
    {
        var keys = new List<int>(ConfigManager.SettingTipsConfig.content.Keys);
        keys.Sort();
        foreach (int key in keys)
        {
            settingTabKeys.Add(key);
            settingTabTexts.Add(ConfigManager.SettingTipsConfig.content[key].text.Replace(@"{\n}", "\n"));
        }

        var tabs = panel.m_settings_panel.m_button_list;
        tabs.itemRenderer = (i, obj) =>
        {
            var tab = obj.asButton;
            tab.title = ConfigManager.SettingTipsConfig.content[settingTabKeys[i]].name; // 页签标题取配表「栏目名称」
            tab.selected = i == settingTab;
            int index = i;
            tab.onClick.Set(() => SelectSettingTab(index));
        };

        var list = panel.m_settings_panel.m_content;
        list.itemRenderer = (i, obj) =>
        {
            var label = (UI_SettingsRuleLabel)obj;
            label.m_content.UBBEnabled = true; // 配表内容带 UBB 标记（加粗 / 颜色）
            label.m_content.text = settingTabTexts[settingTab];
            label.height = label.m_content.height; // 条目高度跟随文本，超框时靠列表拖动
        };

        tabs.numItems = settingTabKeys.Count;
        if (settingTabKeys.Count > 0) SelectSettingTab(0);
    }

    private void SelectSettingTab(int index)
    {
        settingTab = index;
        var tabs = panel.m_settings_panel.m_button_list;
        for (int i = 0; i < tabs.numChildren; i++)
            tabs.GetChildAt(i).asButton.selected = i == index; // 页签选中态

        var list = panel.m_settings_panel.m_content;
        list.numItems = 1; // 重设条目数触发重渲染，换成该页签的说明
        if (list.scrollPane != null) list.scrollPane.posY = 0f; // 换栏回到顶部
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
        EventManager.AddEvent<SCCombatLabel>(ClientEvent.OnCombatLabelDisplay, OnCombatLabelDisplay);
        EventManager.AddEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.AddEvent<SCCampAliveCount>(ClientEvent.OnCampAliveCount, OnCampAliveCount);
        EventManager.AddEvent<SCDefensePointProgress>(ClientEvent.OnDefensePointProgress, OnDefensePointProgress);
        EventManager.AddEvent<int>(ClientEvent.OnSkillUsed, OnSkillUsed);
        if (Tool.CameraController != null) Tool.CameraController.OnCameraUpdated += OnCameraUpdated;

        battleStartTime = Time.time;
        localPlayerCamp = -1;
        spectatorMode = Session != null && Session.IsSpectator;
        localPlayerAlive = !spectatorMode;
        localPlayerArea = -1;
        panel.m_onlook.selectedIndex = spectatorMode ? 1 : 0; // onlook=1：FGUI 自动隐藏血条/技能等非观战元素
        if (panel.m_onlookComboBox != null) panel.m_onlookComboBox.onChanged.Set(OnOnlookComboBoxChanged);
        RefreshCampAliveCount(NetworkManager.LatestCampAliveCount); // 进入战斗先填一次缓存值，随后由变更包刷新
        minimapViewRadius = ConfigManager.GlobalVariableConfig.minimap_view_radius[0]; //每局回到默认档（首档），与服务器 ClearBattleState 对齐
        // 首页/大厅展示的选角预览模型只属于那两个界面，进战斗前清掉（否则会残留在地图的预览锚点上）
        if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.HomePreview != null)
            Tool.ClientLogicManager.HomePreview.Hide();
        ResetGuardStatues();
        HideSettings(); // 设置面板每局从收起状态开始
        HideImportantBanner(); // 重要事件 banner 每局从隐藏状态开始
        OnDefensePointProgress(NetworkManager.LatestDefensePointProgress); // 进入战斗先填一次缓存值，随后由变更包刷新
        ClearEntityBars();
        HideAllTips();
        localBuffs.Clear();
        panel.m_PlayerBar.m_buffList.m_list.numItems = 0;
        ClearMinimap();
        EnsureMinimapMapLoader();
        skillRecharge.Clear();
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
        EventManager.RemoveEvent<SCCombatLabel>(ClientEvent.OnCombatLabelDisplay, OnCombatLabelDisplay);
        EventManager.RemoveEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.RemoveEvent<SCCampAliveCount>(ClientEvent.OnCampAliveCount, OnCampAliveCount);
        EventManager.RemoveEvent<SCDefensePointProgress>(ClientEvent.OnDefensePointProgress, OnDefensePointProgress);
        EventManager.RemoveEvent<int>(ClientEvent.OnSkillUsed, OnSkillUsed);
        if (Tool.CameraController != null) Tool.CameraController.OnCameraUpdated -= OnCameraUpdated;
        spectatorMode = false;
        localPlayerAlive = true;
        spectateTargetClientId = -1;
        spectateTargetEntityId = -1;
        if (panel.m_onlook != null) panel.m_onlook.selectedIndex = 0;
        HideAllTips();
        HideImportantBanner();
    }

    public override void Tick(float deltaTime)
    {
        //剩余时间（本地估算，精确值以服务器 SCScoreInfo 为准）
        if (HasSession && panel.m_batleInfoPanel.m_label_time_left != null)
        {
            float remain = ConfigManager.GlobalVariableConfig.battle_duration - (Time.time - battleStartTime);
            panel.m_batleInfoPanel.m_label_time_left.text = FormatTime(Mathf.Max(0f, remain));
        }
        RefreshTimeIcon();
        TickCombatLabels(deltaTime);
        TickMinimapTimeout();
        TickSelfMinimapRotation();
        TickMinimapMapImage();
        TickMinimapItems();
        TickSpectator(deltaTime);
        TickLocalSkillCd(deltaTime);
    }

    // 本地推演：每帧推进库存恢复，周期重渲染冷却环与库存数；扣减只由服务器回执（OnSkillUsed）驱动
    private void TickLocalSkillCd(float deltaTime)
    {
        if (!skillSlotsBuilt || spectatorMode) return;
        skillRecharge.Tick();
        skillCdRefreshTimer -= deltaTime;
        if (skillCdRefreshTimer > 0f) return;
        skillCdRefreshTimer = 0.1f;
        var list = panel.m_skillList != null ? panel.m_skillList.m_content : null;
        if (list != null) list.numItems = skillSlotCount; // 触发原地重渲染：冷却环按本地模拟推进
    }

    // 服务器受理成功才扣一次：被沉默/强控/死亡/连点被抹掉的请求都不会有这条包
    private void OnSkillUsed(int skillId)
    {
        if (!skillSlotsBuilt || skillId < 0) return;
        skillRecharge.Consume(skillId);
        skillCdRefreshTimer = 0f; // 下一帧立刻重渲染冷却环与库存数
    }

    #region 事件处理
    private void OnEntityDisplayUpdate(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        if (info.type.category == EntityCategory.Character_Attack ||
            info.type.category == EntityCategory.Character_Defense)
        {
            UpdateEntityBar(info, withName: true);
        }
        else if (info.type.category == EntityCategory.CrystalStatue ||
                 info.type.category == EntityCategory.Coral)
        {
            UpdateEntityBar(info, withName: false); // 场景物件常显血条，不占名牌
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
        // 纯观战无本地实体：以观战目标阵营作为「我方」基准（名牌敌我配色、小地图点位敌我识别）
        else if (spectatorMode && info.entityId == spectateTargetEntityId) localPlayerCamp = (int)info.camp;
    }

    private void OnEntityDisplayRemove(int entityId)
    {
        RemoveEntityBar((ushort)entityId);
        CullMinimapItem((ushort)entityId);

        if (IsOwnEntity(entityId))
            localPlayerAlive = false; // 本地死亡：显示观战下拉，进入死亡观战
    }

    #region 观战
    private void TickSpectator(float deltaTime)
    {
        var box = panel.m_onlookComboBox;
        if (box == null) return;
        box.visible = spectatorMode || !localPlayerAlive; // 观战目标下拉：纯观战/死亡观战可见

        // 纯观战未选目标（进场自动选 / 目标死亡后自动重选）：随机挑一名参战单位（真人或 AI）。
        // 不能在此 return，否则每帧都卡在重选、下面的下拉刷新永远执行不到，列表会一直是空的。
        if (spectatorMode && spectateTargetClientId == -1) SpectateTo(PickRandomCombatMember());
        if (spectateTargetClientId != -1)
        {
            // 观战目标的当前实体每帧对齐一次（关系在加载会话里维护）：复活换实体自动重绑，无实体则重选
            int currentEntity = ResolveSpectateEntityId(spectateTargetClientId);
            if (currentEntity <= 0)
            {
                spectateTargetClientId = -1;
                spectateTargetEntityId = -1;
                spectatePendingSince = 0f;
            }
            else
            {
                if (currentEntity != spectateTargetEntityId)
                {
                    spectateTargetEntityId = currentEntity;
                    spectatePendingSince = HasSpectateView(currentEntity) ? 0f : Time.time;
                }
                // 目标实体视图未建（目标已死亡 / 表现数据没到）：3 秒后重选
                if (HasSpectateView(spectateTargetEntityId)) spectatePendingSince = 0f;
                else if (spectatePendingSince > 0f && Time.time - spectatePendingSince > 3f)
                {
                    spectateTargetClientId = -1;
                    spectateTargetEntityId = -1;
                    spectatePendingSince = 0f;
                }
            }
        }
        // 目标表现数据已到达：绑定相机到其视图锚点（锚点由视图逐帧刷新）
        if (spectateTargetEntityId != -1 && Tool.CameraController != null
            && Tool.ClientLogicManager != null && Tool.ClientLogicManager.EntityPlayers != null)
        {
            var cam = Tool.CameraController;
            var anchor = Tool.ClientLogicManager.EntityPlayers.GetOrCreateCameraAnchor((ushort)spectateTargetEntityId);
            // 自由视角下只记目标不摆位，否则每帧会被拉回目标身位
            if (anchor != null && (cam.lookTarget != anchor || cam.IsFree))
                cam.SetLookTarget(anchor, !cam.IsFree);
        }

        onlookRefreshTimer -= deltaTime;
        if (onlookRefreshTimer <= 0f)
        {
            onlookRefreshTimer = 0.5f;
            RefreshOnlookComboBox();
        }
    }

    // 观战目标：真人（正数 id）与 AI（虚拟负数 id，从 -1000 起）都合法；-1 仅作「无目标」哨兵
    private void SpectateTo(int targetClientId)
    {
        if (targetClientId == -1) return;
        spectateTargetClientId = targetClientId;
        spectateTargetEntityId = ResolveSpectateEntityId(targetClientId);
        spectatePendingSince = HasSpectateView(spectateTargetEntityId) ? 0f : Time.time;
        NetworkManager.RequestSpectate(targetClientId);
    }

    private int ResolveSpectateEntityId(int clientId) => HasSession ? Session.GetEntityId(clientId) : -1;

    private static bool HasSpectateView(int entityId)
    {
        if (entityId <= 0) return false;
        return Tool.ClientLogicManager != null && Tool.ClientLogicManager.EntityPlayers != null
            && Tool.ClientLogicManager.EntityPlayers.GetView((ushort)entityId) != null;
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
        SyncSkillRecharge();
        if (!skillSlotsBuilt && HasSession) BuildSkillSlots(Session.Camp); // 兜底：进战斗时若槽位数取不到（理论不会），首包摘要时再搭一次
        // 数量与边框样式已定，这里重新设置 numItems 只是按新数据原地重渲染（FGUI 规定的刷新方式，不改变条目数）
        list.numItems = skillSlotCount;
    }

    // 技能摘要与库存表对齐：新增的按满库存入列，已消失的移除；已有的保留原库存（重复获得不补库存）
    private void SyncSkillRecharge()
    {
        var owned = skillRecharge.GetSkillIds();
        for (int i = 0; i < owned.Count; i++)
        {
            bool still = false;
            for (int j = 0; j < skillSummary.Count; j++)
            {
                if (skillSummary[j] != null && skillSummary[j].skillId == owned[i]) { still = true; break; }
            }
            if (!still) skillRecharge.RemoveSkill(owned[i]);
        }
        for (int i = 0; i < skillSummary.Count; i++)
        {
            if (skillSummary[i] != null && !skillRecharge.HasSkill(skillSummary[i].skillId))
                skillRecharge.AddSkill(skillSummary[i].skillId);
        }
    }

    private void BuildSkillSlots(EntityCamp camp)
    {
        var list = panel.m_skillList != null ? panel.m_skillList.m_content : null;
        if (list == null) return;
        int capacity = ResolveSkillSlotCapacity(camp);
        if (capacity <= 0) capacity = Mathf.Max(1, skillSummary.Count); // 取不到属性时退化为"已持有技能数"，至少露出一格
        skillSlotCount = capacity;
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
        // 悬浮显示技能提示（空槽不提示，但离开时照常收起）
        int skillId = empty ? -1 : data.skillId;
        slot.onRollOver.Set(() => { if (skillId >= 0) ShowSkillTip(skillId, slot); });
        slot.onRollOut.Set(HideSkillTip);
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
        if (slot.m_storeList != null) slot.m_storeList.numItems = 0;
    }

    // 守护点进度只认 SCDefensePointProgress（服务器算好的 0~100 开采比例），不再从实体表现包里取血量
    private void OnDefensePointProgress(SCDefensePointProgress info)
    {
        if (info == null) return;
        ApplyStatueProgress(panel.m_barPanel.m_progressMain, info.main);
        ApplyStatueProgress(panel.m_barPanel.m_progressSub1, info.sub1);
        ApplyStatueProgress(panel.m_barPanel.m_progressSub2, info.sub2);
        ApplyStatueProgress(panel.m_barPanel.m_progressSub3, info.sub3);
    }

    private static void ApplyStatueProgress(UI_DefensivePointBar bar, int progress)
    {
        if (bar == null) return;
        int clamped = Mathf.Clamp(progress, 0, 100);
        bar.m_destroyed.selectedIndex = clamped >= 100 ? 1 : 0;
        bar.m_fill.scale = new Vector2(clamped / 100f, bar.m_fill.scale.y);
    }

    private void ResetGuardStatues()
    {
        ResetStatue(panel.m_barPanel.m_progressMain);
        ResetStatue(panel.m_barPanel.m_progressSub1);
        ResetStatue(panel.m_barPanel.m_progressSub2);
        ResetStatue(panel.m_barPanel.m_progressSub3);
    }

    private static void ResetStatue(UI_DefensivePointBar bar)
    {
        if (bar == null) return;
        bar.m_destroyed.selectedIndex = 0;
        bar.m_fill.scale = new Vector2(0f, bar.m_fill.scale.y);
    }

    private void UpdateLocalPlayerBar(SCEntityDisplayInfo info)
    {
        var bar = panel.m_PlayerBar;
        if (bar == null) return;
        bar.m_bar_fill_health.fillAmount = info.maxHealth > 0 ? Mathf.Clamp01((float)info.health / info.maxHealth) : 0f;
        if (bar.m_label_health != null) bar.m_label_health.text = $"{info.health}/{info.maxHealth}";
        RefreshLocalAvatar(info.camp);
        if (info.hasBuffs) RefreshBuffList(bar.m_buffList.m_list, localBuffs, info.buffs);
    }

    // 头像 = 本地玩家所选角色的头像（阵营由本地实体首个显示包确定）
    private void RefreshLocalAvatar(EntityCamp camp)
    {
        var bar = panel.m_PlayerBar;
        if (bar == null || bar.m_avater_mask == null) return;
        var icons = Tool.AssetsManager == null ? null
            : camp == EntityCamp.Defense ? Tool.AssetsManager.DefenseCharacterIcons : Tool.AssetsManager.AttackCharacterIcons;
        int index = camp == EntityCamp.Defense ? ClientSelection.selectedDefenseIndex : ClientSelection.selectedAttackIndex;
        bool hasIcon = icons != null && index >= 0 && index < icons.Count && icons[index] != null;
        bar.m_avater_mask.m_content.texture = hasIcon ? new NTexture(icons[index]) : null;
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
        return Tool.InfoManager.GetPlayerCharacterInfo(saveIndex).GetAttribute(Tool.SaveManager.GetCharacterLevel(saveIndex)).weaponSlotCount;
    }

    private void OnScoreUpdate(SCScoreInfo info)
    {
        if (info == null) return;
        if (panel.m_batleInfoPanel.m_label_time_left != null) panel.m_batleInfoPanel.m_label_time_left.text = FormatTime(Mathf.Max(0f, info.remainTime));
    }

    private void OnCampAliveCount(SCCampAliveCount info) => RefreshCampAliveCount(info);

    private void RefreshCampAliveCount(SCCampAliveCount info)
    {
        if (info == null) return;
        if (panel.m_batleInfoPanel.m_attacker_member_count != null)
            panel.m_batleInfoPanel.m_attacker_member_count.text = $"{info.attackerAlive}/{info.attackerTotal}";
        if (panel.m_batleInfoPanel.m_defense_member_count != null)
            panel.m_batleInfoPanel.m_defense_member_count.text = $"{info.defenseAlive}/{info.defenseTotal}";
        if (panel.m_batleInfoPanel.m_spectater_member_count != null)
            panel.m_batleInfoPanel.m_spectater_member_count.text = info.spectatorCount.ToString();
    }

    private void OnMinimapUpdate(SCMinimapEntity e)
    {
        if (e == null) return;
        // 分帧累积：仅记录收到时间与最近世界坐标并按需显示，不再每帧清空（离屏/夜间停传由超时剔除处理）
        minimapLastReceived[e.entityId] = Time.time;
        minimapWorldPos[e.entityId] = new Vector2(e.posX, e.posZ);
        UpsertMinimapEntity(e);
    }

    //F 键切换雷达显示半径：服务器权威回应，客户端换用新半径做裁剪与缩放（点位下一帧即按新半径重排）
    private void OnMinimapRadiusUpdate(float radius)
    {
        if (radius <= 0f) return;
        minimapViewRadius = radius;
    }

    private void UpsertMinimapEntity(SCMinimapEntity entity)
    {
        if (panel.m_Minimap == null) return;
        if (!TryGetMinimapCenter(out var myPos)) return; // 拿不到自身位置就无法换算，等下一帧

        if (!minimapItems.TryGetValue(entity.entityId, out var item))
        {
            item = UI_MinimapItem.CreateInstance();
            item.scale = new Vector2(minimapItemScale, minimapItemScale); // 抵消上层缩放，恢复 FGUI 设计尺寸
            panel.m_Minimap.AddChild(item); //GGraph 不是容器，点位挂在 Minimap 面板上
            minimapItems[entity.entityId] = item;
            if (IsOwnEntity(entity.entityId)) selfMinimapItem = item;
        }
        item.m_type.selectedIndex = GetMinimapType(entity);
        item.m_highlight.selectedIndex = entity.marked ? 1 : 0; //白眼标记高亮（服务器随包下发，出现/消失都靠每包同步）
        if (!UpdateMinimapItemPosition(entity.entityId, myPos)) CullMinimapItem(entity.entityId);
        RaiseSelfItemToTop(); //新点加入可能盖住自身图标，每次校一次层级
    }

    // 点位位置每帧按当前玩家位置重算：玩家移动时点位立即反向平移，不必等服务器包
    private void TickMinimapItems()
    {
        if (minimapItems.Count == 0) return;
        if (!TryGetMinimapCenter(out var myPos)) return;

        s_culledMinimapIds.Clear();
        foreach (var pair in minimapItems)
            if (!UpdateMinimapItemPosition(pair.Key, myPos)) s_culledMinimapIds.Add(pair.Key);

        foreach (ushort id in s_culledMinimapIds) CullMinimapItem(id);
    }

    // 返回 false = 超出雷达显示半径（由调用方剔除）
    private bool UpdateMinimapItemPosition(ushort entityId, Vector3 myPos)
    {
        if (!minimapItems.TryGetValue(entityId, out var item) || item == null) return true;
        var mapBase = panel.m_Minimap != null ? panel.m_Minimap.m_mapBase : null;
        if (mapBase == null) return true;

        // 位置一律用服务器下发的小地图包坐标；自身固定在雷达中心，不参与换算
        float dx, dz;
        if (IsOwnEntity(entityId))
        {
            dx = 0f;
            dz = 0f;
        }
        else
        {
            Vector2 world = minimapWorldPos[entityId];
            dx = world.x - myPos.x;
            dz = world.y - myPos.z;
            // 只画雷达半径内：超出显示半径的点位剔除（服务器圈外本就过滤，回到圈内会重建）
            if (dx * dx + dz * dz > minimapViewRadius * minimapViewRadius) return false;
        }

        //雷达坐标：上方=世界Z+、右侧=世界X+，当前雷达显示半径铺满 mapBase；减半宽高让图标几何居中
        float scale = mapBase.width * 0.5f / minimapViewRadius;
        item.SetXY(mapBase.x + mapBase.width * 0.5f + dx * scale - item.width * 0.5f,
            mapBase.y + mapBase.height * 0.5f - dz * scale - item.height * 0.5f);
        return true;
    }

    // 雷达中心 = 本地玩家实体位置；纯观战无本地实体时取观战目标位置
    private bool TryGetMinimapCenter(out Vector3 pos)
    {
        pos = Vector3.zero;
        if (!HasSession || Tool.ClientLogicManager == null
            || Tool.ClientLogicManager.EntityPlayers == null) return false;
        ushort centerId = OwnEntityId;
        if (centerId == 0 && spectateTargetEntityId > 0) centerId = (ushort)spectateTargetEntityId;
        return centerId != 0 && Tool.ClientLogicManager.EntityPlayers.TryGetEntityPosition(centerId, out pos);
    }

    // 点位剔除：显示、收包时间与最后世界坐标一起清，回到雷达内等下一个包重建
    private void CullMinimapItem(ushort entityId)
    {
        RemoveMinimapItem(entityId);
        minimapLastReceived.Remove(entityId);
        minimapWorldPos.Remove(entityId);
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
            case EntityCategory.NeutralStatue: return 3;
            case EntityCategory.Coral: return 4;
            case EntityCategory.CrystalStatue: return 5;
            case EntityCategory.Zombie: return 6;
            case EntityCategory.EliteZombie: return 7;
            case EntityCategory.GuardStatue: return entity.type == EntityType.KingStatue ? 8 : 9;
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
        minimapWorldPos.Clear();
        selfMinimapItem = null;
    }

    // 遮罩内创建地形底图 Loader：只建一次，包被重建时自动补建；图片显示由 TickMinimapMapImage 每帧驱动
    private void EnsureMinimapMapLoader()
    {
        var minimap = panel.m_Minimap;
        if (minimap == null || minimap.m_mask == null) return;
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
        if (!TryGetMinimapCenter(out var myPos)) return; // 纯观战用观战目标位置作雷达中心
        TickLocalPlayerArea(myPos);

        float ppm = mapBase.width * 0.5f / minimapViewRadius; // 像素/米
        float size = Landscape.MapSize * ppm;
        float maskX = minimap.m_mask != null ? minimap.m_mask.x : 0f; // Loader 挂在遮罩里，坐标需换算成遮罩局部
        float maskY = minimap.m_mask != null ? minimap.m_mask.y : 0f;
        // 图左上角对应世界 (0, 地图边长)：先按雷达坐标算它在 Minimap 坐标系的位置，再减遮罩偏移
        minimapMapLoader.SetXY(
            mapBase.x + mapBase.width * 0.5f - myPos.x * ppm - maskX,
            mapBase.y + mapBase.height * 0.5f - (Landscape.MapSize - myPos.z) * ppm - maskY);
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

        float u = Mathf.Clamp01(myPos.x / Landscape.MapSize);
        float v = Mathf.Clamp01(myPos.z / Landscape.MapSize);
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

    // 重要事件 banner：技能名取 skill.csv，播报文案格式固定
    private void EnqueueImportantBanner(int skillId)
    {
        importantBannerQueue.Enqueue(ImportantBannerPrefix + ConfigManager.SkillConfig.Get(skillId).name);
        PlayNextImportantBanner();
    }

    // 一条播完（m_t0 结束回调）再取下一条；队列空则收起
    private void PlayNextImportantBanner()
    {
        var banner = panel.m_important_event_banner;
        if (banner == null || importantBannerPlaying) return;
        if (importantBannerQueue.Count == 0)
        {
            banner.visible = false;
            return;
        }
        importantBannerPlaying = true;
        banner.visible = true;
        if (banner.m_title != null) banner.m_title.text = importantBannerQueue.Dequeue();
        if (banner.m_t0 != null) banner.m_t0.Play(OnImportantBannerPlayed);
        else OnImportantBannerPlayed();
    }

    private void OnImportantBannerPlayed()
    {
        importantBannerPlaying = false;
        PlayNextImportantBanner();
    }

    // 进/退出战斗都收起，并丢掉未播的积压
    private void HideImportantBanner()
    {
        importantBannerQueue.Clear();
        importantBannerPlaying = false;
        var banner = panel.m_important_event_banner;
        if (banner == null) return;
        if (banner.m_t0 != null) banner.m_t0.Stop();
        banner.visible = false;
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
        foreach (var id in s_expiredMinimapIds) CullMinimapItem(id);
    }

    // 事件条目播报在聊天面板（ChatPlugin 系统消息页签），这里只负责日夜大横幅与技能播报 banner
    private void OnBattleEvent(SCBattleEvent e)
    {
        if (e == null) return;
        if (e.type == SCBattleEvent.Type.Nightfall) ShowEventBanner(BannerSunset);       // 日落横幅
        else if (e.type == SCBattleEvent.Type.Daybreak) ShowEventBanner(BannerSunrise);   // 日出横幅
        else if (e.type == SCBattleEvent.Type.SkillBroadcast) EnqueueImportantBanner(e.textId); // 特殊技能播报
    }

    private void OnCombatLabelDisplay(SCCombatLabel label)
    {
        if (label == null) return;
        var type = (CombatLabelType)label.type;
        var targetId = (ushort)label.targetId;
        ShowCombatLabel(label.amount, type, targetId, label.hasHitPos, label.hitPos);
        PlayHitVfx(type, targetId, label.hasHitPos, label.hitPos);
        if (IsOwnEntity(targetId)) PlaySelfHitAnim(type);
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
    private void UpdateEntityBar(SCEntityDisplayInfo info, bool withName)
    {
        if (!entityBars.TryGetValue(info.entityId, out var bar))
        {
            bar = CreateEntityBar(info.entityId);
            entityBars[info.entityId] = bar;
        }
        bar.visible = true;

        // 阵营：进攻/防守/观战/中立（中立含中立雕像与场景物件），档位即 PlayerCampIcon.m_player_camp 的页序
        bar.m_campIcon.m_player_camp.selectedIndex = (int)ToPlayerCamp(info.camp);
        bar.m_is_enemy.selectedIndex = IsEnemyCamp(info.camp) ? 1 : 0;
        if (bar.m_entity_name != null)
            bar.m_entity_name.text = withName && HasSession ? Session.GetNameByEntity(info.entityId) : "";

        // 友/敌两条血条由 m_is_enemy 互斥显示，两条都写同一个比例
        float ratio = info.maxHealth > 0 ? Mathf.Clamp01((float)info.health / info.maxHealth) : 0f;
        bar.m_bar_fill_health_partner.fillAmount = ratio;
        bar.m_bar_fill_health_enemy.fillAmount = ratio;
        if (bar.m_label_health != null) bar.m_label_health.text = $"{info.health}/{info.maxHealth}";

        if (info.hasBuffs) RefreshBuffList(bar.m_buff_list.m_list, entityBuffs[info.entityId], info.buffs);
    }

    // 场景 UI（血条 / 伤害飘字）挂在世界层容器上，坐标从面板空间换到容器局部空间
    private Vector2 ToWorldLayer(Vector2 panelPoint)
        => panel.m_world_element_container.GlobalToLocal(Root.LocalToGlobal(panelPoint));

    private UI_EntityBar CreateEntityBar(ushort entityId)
    {
        var bar = UI_EntityBar.CreateInstance();
        panel.m_world_element_container.AddChild(bar); // 挂在世界层：压在战斗界面各元素之下
        var buffs = new List<SCEntityDisplayInfo.BuffRuntime>();
        entityBuffs[entityId] = buffs;
        bar.m_buff_list.m_list.itemRenderer = (i, obj) => RenderBuffItem((UI_buffItem)obj, buffs[i]); // 必须先设渲染器再设 numItems
        return bar;
    }

    // 实体阵营 → 玩家阵营档位（PlayerCampIcon.m_player_camp 页序：0 进攻 / 1 防守 / 2 观战 / 3 中立）
    private static PlayerCampType ToPlayerCamp(EntityCamp camp)
    {
        if (camp == EntityCamp.Attack) return PlayerCampType.Attack;
        if (camp == EntityCamp.Defense || camp == EntityCamp.DefenseTower) return PlayerCampType.Defense;
        return PlayerCampType.Neutral; // 中立雕像、珊瑚与其它场景物件
    }

    // 敌对：只有攻/守两方算阵营；自己阵营未知或对方是中立/场景物件时都不算敌对
    private bool IsEnemyCamp(EntityCamp camp)
    {
        if (localPlayerCamp != (int)EntityCamp.Attack && localPlayerCamp != (int)EntityCamp.Defense) return false;
        // 防御塔算防守方：进攻方看到是敌方，防守方看到是友方
        if (camp == EntityCamp.DefenseTower) return localPlayerCamp == (int)EntityCamp.Attack;
        if (camp != EntityCamp.Attack && camp != EntityCamp.Defense) return false;
        return camp != (EntityCamp)localPlayerCamp;
    }

    private static void RefreshBuffList(GList list, List<SCEntityDisplayInfo.BuffRuntime> target, List<SCEntityDisplayInfo.BuffRuntime> source)
    {
        target.Clear();
        if (source != null) target.AddRange(source);
        list.numItems = target.Count;
    }

    private void RenderBuffItem(UI_buffItem item, SCEntityDisplayInfo.BuffRuntime buff)
    {
        if (buff == null) return;
        var cfg = ConfigManager.EffectConfig.Get(buff.type);
        item.m_negative.selectedIndex = cfg.positive == true ? 0 : 1; // 0 正面 / 1 负面
        Texture icon = null;
        var map = Tool.AssetsManager != null ? Tool.AssetsManager.BuffIconMap : null;
        if (map != null && map.TryGetValue(buff.type, out var tex)) icon = tex;
        item.m_mask.m_loader.texture = icon != null ? new NTexture(icon) : null; // 没配图标 → 不显示
        // level 即显示层数：逐层计时类 = 各层等级之和，单层类 = 该条等级（服务端已聚合）
        item.m_layer_count.text = buff.level.ToString();
        int buffType = buff.type;
        item.onRollOver.Set(() => ShowBuffTip(buffType, item));
        item.onRollOut.Set(HideBuffTip);
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
            if (visible)
            {
                // 血条画在屏幕面板上，不随距离自然缩小：按相机到头顶锚点的距离倒比缩放，默认距离处为 (1,1)
                float distance = Vector3.Distance(cam.transform.position, headPos);
                float scale = barDefaultSizeDistance / Mathf.Max(distance, 0.01f); // 相机贴到锚点时按 1 厘米算，避免除零
                bar.SetScale(scale, scale);
                bar.xy = ToWorldLayer(local) - new Vector2(bar.width * 0.5f * scale, 0f); // 缩放后正中仍落在头顶正上方
            }
        }
    }

    private void RemoveEntityBar(ushort entityId)
    {
        if (entityBars.TryGetValue(entityId, out var bar))
        {
            if (bar != null) bar.Dispose();
            entityBars.Remove(entityId);
        }
        entityBuffs.Remove(entityId);
    }

    private void ClearEntityBars()
    {
        foreach (var bar in entityBars.Values)
        {
            if (bar != null) bar.Dispose();
        }
        entityBars.Clear();
        entityBuffs.Clear();
    }

    private void ShowCombatLabel(int value, CombatLabelType type, ushort targetId, bool hasHitPos, Vector3 hitPos)
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
        int page = (int)type;
        if (page < 0 || page >= CombatLabelPageCount)
        {
            label.Dispose();
            Debug.LogError($"[战斗飘字] 未处理的飘字类型：{page}");
            return;
        }
        label.m_type.selectedIndex = page; // 枚举值即 FGUI 页号
        // 闪避 / 失效页不写数字，其余页统一写下发数值
        if (type != CombatLabelType.Evade && type != CombatLabelType.Invalid) label.m_num_common.text = value.ToString();
        panel.m_world_element_container.AddChild(label); // 挂在世界层：压在战斗界面各元素之下
        //组件轴心是左上角（FGUI 里 xy 即左上角），减去半个宽度让飘字正中在锚点位置
        label.xy = ToWorldLayer(WorldToPanel(anchor, cam) + scatterPx) - new Vector2(label.width * 0.5f, 0f);
        combatLabels.Add(new CombatLabelItem { label = label, time = Time.time, anchor = anchor, scatterPx = scatterPx, risePx = 0f });
    }

    // 命中特效：直接伤害取命中点高度 + 实体所在列，buff/DoT 类贴实体中心；闪避与失效没有命中不播
    private void PlayHitVfx(CombatLabelType type, ushort targetId, bool hasHitPos, Vector3 hitPos)
    {
        if (type == CombatLabelType.Evade || type == CombatLabelType.Invalid) return;
        if (Tool.VfxManager == null || Tool.ClientLogicManager == null || Tool.ClientLogicManager.EntityPlayers == null) return;
        var players = Tool.ClientLogicManager.EntityPlayers;

        if (type == CombatLabelType.Normal || type == CombatLabelType.Crit || type == CombatLabelType.Shield)
        {
            if (!players.TryGetEntityPosition(targetId, out var entityPos)) return; // 视图未建/已移除：飘字兜底逻辑不变，特效不播
            float y;
            if (hasHitPos) y = hitPos.y;
            else if (players.TryGetAnchorPos(targetId, EntityAnchor.Center, out var center)) y = center.y;
            else return;
            Tool.VfxManager.PlayHitVFX(HitVfxIndexDirect, new Vector3(entityPos.x, y, entityPos.z), HitVfxLife);
        }
        else
        {
            if (!players.TryGetAnchorPos(targetId, EntityAnchor.Center, out var center)) return;
            Tool.VfxManager.PlayHitVFX(HitVfxIndexBuff, center, HitVfxLife);
        }
    }

    // 自己被打时的受击动画：只有命中伤害（正常/暴击/护盾）播，DoT 与其它结算不播；连击时从头重播
    private void PlaySelfHitAnim(CombatLabelType type)
    {
        if (type != CombatLabelType.Normal && type != CombatLabelType.Crit && type != CombatLabelType.Shield) return;
        if (panel.m_showHit != null) panel.m_showHit.Play();
    }

    // 相机插值收尾回调：用最终相机变换重投影血条/名字/飘字，消除与渲染差一帧的震颤
    private void OnCameraUpdated()
    {
        UpdateEntityBarPositions();
        ReprojectCombatLabels();
    }

    // 飘字按世界锚点重投影（上升量仍走屏幕像素），使出生点与相机同步、不再滞后一帧
    private void ReprojectCombatLabels()
    {
        var cam = GetProjectionCamera();
        if (cam == null) return;
        for (int i = 0; i < combatLabels.Count; i++)
        {
            var item = combatLabels[i];
            if (item.label == null) continue;
            // 锚点转到相机背后时投影会镜像错位，直接隐藏（与 ShowCombatLabel 的剔除一致）
            if (cam.WorldToScreenPoint(item.anchor).z <= 0f) { item.label.visible = false; continue; }
            item.label.visible = true;
            item.label.xy = ToWorldLayer(WorldToPanel(item.anchor) + item.scatterPx) - new Vector2(item.label.width * 0.5f, 0f) + new Vector2(0f, -item.risePx);
        }
    }

    private void TickCombatLabels(float deltaTime)
    {
        for (int i = combatLabels.Count - 1; i >= 0; i--)
        {
            var item = combatLabels[i];
            float age = Time.time - item.time;
            if (age > CombatLabelLife)
            {
                if (item.label != null) item.label.Dispose();
                combatLabels.RemoveAt(i);
                continue;
            }
            if (item.label == null)
            {
                combatLabels.RemoveAt(i);
                continue;
            }
            item.risePx += 60f * deltaTime;
            combatLabels[i] = item;
            if (age > CombatLabelLife * 0.5f)
                item.label.alpha = Mathf.Clamp01(1f - (age - CombatLabelLife * 0.5f) / (CombatLabelLife * 0.5f));
        }
    }

    private void RefreshTimeIcon()
    {
        if (panel.m_batleInfoPanel.m_icon_day_night == null) return;
        panel.m_batleInfoPanel.m_icon_day_night.rotation = 180f - EnvironmentManager.Time01 * 180f;
    }

    private static string FormatTime(float seconds)
    {
        int s = Mathf.CeilToInt(seconds);
        return $"{s / 60:D2}:{s % 60:D2}";
    }

    private static Texture ResolveSkillIcon(int skillId)
    {
        if (skillId < 0) return null;
        var map = Tool.AssetsManager != null ? Tool.AssetsManager.SkillIconMap : null;
        if (map == null) return null;
        return map.TryGetValue(skillId, out var icon) ? icon : null; // 该技能没配图标 → 不显示图标
    }

    private void RefreshSkillSlot(UI_SkillListItem item, SCEntityDisplayInfo.SkillSlotRuntime slot)
    {
        Texture iconTexture = ResolveSkillIcon(slot.skillId);

        if (item.m_loader_icon != null)
        {
            item.m_loader_icon.texture = iconTexture != null ? new NTexture(iconTexture) : null;
            item.m_loader_icon.visible = iconTexture != null;
        }
        //底图与技能图标一并设置（同一张图，底图在后/图标在前由 prefab 层级保证）
        if (item.m_loader_iconBase != null)
        {
            item.m_loader_iconBase.texture = iconTexture != null ? new NTexture(iconTexture) : null;
            item.m_loader_iconBase.visible = iconTexture != null;
        }


        //CD 与库存：客户端本地推演（TickLocalSkillCd），服务器只下发技能 id 列表，不携带冷却进度
        int storeLimit = SkillManager.GetSkillStore(slot.skillId);
        bool unlimited = storeLimit < 0; // 无限库存没有充能概念，不显示库存数
        item.m_loader_icon.fillAmount = unlimited ? 1f : skillRecharge.GetCycleProgress(slot.skillId);
        if (item.m_storeList != null) item.m_storeList.numItems = unlimited ? 0 : skillRecharge.GetStore(slot.skillId);
    }

    #region 悬浮提示
    private static readonly string[] SkillScoreNames = { "输出", "冷却恢复", "控制", "支援", "攻击范围", "攻击速度" };

    private void ShowSkillTip(int skillId, GObject anchor)
    {
        HideBuffTip();
        var cfg = ConfigManager.SkillConfig.Get(skillId);
        if (skillTip == null) skillTip = UI_SkillTip.CreateInstance();
        if (skillTip.parent == null) Root.AddChild(skillTip);

        skillTip.m_SkillName.text = cfg.name;
        skillTip.m_label_cd.text = $"{cfg.cd:0.#}s";
        skillTip.m_label_store.text = cfg.storeLimit < 0 ? "∞" : cfg.storeLimit.ToString();

        var scores = cfg.scores;
        var list = skillTip.m_score_list;
        list.itemRenderer = (i, obj) =>
        {
            if (obj is not UI_TipLevel row) return;
            row.m_title.text = i < SkillScoreNames.Length ? SkillScoreNames[i] : "";
            row.m_level.selectedIndex = Mathf.Clamp(scores[i], 0, 4); // 0~4 = D/C/B/A/S
        };
        list.numItems = scores.Count;

        skillTip.visible = true;
        PlaceTip(skillTip, anchor);
    }

    private void ShowBuffTip(int buffType, GObject anchor)
    {
        HideSkillTip();
        var cfg = ConfigManager.EffectConfig.Get(buffType);
        if (buffTip == null) buffTip = UI_BuffTip.CreateInstance();
        if (buffTip.parent == null) Root.AddChild(buffTip);

        buffTip.m_buff_name.text = cfg.name;
        buffTip.m_buff_des.text = cfg.description;
        buffTip.visible = true;
        PlaceTip(buffTip, anchor);
    }

    private void HideSkillTip()
    {
        if (skillTip != null) skillTip.visible = false;
    }

    private void HideBuffTip()
    {
        if (buffTip != null) buffTip.visible = false;
    }

    private void HideAllTips()
    {
        HideSkillTip();
        HideBuffTip();
    }

    // 提示摆在锚点正上方居中；顶部放不下就改到锚点下方，左右夹在页面内
    private void PlaceTip(GComponent tip, GObject anchor)
    {
        Vector2 local = Root.GlobalToLocal(anchor.LocalToGlobal(Vector2.zero));
        float x = Mathf.Clamp(local.x + anchor.width * 0.5f - tip.width * 0.5f, 0f, Mathf.Max(0f, Root.width - tip.width));
        float y = local.y - tip.height - 8f;
        if (y < 0f) y = local.y + anchor.height + 8f;
        tip.SetXY(x, y);
    }
    #endregion

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
