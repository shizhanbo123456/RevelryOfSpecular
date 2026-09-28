using Ros.UI.Main;
using System.Collections.Generic;
using Ros.Info;
using Ros.Transport;
using FairyGUI;
using UnityEngine;

/// <summary>
/// 战斗 HUD 逻辑（FGUI）：顶栏时间/昼夜图标、本地玩家大血条（左上角固定）、技能栏（CD=图标填充比例、库存、键位、经验星星）、
/// 守护点血量面板 ×4（中心 + 外围 3）、小地图（10 档位点位）、事件列表、伤害飘字、
/// 名牌（UI_PlayerName + UI_EntityBar 屏幕跟随）、复活进度、结算面板（显示 SettleAutoClose 秒后自动关闭回大厅）。
/// </summary>
public class BattlePage : PageBase
{
    private readonly UI_BattlePanel panel;
    private UI_BattleResult resultPanel;
    private UI_BattleResultDetail resultDetail;
    private float battleStartTime;
    private float settleCloseAt = -1f;

    private readonly List<UI_SkillListItem> skillSlots = new();
    private readonly Dictionary<ushort, UI_PlayerName> nameLabels = new();
    private readonly Dictionary<ushort, UI_EntityBar> entityBars = new();
    private readonly Dictionary<ushort, int> barOwners = new();
    private readonly Dictionary<ushort, UI_MinimapItem> minimapItems = new();
    private readonly List<(UI_DamageLabel label, float time)> damageLabels = new();
    private readonly List<(UI_EventItem item, float time)> eventItems = new();
    private int localPlayerCamp = -1; // 本地玩家阵营（随本地实体摘要更新；小地图敌我识别用）

    private const float DamageLife = 0.8f;
    private const float EventLife = 3.5f;
    private const float SettleAutoClose = 5f;
    private static readonly string[] SlotKeys = { "U", "I", "O", "L", "H" };

    private static readonly Color CampAttackColor = new Color(1f, 0.45f, 0.4f);
    private static readonly Color CampDefenseColor = new Color(0.4f, 0.72f, 1f);

    public BattlePage(UI_BattlePanel panel) : base(panel)
    {
        this.panel = panel;
    }

    public override void Construct()
    {
        panel.m_btn_exit.onClick.Add(() => Tool.NetworkManager?.ExitWorld()); // 切页由 OnRestartGame 事件统一处理
        panel.m_showRegenerationBar.selectedIndex = 0; // 复活进度默认隐藏
    }

