using System.Collections.Generic;
using Ros.Info;
using Ros.Transport;
using UnityEngine;
using UnityEngine.UI;

public partial class BattlePage : RosPage
{
    private float battleStartTime;

    // 名牌与伤害飘字（屏幕空间 uGUI）
    private readonly Dictionary<ushort, EntityNameBarItem> nameBars = new();
    private readonly List<(Text label, float time)> damageLabels = new();
    private const float DamageLife = 0.8f;

    // 阵营配色（攻红/守蓝，语义状态色）
    private static readonly Color CampAttackColor = new Color(1f, 0.45f, 0.4f);
    private static readonly Color CampDefenseColor = new Color(0.4f, 0.72f, 1f);

    public override void Init()
    {
        settlementPanel.SetCloseCallback(OnSettleClose);
    }

    public override void Enter(ShowParam param)
    {
        EventManager.AddEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
        EventManager.AddEvent<int>(ClientEvent.OnEntityDisplayRemove, OnEntityDisplayRemove);
        EventManager.AddEvent<SCScoreInfo>(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.AddEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.AddEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.AddEvent<string>(ClientEvent.OnRightClickBlocked, OnRightClickBlocked);

        //开局信息立即应用
        battleStartTime = Time.time;
        beaconInfoListWrapper?.SetItemCount(Config.outer_beacon_count);
        ClearNameBars();
        settlementPanel.Hide();
        RefreshTimeIcon();
    }

    public override void Exit()
    {
        EventManager.RemoveEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
        EventManager.RemoveEvent<int>(ClientEvent.OnEntityDisplayRemove, OnEntityDisplayRemove);
        EventManager.RemoveEvent<SCScoreInfo>(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.RemoveEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.RemoveEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.RemoveEvent<string>(ClientEvent.OnRightClickBlocked, OnRightClickBlocked);
    }

    public override void Tick(float deltaTime)
    {
        //剩余时间（本地估算，精确值以服务器 SCScoreInfo 为准）
        if (NetworkManager.battleInfo != null && timeLabel != null)
        {
            float remain = Config.battle_duration - (Time.time - battleStartTime);
            timeLabel.text = FormatTime(Mathf.Max(0f, remain));
        }
        //昼夜图标（Time01 由 EnvironmentManager 客户端推演连续变化，逐帧跟随）
        RefreshTimeIcon();
        UpdateNameBarPositions();
        TickDamageLabels();
    }

    #region 事件处理
    /// <summary>实体表现摘要：守护点 → 右侧面板；玩家角色 → 名牌；本地玩家 → 底部技能栏。</summary>
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
            UpdateNameBar(info);
        }

        if (NetworkManager.battleInfo != null && info.entityId == NetworkManager.battleInfo.playerEntityId)
        {
            OnLocalSkillBarUpdate(info);
        }
    }

    private void OnLocalSkillBarUpdate(SCEntityDisplayInfo info)
    {
        if (skillListWrapper == null) return;
        //每次摘要到达整表重渲染：CD/库存/经验/选中态都随最新槽位数据走
        var skills = info.skills;
        skillListWrapper.itemRenderer = (item, i) =>
            item.Refresh(skills[i], i == info.selectedIndex, i);
        skillListWrapper.SetItemCount(skills.Count);
    }

    /// <summary>守护点摘要 → 中心守护点走固定单槽，外围守护点按 type.value 对号入座 RosList。</summary>
    private void OnBeaconDisplay(SCEntityDisplayInfo info)
    {
        bool destroyed = info.health <= 0;
        if (info.type == EntityType.CoreBeacon)
        {
            if (centerBeaconInfo == null) return;
            if (destroyed) centerBeaconInfo.SetDestroyed();
            else centerBeaconInfo.Refresh(info);
            return;
        }

        if (beaconInfoListWrapper == null) return;
        int index = info.type.value; // 外围守护点：Beacon(0~outer_beacon_count-1)，value 即列表下标
        if (index < 0 || index >= beaconInfoListWrapper.Count) return;
        var item = beaconInfoListWrapper.GetItem(index);
        if (destroyed) item.SetDestroyed();
        else item.Refresh(info);
    }

