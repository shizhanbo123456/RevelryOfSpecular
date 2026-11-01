using Ros.UI.Main;
using System.Collections.Generic;
using Ros.Transport;
using FairyGUI;
using UnityEngine;

public class BattlePage : PageBase
{
    private readonly UI_BattlePanel panel;

    private float skillCdRefreshTimer;   // 冷却环重渲染节流（本地推演需要周期刷新）
    private float nextQuickMessageTime;  // 快捷消息发送节流（防刷）
    private readonly List<UI_SkillList.Slot> skillSlots = new(); // 技能栏快照缓冲（每 0.1s 重填一次，避免分配）
    private readonly Dictionary<ushort, UI_EntityBar> entityBars = new();
    // 血量缓存：摘要包只在 healthDirty/全量时携带血量段，未携带的包（如仅 buff 变化）不能把血条刷成 0/0
    private readonly Dictionary<ushort, (int health, int maxHealth)> entityVitals = new();
    private readonly List<UI_EntityBar.Buff> barBuffs = new(); // 血条 buff 快照缓冲（每个摘要包重填）
    private readonly List<SCEntityDisplayInfo.BuffRuntime> localBuffs = new();                      // 本地玩家左上角血条的 buff 数据源
    private readonly float barDefaultSizeDistance = ConfigManager.GlobalVariableConfig.distance_of_bar_default_size; // 血条 scale=(1,1) 时的相机距离
    private Texture2D mapAreaTexture;    // AssetsManager.MapArea 的 Texture2D 引用（贴图需开 Read/Write）

    private int localPlayerArea = -1;    // 本地玩家当前区域（EventBanner 页 index，-1=未知；死亡观战期间冻结）
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

    // 客户端技能状态（持有表 / 技能栏 / 冷却库存）全在它身上
    private static ClientSkillManager Skills => Tool.ClientLogicManager != null ? Tool.ClientLogicManager.SkillVfx : null;

    // 剩余时间推演的唯一持有者（起点由服务器快照校准，见 ClientBattleTimeManager）
    private static ClientBattleTimeManager BattleTime => Tool.ClientLogicManager != null ? Tool.ClientLogicManager.BattleTime : null;

    private static ushort OwnEntityId => HasSession ? Session.OwnEntityId : (ushort)0;
    private bool IsOwnEntity(int entityId) => HasSession && OwnEntityId == entityId;

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
        panel.m_PlayerBar.m_buffList.m_list.itemRenderer =
            (i, obj) => ((UI_buffItem)obj).Fill(localBuffs[i].type, localBuffs[i].level, ShowBuffTip, HideBuffTip);
        panel.m_skillSelectPanel.Build(OnSkillSelectCommitted);
        panel.m_skillSelectPanel.Close();
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

    // 快捷消息面板：收起时按键展开；展开时按键发送并收起（按住 J 时附带相机瞄准点）
    private void HideQuickMessage() => SetQuickMessageVisible(false);

    private void SetQuickMessageVisible(bool visible)
    {
        panel.m_quickMessagePanel.visible = visible;
        panel.m_quickMessagePanel.touchable = visible; // 收起时不吃点击
    }

    private void HandleQuickMessageKey(int index)
    {
        if (!panel.m_quickMessagePanel.visible)
        {
            SetQuickMessageVisible(true);
            return;
        }
        SendQuickMessage(index);
        SetQuickMessageVisible(false);
    }

    private void SendQuickMessage(int index)
    {
        float now = Time.time;
        if (now < nextQuickMessageTime) return; // 防刷：同玩家 1s 一条
        nextQuickMessageTime = now + Config.ai_message_min_interval;

        var message = new CSQuickMessage() { messageId = index };
        if (Input.GetKey(Config.melee_key) && TryGetAimPoint(out var aim))
        {
            message.hasTargetPosition = true;
            message.targetPosition = aim;
        }
        if (Tool.NetworkManager != null) Tool.NetworkManager.SendQuickMessage(message);
    }