    public override void Enter(ShowParam param)
    {
        base.Enter(param);
        EventManager.AddEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
        EventManager.AddEvent<int>(ClientEvent.OnEntityDisplayRemove, OnEntityDisplayRemove);
        EventManager.AddEvent<SCMinimapInfo>(ClientEvent.OnMinimapUpdate, OnMinimapUpdate);
        EventManager.AddEvent<SCScoreInfo>(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.AddEvent<SettlementResult>(ClientEvent.OnSettlementResult, OnSettlementResult);
        EventManager.AddEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.AddEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.AddEvent<string>(ClientEvent.OnRightClickBlocked, OnRightClickBlocked);

        battleStartTime = Time.time;
        localPlayerCamp = -1;
        ResetBeacons();
        ClearEntityBars();
        ClearMinimap();
        HideSettlement();
        RefreshTimeIcon();
    }

    public override void Exit()
    {
        base.Exit();
        EventManager.RemoveEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
        EventManager.RemoveEvent<int>(ClientEvent.OnEntityDisplayRemove, OnEntityDisplayRemove);
        EventManager.RemoveEvent<SCMinimapInfo>(ClientEvent.OnMinimapUpdate, OnMinimapUpdate);
        EventManager.RemoveEvent<SCScoreInfo>(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.RemoveEvent<SettlementResult>(ClientEvent.OnSettlementResult, OnSettlementResult);
        EventManager.RemoveEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.RemoveEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.RemoveEvent<string>(ClientEvent.OnRightClickBlocked, OnRightClickBlocked);
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
        UpdateEntityBarPositions();
        TickDamageLabels(deltaTime);
        TickEventItems();
        //结算面板自动关闭
        if (settleCloseAt > 0f && Time.time >= settleCloseAt) CloseSettlement();
    }

    #region 事件处理
    /// <summary>实体表现摘要：守护点 → 血量面板；玩家角色 → EntityBar 名牌血条；本地玩家 → 大血条 + 技能栏。</summary>
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

    /// <summary>实体移除 → 清理名牌/血条/小地图点位。</summary>
    private void OnEntityDisplayRemove(int entityId)
    {
        RemoveEntityBar((ushort)entityId);
        RemoveMinimapItem((ushort)entityId);
    }

    private void OnLocalSkillBarUpdate(SCEntityDisplayInfo info)
    {
        var skillList = panel.m_skillList;
        if (skillList == null) return;
        //槽位数量变化时增减（模板实例化进容器）；每次摘要到达整表重渲染
        while (skillSlots.Count < info.skills.Count)
        {
            var item = UI_SkillListItem.CreateInstance();
            skillList.AddChild(item);
            item.m_loader_icon.fillMethod = FillMethod.Horizontal;
            skillSlots.Add(item);
        }
        for (int i = skillSlots.Count - 1; i >= info.skills.Count; i--)
        {
            skillSlots[i].Dispose();
            skillSlots.RemoveAt(i);
        }
        for (int i = 0; i < skillSlots.Count; i++)
        {
            RefreshSkillSlot(skillSlots[i], info.skills[i], i);
        }
    }

    /// <summary>守护点摘要 → 中心守护点走 m_progressMain，外围守护点按 value 对号 m_progressSub1~3。</summary>
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
        bar.m_fill.fillAmount = destroyed ? 0f : (info.maxHealth > 0 ? Mathf.Clamp01((float)info.health / info.maxHealth) : 0f);
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

    /// <summary>本地玩家大血条（左上角固定）：等级 + 血量数字 + 血条宽度（按初始满血宽度比例缩放）。</summary>
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
            // 本地玩家等级：按自己阵营取所选角色的局外等级
            int level = Tool.SaveManager == null ? 1
                : info.camp == EntityCamp.Attack ? Tool.SaveManager.GetCharacterLevel(ClientSelection.selectedAttackIndex)
                : Tool.SaveManager.GetCharacterLevel(Config.attack_character_count + ClientSelection.selectedDefenseIndex);
            bar.m_label_level.text = $"Lv{level}";
        }
    }

    private void OnScoreUpdate(SCScoreInfo info)
    {
        if (info == null) return;
        if (panel.m_label_time_left != null) panel.m_label_time_left.text = FormatTime(Mathf.Max(0f, info.remainTime));
    }