    /// <summary>实体移除 → 清理对应名牌。</summary>
    private void OnEntityDisplayRemove(int entityId)
    {
        if (nameBars.TryGetValue((ushort)entityId, out var item))
        {
            if (item != null) Destroy(item.gameObject);
            nameBars.Remove((ushort)entityId);
        }
    }

    private void ClearNameBars()
    {
        foreach (var item in nameBars.Values)
        {
            if (item != null) Destroy(item.gameObject);
        }
        nameBars.Clear();
    }

    /// <summary>创建/刷新玩家名牌（名字+阵营色+血量比）。</summary>
    private void UpdateNameBar(SCEntityDisplayInfo info)
    {
        if (nameBarPanel == null || nameBarTemplate == null) return;
        if (!nameBars.TryGetValue(info.entityId, out var item))
        {
            item = Instantiate(nameBarTemplate, nameBarPanel);
            nameBars[info.entityId] = item;
        }
        item.gameObject.SetActive(true);

        bool isAttack = info.camp == EntityCamp.Attack;
        Color campColor = isAttack ? CampAttackColor : CampDefenseColor;
        if (item.NameText != null)
        {
            item.NameText.text = $"玩家{info.ownerClientId}";
            item.NameText.color = campColor;
        }
        if (item.HealthFill != null)
        {
            item.HealthFill.color = campColor;
            item.HealthFill.fillAmount = info.maxHealth > 0 ? Mathf.Clamp01((float)info.health / info.maxHealth) : 0f;
        }
    }

    /// <summary>逐帧：名牌跟随实体头顶（世界坐标 → 屏幕坐标；相机背面隐藏）。</summary>
    private void UpdateNameBarPositions()
    {
        if (nameBars.Count == 0) return;
        var cam = Camera.main;
        if (cam == null) return;
        foreach (var pair in nameBars)
        {
            var item = pair.Value;
            if (item == null) continue;
            var players = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.EntityPlayers : null;
            var headPos = Vector3.zero;
            bool visible = players != null && players.TryGetEntityHeadPos(pair.Key, out headPos);
            var screen = visible ? cam.WorldToScreenPoint(headPos) : Vector3.zero;
            visible = visible && screen.z > 0f; // 相机背面不可见
            item.gameObject.SetActive(visible);
            if (visible) item.transform.position = screen;
        }
    }

    private void OnScoreUpdate(SCScoreInfo info)
    {
        if (info == null) return;
        if (timeLabel != null) timeLabel.text = FormatTime(Mathf.Max(0f, info.remainTime));
        if (info.gameState != 0)
        {
            Tool.UIManager?.ShowFloating(GetEndText(info.gameState), GetEndColor(info.gameState));
            settlementPanel.Show(info);
        }
    }