    // 相机屏幕中心射线与地面 y=0 的交点；取不到就按"无位置"发送
    private static bool TryGetAimPoint(out Vector3 point)
    {
        point = default;
        var cam = Tool.CameraController != null ? Tool.CameraController.WorldCamera : null;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        if (Mathf.Abs(ray.direction.y) < 0.0001f) return false;
        float distance = -ray.origin.y / ray.direction.y;
        if (distance <= 0f) return false;
        point = ray.origin + ray.direction * distance;
        return true;
    }

    private static bool AnyQuickMessageKeyDown(out int index)
    {
        var keys = Config.quick_message_keys;
        for (int i = 0; i < keys.Length; i++)
        {
            if (!Input.GetKeyDown(keys[i])) continue;
            index = i;
            return true;
        }
        index = -1;
        return false;
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
        EventManager.AddEvent(ClientEvent.OnSkillBarRefresh, OnSkillBarRefresh);
        if (Tool.CameraController != null) Tool.CameraController.OnCameraUpdated += OnCameraUpdated;

        localPlayerCamp = -1;
        spectatorMode = Session != null && Session.IsSpectator;
        localPlayerAlive = !spectatorMode;
        localPlayerArea = -1;
        panel.m_onlook.selectedIndex = spectatorMode ? 1 : 0; // onlook=1：FGUI 自动隐藏血条/技能等非观战元素
        if (panel.m_onlookComboBox != null) panel.m_onlookComboBox.onChanged.Set(OnOnlookComboBoxChanged);
        RefreshCampAliveCount(NetworkManager.LatestCampAliveCount); // 进入战斗先填一次缓存值，随后由变更包刷新
        // 首页/大厅展示的选角预览模型只属于那两个界面，进战斗前清掉（否则会残留在地图的预览锚点上）
        if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.HomePreview != null)
            Tool.ClientLogicManager.HomePreview.Hide();
        ResetGuardStatues();
        HideSettings(); // 设置面板每局从收起状态开始
        HideQuickMessage(); // 快捷消息面板每局从收起状态开始
        HideImportantBanner(); // 重要事件 banner 每局从隐藏状态开始
        OnDefensePointProgress(NetworkManager.LatestDefensePointProgress); // 进入战斗先填一次缓存值，随后由变更包刷新
        ClearEntityBars();
        HideAllTips();
        localBuffs.Clear();
        panel.m_PlayerBar.m_buffList.m_list.numItems = 0;
        panel.m_Minimap?.Reset(ConfigManager.GlobalVariableConfig.minimap_view_radius[0]); //每局回到默认档（首档），与服务器 ClearBattleState 对齐
        panel.m_skillSelectPanel.Close();
        Skills?.ResetBattle();
        BuildSkillSlots();
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
        EventManager.RemoveEvent(ClientEvent.OnSkillBarRefresh, OnSkillBarRefresh);
        if (Tool.CameraController != null) Tool.CameraController.OnCameraUpdated -= OnCameraUpdated;
        spectatorMode = false;
        localPlayerAlive = true;
        spectateTargetClientId = -1;
        spectateTargetEntityId = -1;
        if (panel.m_onlook != null) panel.m_onlook.selectedIndex = 0;
        panel.m_skillSelectPanel.Close();
        HideAllTips();
        HideImportantBanner();
    }

    public override void Tick(float deltaTime)
    {
        // 键盘被 UI 占用（GM 面板 / 文字输入）时，技能选择与快捷消息都不响应
        if (!InputManager.KeysCapturedByUi)
        {
            if (Input.GetKeyDown(Config.skill_select_key)) ToggleSkillSelect();
            if (AnyQuickMessageKeyDown(out int quickMessageIndex)) HandleQuickMessageKey(quickMessageIndex);
        }
        //剩余时间（以快照校准的本地推演为准，重连/中途进入的起点由服务器补发）
        if (HasSession && BattleTime != null && panel.m_batleInfoPanel.m_label_time_left != null)
            panel.m_batleInfoPanel.m_label_time_left.text = FormatTime(Mathf.Max(0f, BattleTime.RemainTime));
        RefreshTimeIcon();
        TickMinimap();
        TickSpectator(deltaTime);
        TickLocalSkillCd(deltaTime);
    }

    // 冷却推演在 ClientSkillManager，这里只按节流重渲染冷却环与库存数
    private void TickLocalSkillCd(float deltaTime)
    {
        if (spectatorMode || panel.m_skillList == null || !panel.m_skillList.IsBuilt) return;
        skillCdRefreshTimer -= deltaTime;
        if (skillCdRefreshTimer > 0f) return;
        skillCdRefreshTimer = 0.1f;
        RefreshSkillBar();
    }

    // 持有表变化 / 选择面板应用 / 消耗回执：下一帧立刻重画技能栏
    private void OnSkillBarRefresh()
    {
        if (panel.m_skillList == null || !panel.m_skillList.IsBuilt) BuildSkillSlots(); // 首包到达时兜底搭栏
        skillCdRefreshTimer = 0f;
        panel.m_skillSelectPanel.RefreshHeld(Skills?.HeldSkills); // 打开期间新获得的技能要进列表
    }

    #region 事件处理
    private void OnEntityDisplayUpdate(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        // 血条是否创建由 entity.csv 的「显示血条」列决定；名牌只给双方角色
        if (ConfigManager.EntityConfig.GetByCategory(info.type).showHealthBar)
        {
            bool withName = info.type.category == EntityCategory.Character_Attack ||
                            info.type.category == EntityCategory.Character_Defense;
            UpdateEntityBar(info, withName);
        }

        if (IsOwnEntity(info.entityId))
        {
            localPlayerCamp = (int)info.camp;
            localPlayerAlive = true;
            spectateTargetClientId = -1; // 本地实体出现（复活/正常出战）：服务器已退出观战，视角回自身
            spectateTargetEntityId = -1;
            UpdateLocalPlayerBar(info);
        }
        // 纯观战无本地实体：以观战目标阵营作为「我方」基准（名牌敌我配色、小地图点位敌我识别）
        else if (spectatorMode && info.entityId == spectateTargetEntityId) localPlayerCamp = (int)info.camp;
    }

    private void OnEntityDisplayRemove(int entityId)
    {
        RemoveEntityBar((ushort)entityId);
        entityVitals.Remove((ushort)entityId);
        panel.m_Minimap?.Remove((ushort)entityId);

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
                if (unit.clientId == NetworkManager.LocalToken) continue; // 自己不进观战列表
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

    // 技能栏条目数 = 本局角色技能槽位数
    private void BuildSkillSlots()
    {
        int count = Skills != null ? Skills.SlotCapacity : 0;
        if (count <= 0) count = 1; // 观战/属性取不到时至少露出一格
        panel.m_skillList?.Build(count, ShowSkillTip, HideSkillTip);
        RefreshSkillBar();
    }

    // 只做取数：槽位技能、恢复进度、库存次数来自 ClientSkillManager，绘制归技能栏自身
    private void RefreshSkillBar()
    {
        if (panel.m_skillList == null || !panel.m_skillList.IsBuilt) return;
        skillSlots.Clear();
        int count = panel.m_skillList.SlotCount;
        var recharge = Skills != null ? Skills.Recharge : null;
        for (int i = 0; i < count; i++)
        {
            int skillId = Skills != null ? Skills.GetSlotSkillId(i) : -1;
            if (skillId < 0)
            {
                skillSlots.Add(new UI_SkillList.Slot(-1, 1f, 0, true));
                continue;
            }
            // 无限库存没有充能概念，不显示次数
            bool unlimited = SkillManager.GetSkillStore(skillId) < 0;
            skillSlots.Add(new UI_SkillList.Slot(skillId,
                unlimited || recharge == null ? 1f : recharge.GetCycleProgress(skillId),
                unlimited || recharge == null ? 0 : recharge.GetStore(skillId),
                unlimited));
        }
        panel.m_skillList.Refresh(skillSlots);
    }

    #region 技能选择面板
    private void ToggleSkillSelect()
    {
        if (spectatorMode || !HasSession || panel.m_skillList == null || !panel.m_skillList.IsBuilt) return;
        if (panel.m_skillSelectPanel.IsOpen) panel.m_skillSelectPanel.Commit(); // 关闭即应用，与关闭按钮同一路径
        else panel.m_skillSelectPanel.Open(Skills?.HeldSkills, Skills?.ActiveSkills, Skills != null ? Skills.SlotCapacity : 0);
    }

    // 关闭回调：勾选集合按技能 id 升序进栏（排序在 ClientSkillManager 里做）
    private void OnSkillSelectCommitted()
    {
        if (Skills != null) Skills.TrySetActiveSkills(panel.m_skillSelectPanel.Selection);
        RefreshSkillBar();
    }
    #endregion

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
        var vitals = ResolveVitals(info);
        bar.m_bar_fill_health.fillAmount = vitals.maxHealth > 0 ? Mathf.Clamp01((float)vitals.health / vitals.maxHealth) : 0f;
        if (bar.m_label_health != null) bar.m_label_health.text = $"{vitals.health}/{vitals.maxHealth}";
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
        // 每个实体一个独立包：只登记坐标与类别，换算和显隐交给小地图自身
        panel.m_Minimap?.Push(new UI_Minimap.Point(e.entityId, e.type.category, e.type, e.camp,
            e.marked, IsOwnEntity(e.entityId), new Vector2(e.posX, e.posZ)), (EntityCamp)localPlayerCamp);
    }

    //F 键切换雷达显示半径：服务器权威回应，客户端换用新半径做裁剪与缩放（点位下一帧即按新半径重排）
    private void OnMinimapRadiusUpdate(float radius)
    {
        panel.m_Minimap?.SetRadius(radius);
    }

    // 圆心与自身朝向的取数在这边（相机/会话/实体表现都在战斗页的依赖里），换算与绘制全在小地图自身
    private void TickMinimap()
    {
        bool hasCenter = TryGetMinimapCenter(out var center);
        bool hasYaw = TryGetSelfYaw(out float yaw);
        panel.m_Minimap?.Tick(center, hasCenter, yaw, hasYaw);
        if (hasCenter) TickLocalPlayerArea(center);
    }

    // 自身图标随朝向旋转：地图不转，图标转（每帧读本地玩家推演朝向）
    private bool TryGetSelfYaw(out float yaw)
    {
        yaw = 0f;
        if (!HasSession || Tool.ClientLogicManager == null
            || Tool.ClientLogicManager.EntityPlayers == null) return false;
        if (!Tool.ClientLogicManager.EntityPlayers.TryGetEntityTransform(OwnEntityId, out _, out var rot)) return false;
        yaw = rot.eulerAngles.y; //FGUI 顺时针角度与 Unity yaw 同向（图标默认朝上时）
        return true;
    }

    // 雷达中心 = 本地玩家实体位置；纯观战无本地实体时取观战目标位置；自由视角下取相机自身
    private bool TryGetMinimapCenter(out Vector3 pos)
    {
        pos = Vector3.zero;
        // 自由视角相机已脱离跟随点：圆心必须跟相机，否则服务器按相机下发的点位会被按旧圆心换算而漂出雷达
        var cam = Tool.CameraController;
        if (cam != null && cam.IsFree)
        {
            pos = cam.transform.position;
            return true;
        }
        if (!HasSession || Tool.ClientLogicManager == null
            || Tool.ClientLogicManager.EntityPlayers == null) return false;
        ushort centerId = OwnEntityId;
        if (centerId == 0 && spectateTargetEntityId > 0) centerId = (ushort)spectateTargetEntityId;
        return centerId != 0 && Tool.ClientLogicManager.EntityPlayers.TryGetEntityPosition(centerId, out pos);
    }

    // 区域横幅的数据源：圆心由小地图那边算出后传进来，EventBanner 本身属于 BattlePanel（日出/日落也走它）
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
            bar = UI_EntityBar.CreateInstance();
            panel.m_world_element_container.AddChild(bar); // 挂在世界层：压在战斗界面各元素之下
            entityBars[info.entityId] = bar;
        }
        string name = withName && HasSession ? Session.GetNameByEntity(info.entityId) : "";
        var vitals = ResolveVitals(info);
        bar.Bind(vitals.health, vitals.maxHealth, info.camp, (EntityCamp)localPlayerCamp, name,
            info.hasBuffs ? ToBarBuffs(info.buffs) : null, ShowBuffTip, HideBuffTip);
    }

    // 血量段只在置脏/全量包携带：带则刷新缓存，不带则沿用上次值（首见包必为全量）
    private (int health, int maxHealth) ResolveVitals(SCEntityDisplayInfo info)
    {
        if (info.hasHealth) entityVitals[info.entityId] = (info.health, info.maxHealth);
        return entityVitals.TryGetValue(info.entityId, out var vitals) ? vitals : default;
    }

    // 协议 buff 列表 → 血条条目数据（复用缓冲，避免每个摘要包都分配）
    private IReadOnlyList<UI_EntityBar.Buff> ToBarBuffs(List<SCEntityDisplayInfo.BuffRuntime> source)
    {
        barBuffs.Clear();
        if (source != null)
            for (int i = 0; i < source.Count; i++)
                if (source[i] != null) barBuffs.Add(new UI_EntityBar.Buff(source[i].type, source[i].level));
        return barBuffs;
    }

    // 本地玩家血条的 buff 列表：协议数据拷进缓冲后按条数重建
    private static void RefreshBuffList(GList list, List<SCEntityDisplayInfo.BuffRuntime> target, List<SCEntityDisplayInfo.BuffRuntime> source)
    {
        target.Clear();
        if (source != null) target.AddRange(source);
        list.numItems = target.Count;
    }

    // 头顶锚点由实体表现提供，投影与缩放归血条自身
    private void UpdateEntityBarPositions()
    {
        if (entityBars.Count == 0) return;
        foreach (var pair in entityBars)
        {
            var bar = pair.Value;
            if (bar == null) continue;
            bool hasAnchor = false;
            var headPos = Vector3.zero;
            if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.EntityPlayers != null)
                hasAnchor = Tool.ClientLogicManager.EntityPlayers.TryGetAnchorPos(pair.Key, EntityAnchor.Bar, out headPos);
            bar.Place(hasAnchor, headPos, barDefaultSizeDistance);
        }
    }

    private void RemoveEntityBar(ushort entityId)
    {
        if (entityBars.TryGetValue(entityId, out var bar))
        {
            if (bar != null) bar.Dispose();
            entityBars.Remove(entityId);
        }
    }

    private void ClearEntityBars()
    {
        foreach (var bar in entityBars.Values)
        {
            if (bar != null) bar.Dispose();
        }
        entityBars.Clear();
    }

    private void ShowCombatLabel(int value, CombatLabelType type, ushort targetId, bool hasHitPos, Vector3 hitPos)
    {
        // 锚点：有命中点用命中点，否则退到实体头顶（飘字与命中特效共用这套兜底）
        Vector3 anchor = hitPos;
        if (!hasHitPos)
        {
            anchor = Vector3.zero;
            if (Tool.ClientLogicManager != null && Tool.ClientLogicManager.EntityPlayers != null)
                Tool.ClientLogicManager.EntityPlayers.TryGetAnchorPos(targetId, EntityAnchor.LabelFallBack, out anchor);
        }
        UI_DamageLabel.Spawn(panel.m_world_element_container, value, type, anchor);
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
    private void OnCameraUpdated() => UpdateEntityBarPositions();

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

    #region 悬浮提示
    private void ShowSkillTip(int skillId, GObject anchor)
    {
        HideBuffTip();
        if (skillTip == null) skillTip = UI_SkillTip.CreateInstance();
        if (skillTip.parent == null) Root.AddChild(skillTip);
        skillTip.Fill(skillId);
        skillTip.ShowAt(anchor);
    }

    private void ShowBuffTip(int buffType, GObject anchor)
    {
        HideSkillTip();
        if (buffTip == null) buffTip = UI_BuffTip.CreateInstance();
        if (buffTip.parent == null) Root.AddChild(buffTip);
        buffTip.Fill(buffType);
        buffTip.ShowAt(anchor);
    }

    private void HideSkillTip() => skillTip?.Hide();

    private void HideBuffTip() => buffTip?.Hide();

    private void HideAllTips()
    {
        HideSkillTip();
        HideBuffTip();
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