    /// <summary>结算面板：显示 + 转场动画，SettleAutoClose 秒后自动关闭回组队大厅。</summary>
    private void OnSettlementResult(SettlementResult r)
    {
        if (resultPanel == null)
        {
            resultPanel = UI_BattleResult.CreateInstance();
            Root.AddChild(resultPanel);
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
        settleCloseAt = Time.time + SettleAutoClose;
    }

    /// <summary>把结算明细逐行写入 BattleResultDetail 的列表；每行单行文本，按本地阵营二选一显示守护点数据。</summary>
    private static void BuildResultRows(UI_BattleResultDetail detail, SettlementResult r)
    {
        detail.m_resultList.RemoveChildren();
        var battle = NetworkManager.battleInfo;
        EntityCamp camp = battle != null ? battle.camp : EntityCamp.None;
        var rows = new List<string>
        {
            $"战斗分数：进攻方 {(int)r.attackScore} ／ 防守方 {(int)r.defenseScore}",
            $"击杀数：{r.killScore}",
            camp == EntityCamp.Attack
                ? $"对防守点伤害：{(int)r.expGain}"
                : $"防守点剩余血量：{(int)r.beaconHealth}",
            $"获得的角色经验：+{r.expGain}",
            $"获得的玩家经验：+{r.expGain}",
        };
        if (r.characterLevelAfter > r.characterLevelBefore)
            rows.Add($"角色升级：{r.characterLevelBefore} → {r.characterLevelAfter}");
        if (r.playerLevelAfter > r.playerLevelBefore)
            rows.Add($"玩家升级：{r.playerLevelBefore} → {r.playerLevelAfter}");
        foreach (var text in rows)
        {
            var item = UI_BattleResultDetailItem.CreateInstance();
            item.m_content.text = text;
            detail.m_resultList.AddChild(item);
        }
    }

    private void CloseSettlement()
    {
        settleCloseAt = -1f;
        if (resultPanel != null) resultPanel.visible = false;
        Tool.ClientLogicManager?.EntityPlayers.ClearAll();
        Tool.UIManager?.TurnPage(PageType.Lobby); // 组队状态保留，点"准备"开启下一轮
    }

    private void HideSettlement()
    {
        settleCloseAt = -1f;
        if (resultPanel != null) resultPanel.visible = false;
    }

    private void OnMinimapUpdate(SCMinimapInfo info)
    {
        if (info.minimapLost)
        {
            ClearMinimap();
            return;
        }
        var mapBase = panel.m_Minimap?.m_mapBase;
        if (mapBase == null) return;
        var seen = new HashSet<ushort>();
        foreach (var entity in info.entities)
        {
            if (entity == null) continue;
            seen.Add(entity.entityId);
            if (!minimapItems.TryGetValue(entity.entityId, out var item))
            {
                item = UI_MinimapItem.CreateInstance();
                panel.m_Minimap.AddChild(item); //GGraph 不是容器，点位挂在 Minimap 面板上
                minimapItems[entity.entityId] = item;
            }
            item.m_type.selectedIndex = GetMinimapType(entity);
            //世界坐标 → 小地图：X+ 向右、Z+ 向上（FGUI y 向下，Z 取反）；坐标含 mapBase 在面板内的偏移
            item.SetXY(mapBase.x + entity.posX / Landscape.MapSize * mapBase.width,
                mapBase.y + (1f - entity.posZ / Landscape.MapSize) * mapBase.height);
        }
        //消失的实体移除点位
        List<ushort> expired = null;
        foreach (var pair in minimapItems)
        {
            if (!seen.Contains(pair.Key)) (expired ??= new List<ushort>()).Add(pair.Key);
        }
        if (expired != null)
        {
            foreach (var id in expired) RemoveMinimapItem(id);
        }
    }

    /// <summary>小地图档位映射：0 自己 / 1 队友 / 2 敌人 / 3 瘟疫树 / 4 水晶 / 5 防御塔 / 6 僵尸 / 7 精英僵尸 / 8 主守护点 / 9 次守护点。</summary>
    private int GetMinimapType(SCMinimapInfo.MinimapEntity entity)
    {
        switch (entity.type.category)
        {
            case EntityCategory.Character_Attack:
            case EntityCategory.Character_Defense:
                if (entity.entityId == NetworkManager.battleInfo?.playerEntityId) return 0;
                return entity.camp == (EntityCamp)localPlayerCamp ? 1 : 2;
            case EntityCategory.PlagueTree: return 3;
            case EntityCategory.Crystal: return 4;
            case EntityCategory.Tower: return 5;
            case EntityCategory.Zombie: return 6;
            case EntityCategory.EliteZombie: return 7;
            case EntityCategory.Beacon: return entity.type == EntityType.CoreBeacon ? 8 : 9;
            default: return 7;
        }
    }

    private void RemoveMinimapItem(ushort entityId)
    {
        if (minimapItems.TryGetValue(entityId, out var item))
        {
            if (item != null) item.Dispose();
            minimapItems.Remove(entityId);
        }
    }

    private void ClearMinimap()
    {
        foreach (var item in minimapItems.Values)
        {
            if (item != null) item.Dispose();
        }
        minimapItems.Clear();
    }

    private void OnBattleEvent(SCBattleEvent e)
    {
        if (e == null) return;
        switch (e.type)
        {
            case SCBattleEvent.Type.Damage:
                ShowDamage(e.value, (ushort)e.targetId);
                break;
            case SCBattleEvent.Type.Kill:
            {
                //「玩家A (图标) 玩家B」：value = 击杀者客户端 id（-1 无归属），targetId = 受害实体（名字按归属反查）
                int killerId = e.value;
                string victimName = barOwners.TryGetValue((ushort)e.targetId, out var owner)
                    ? NetworkManager.GetMemberName(owner) : "玩家";
                string killerName = killerId >= 0 ? NetworkManager.GetMemberName(killerId) : "玩家";
                SpawnEventItem(item =>
                {
                    item.m_type.selectedIndex = 2; // 文字+图标+文字
                    item.m_type2_loader.m_type.selectedIndex = 6; // 玩家间击败
                    item.m_type2_label1.text = killerName;
                    item.m_type2_label2.text = victimName;
                });
                break;
            }
            case SCBattleEvent.Type.BeaconDestroyed:
                SpawnTextEvent("守护点被摧毁！", CampAttackColor);
                break;
            case SCBattleEvent.Type.CrystalCollected:
                SpawnTextEvent("采集水晶，获得收益", new Color(0.42f, 0.85f, 0.55f));
                break;
            case SCBattleEvent.Type.CrystalBroken:
                SpawnTextEvent("该水晶已被感染，无产出", new Color(1f, 0.62f, 0.28f));
                break;
            case SCBattleEvent.Type.PlagueTreeCaptured:
                SpawnEventItem(item =>
                {
                    item.m_type.selectedIndex = 1; // 图标+文字
                    item.m_type1_loader.m_type.selectedIndex = 1; // 瘟疫树被击败
                    item.m_type1_label.text = "攻占瘟疫树！获得瘟疫祝福";
                });
                break;
            case SCBattleEvent.Type.ShowText:
                SpawnTextEvent(NoticeMessageMap.Get(e.value), Color.white);
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

    private void OnRightClickBlocked(string msg)
    {
        Tool.UIManager?.ShowFloating(string.IsNullOrEmpty(msg) ? "该技能无法在此状态下使用" : msg, new Color(1f, 0.62f, 0.28f));
    }
    #endregion

    #region//Local
    /// <summary>创建/刷新玩家名牌：UI_PlayerName（名字，头顶）+ UI_EntityBar（血条，名字下方），阵营配色。</summary>
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

    /// <summary>逐帧：名牌/血条跟随实体头顶（世界 → 屏幕 → 面板局部；相机背面隐藏；名字在血条上方）。</summary>
    private void UpdateEntityBarPositions()
    {
        var cam = Camera.main;
        if (cam == null) return;
        foreach (var pair in entityBars)
        {
            var bar = pair.Value;
            if (bar == null) continue;
            var headPos = Vector3.zero;
            bool visible = Tool.ClientLogicManager?.EntityPlayers?.TryGetEntityHeadPos(pair.Key, out headPos) == true;
            Vector2 local = Vector2.zero;
            if (visible)
            {
                var screen = cam.WorldToScreenPoint(headPos);
                visible = screen.z > 0f;
                if (visible) local = new Vector2(screen.x / UiScale, screen.y / UiScale);
            }
            bar.visible = visible;
            if (visible) bar.xy = local;
        }
        foreach (var pair in nameLabels)
        {
            var name = pair.Value;
            if (name == null) continue;
            //名字挂在血条上方；血条不可见（实体消失/背面）时名字一并隐藏
            bool visible = entityBars.TryGetValue(pair.Key, out var bar) && bar != null && bar.visible;
            name.visible = visible;
            if (visible) name.xy = bar.xy + new Vector2(0f, -26f);
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

    /// <summary>伤害飘字：value 0=无效，>0=普通，<0=暴击；在受击实体头顶生成，上浮+渐隐+到期销毁。</summary>
    private void ShowDamage(int encoded, ushort targetId)
    {
        string text;
        Color color;
        int fontSize;
        if (encoded == 0)
        {
            text = "无效";
            color = new Color(0.7f, 0.7f, 0.7f);
            fontSize = 14;
        }
        else if (encoded > 0)
        {
            text = encoded.ToString();
            color = Color.white;
            fontSize = 18;
        }
        else
        {
            text = $"暴击 {-encoded}";
            color = new Color(1f, 0.62f, 0.28f);
            fontSize = 22;
        }
        var label = UI_DamageLabel.CreateInstance();
        label.m_num.text = text;
        label.m_num.color = color;
        label.m_num.textFormat.size = fontSize;
        Root.AddChild(label);
        var headPos = Vector3.zero;
        if (Tool.ClientLogicManager?.EntityPlayers?.TryGetEntityHeadPos(targetId, out headPos) == true)
        {
            label.xy = WorldToPanel(headPos);
        }
        damageLabels.Add((label, Time.time));
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
            item.label.y -= 60f * deltaTime;
            if (age > DamageLife * 0.5f)
                item.label.alpha = Mathf.Clamp01(1f - (age - DamageLife * 0.5f) / (DamageLife * 0.5f));
        }
    }

    /// <summary>事件条目：加入 m_EventList，纵排，到期移除并重排。</summary>
    private void SpawnEventItem(System.Action<UI_EventItem> setup)
    {
        var container = panel.m_EventList;
        if (container == null) return;
        var item = UI_EventItem.CreateInstance();
        container.AddChild(item);
        setup(item);
        eventItems.Add((item, Time.time));
        RelayoutEventItems();
    }

    private void SpawnTextEvent(string text, Color color)
    {
        SpawnEventItem(item =>
        {
            item.m_type.selectedIndex = 0; // 纯文字
            item.m_type0_label.text = text;
            item.m_type0_label.color = color;
        });
    }

    private void TickEventItems()
    {
        bool removed = false;
        for (int i = eventItems.Count - 1; i >= 0; i--)
        {
            var entry = eventItems[i];
            if (Time.time - entry.time > EventLife)
            {
                if (entry.item != null) entry.item.Dispose();
                eventItems.RemoveAt(i);
                removed = true;
            }
        }
        if (removed) RelayoutEventItems();
    }

    private void RelayoutEventItems()
    {
        for (int i = 0; i < eventItems.Count; i++)
        {
            if (eventItems[i].item != null) eventItems[i].item.SetXY(12f, 10f + i * 64f);
        }
    }

    /// <summary>昼夜图标：Time01（1=正午，0=午夜）线性映射到绕 Z 的 0°~180°。</summary>
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
            default: return Color.white;
        }
    }

    private static string FormatTime(float seconds)
    {
        int s = Mathf.CeilToInt(seconds);
        return $"{s / 60:D2}:{s % 60:D2}";
    }

    /// <summary>标题文字：本地玩家阵营获胜显示"胜利"，其余（败北/平局）显示"结束"。</summary>
    private static string GetEndText(int gameState, EntityCamp camp)
    {
        bool win = (gameState == 1 && camp == EntityCamp.Attack)
                || (gameState == 2 && camp == EntityCamp.Defense);
        return win ? "胜利" : "结束";
    }

    /// <summary>技能槽刷新：图标（SkillInfo.icon，底图与图标一并设置）、CD=图标填充比例（0→100 一轮冷却）、库存、键位、经验星星（exp 与星星 1:1）。</summary>
    private void RefreshSkillSlot(UI_SkillListItem item, SCEntityDisplayInfo.SkillSlotRuntime slot, int index)
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
        if (item.m_key != null) item.m_key.text = index < SlotKeys.Length ? SlotKeys[index] : "";
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