    private void OnSettleClose()
    {
        settlementPanel.Hide();
        Tool.ClientLogicManager?.EntityPlayers.ClearAll();
        Tool.UIManager?.TurnPage(PageType.Lobby); // 组队状态保留，点"准备"开启下一轮
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
                Tool.UIManager?.ShowFloating("击杀！", CampAttackColor);
                break;
            case SCBattleEvent.Type.BeaconDestroyed:
                Tool.UIManager?.ShowFloating("守护点被摧毁！", new Color(1f, 0.32f, 0.3f));
                break;
            case SCBattleEvent.Type.CrystalCollected:
                Tool.UIManager?.ShowFloating("采集水晶，获得收益", new Color(0.42f, 0.85f, 0.55f));
                break;
            case SCBattleEvent.Type.CrystalBroken:
                Tool.UIManager?.ShowFloating("该水晶已被感染，无产出", new Color(1f, 0.62f, 0.28f));
                break;
            case SCBattleEvent.Type.PlagueTreeCaptured:
                Tool.UIManager?.ShowFloating("攻占瘟疫树！获得瘟疫祝福", CampDefenseColor);
                break;
            case SCBattleEvent.Type.ShowText:
                //消息提示控件尚未在 uGUI 重建，暂以飘字代替
                Tool.UIManager?.ShowFloating(NoticeMessageMap.Get(e.value), Color.white);
                break;
        }
    }

    private void OnReviveProgressUpdate(SCReviveInfo info)
    {
        if (info == null) return;
        bool isLocal = NetworkManager.battleInfo != null && info.entityId == NetworkManager.battleInfo.playerEntityId;
        if (!isLocal || revivePanel == null) return;
        revivePanel.SetActive(!info.ready);
        if (info.ready) return;
        if (reviveFill != null) reviveFill.fillAmount = Mathf.Clamp01(info.progress);
        if (reviveLabel != null)
            reviveLabel.text = $"复活中 {Mathf.RoundToInt(info.progress * 100f)}%   （愈战愈勇 ×{info.yzStack}）";
    }

    private void OnRightClickBlocked(string msg)
    {
        Tool.UIManager?.ShowFloating(string.IsNullOrEmpty(msg) ? "该技能无法在此状态下使用" : msg, new Color(1f, 0.62f, 0.28f));
    }
    #endregion

    #region//Local
    /// <summary>伤害飘字：value 0=无效，>0=普通伤害，<0=暴击（绝对值为伤害量）；在受击实体头顶生成。</summary>
    private void ShowDamage(int encoded, ushort targetId)
    {
        if (damagePanel == null || damageTextTemplate == null) return;
        string text;
        Color color;
        int fontSize;
        if (encoded == 0)
        {
            text = "无效";
            color = new Color(0.7f, 0.7f, 0.7f);
            fontSize = 16;
        }
        else if (encoded > 0)
        {
            text = encoded.ToString();
            color = Color.white;
            fontSize = 20;
        }
        else
        {
            text = $"暴击 {-encoded}";
            color = new Color(1f, 0.62f, 0.28f);
            fontSize = 26;
        }
        var label = Instantiate(damageTextTemplate, damagePanel);
        label.text = text;
        label.color = color;
        label.fontSize = fontSize;
        //定位：受击实体头顶（屏幕坐标；假设 Canvas 为 Screen Space - Overlay）
        if (Tool.ClientLogicManager?.EntityPlayers?.TryGetEntityHeadPos(targetId, out var headPos) == true
            && Camera.main != null)
        {
            label.transform.position = Camera.main.WorldToScreenPoint(headPos);
        }
        damageLabels.Add((label, Time.time));
    }

    /// <summary>伤害飘字逐帧：上浮 + 后半段渐隐 + 到期销毁。</summary>
    private void TickDamageLabels()
    {
        for (int i = damageLabels.Count - 1; i >= 0; i--)
        {
            var item = damageLabels[i];
            float age = Time.time - item.time;
            if (age > DamageLife)
            {
                if (item.label != null) Destroy(item.label.gameObject);
                damageLabels.RemoveAt(i);
                continue;
            }
            if (item.label == null)
            {
                damageLabels.RemoveAt(i);
                continue;
            }
            item.label.transform.position += Vector3.up * (60f * Time.deltaTime);
            if (age > DamageLife * 0.5f) item.label.CrossFadeAlpha(0f, DamageLife * 0.5f, false);
        }
    }

    /// <summary>昼夜图标：Time01（1=正午，0=午夜）线性映射到绕 Z 的 0°~180°。</summary>
    private void RefreshTimeIcon()
    {
        if (timeIconPivot == null) return;
        timeIconPivot.localEulerAngles = new Vector3(0f, 0f, 180f - EnvironmentManager.Time01 * 180f);
    }

    private static Color GetEndColor(int gameState)
    {
        switch (gameState)
        {
            case 1: return CampAttackColor;  // 进攻方胜利：红
            case 2: return CampDefenseColor; // 防守方胜利：蓝
            default: return Color.white;     // 平局
        }
    }

    private static string FormatTime(float seconds)
    {
        int s = Mathf.CeilToInt(seconds);
        return $"{s / 60:D2}:{s % 60:D2}";
    }

    private static string GetEndText(int gameState)
    {
        switch (gameState)
        {
            case 1: return "进攻方胜利";
            case 2: return "防守方胜利";
            default: return "平局";
        }
    }
    #endregion
}
