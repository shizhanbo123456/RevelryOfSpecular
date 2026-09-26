using System.Collections.Generic;
using Ros.Info;
using Ros.Transport;
using UnityEngine;
using UnityEngine.UI;

public partial class BattlePage : RosPage
{
    private const string DayName = "白天";
    private const string NightName = "晚上";
    private const float FloatingLife = 2.5f;

    private readonly List<BattleSkillItem> skillSlots = new();
    private readonly Dictionary<ushort, BeaconBarItem> beaconBars = new();
    private readonly List<(Text label, float time)> floatingLabels = new();
    private float battleStartTime;

    public override void Enter(ShowParam param)
    {
        EventManager.AddEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
        EventManager.AddEvent<SCScoreInfo>(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.AddEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.AddEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.AddEvent<string>(ClientEvent.OnRightClickBlocked, OnRightClickBlocked);
        EventManager.AddEvent<int>(ClientEvent.OnDayNightChange, OnDayNightChange);

        //开局信息立即应用：清上一局残留（守护点 entityId 每局不同，旧条目直接作废）
        battleStartTime = Time.time;
        ClearBeacons();
        HideSettlement();
        RefreshDayNightLabel();
    }

    public override void Exit()
    {
        EventManager.RemoveEvent<SCEntityDisplayInfo>(ClientEvent.OnEntityDisplayUpdate, OnEntityDisplayUpdate);
        EventManager.RemoveEvent<SCScoreInfo>(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.RemoveEvent<SCBattleEvent>(ClientEvent.OnBattleEvent, OnBattleEvent);
        EventManager.RemoveEvent<SCReviveInfo>(ClientEvent.OnReviveProgressUpdate, OnReviveProgressUpdate);
        EventManager.RemoveEvent<string>(ClientEvent.OnRightClickBlocked, OnRightClickBlocked);
        EventManager.RemoveEvent<int>(ClientEvent.OnDayNightChange, OnDayNightChange);
    }

    public override void Tick(float deltaTime)
    {
        //飘字自动消失
        for (int i = floatingLabels.Count - 1; i >= 0; i--)
        {
            var item = floatingLabels[i];
            if (Time.time - item.time > FloatingLife)
            {
                if (item.label != null) Destroy(item.label.gameObject);
                floatingLabels.RemoveAt(i);
            }
        }
        //剩余时间（本地估算，精确值以服务器 SCScoreInfo 为准）
        if (NetworkManager.battleInfo != null && timeLabel != null)
        {
            float remain = Config.battle_duration - (Time.time - battleStartTime);
            timeLabel.text = FormatTime(Mathf.Max(0f, remain));
        }
    }

    #region 事件处理
    /// <summary>实体表现摘要：守护点 → 右侧面板；本地玩家 → 底部技能栏。</summary>
    private void OnEntityDisplayUpdate(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        if (info.type.category == EntityCategory.Beacon)
        {
            OnBeaconDisplay(info);
        }
        else if (NetworkManager.battleInfo != null && info.entityId == NetworkManager.battleInfo.playerEntityId)
        {
            OnLocalSkillBarUpdate(info);
        }
    }

    private void OnLocalSkillBarUpdate(SCEntityDisplayInfo info)
    {
        if (skillBar == null || skillTemplate == null) return;
        //重建槽位（数量变化时）；模板本体即 0 号槽（与 RosList 同一约定，ActiveFor 克隆 list[0] 补足）
        if (skillSlots.Count == 0)
        {
            skillTemplate.gameObject.SetActive(false);
            skillSlots.Add(skillTemplate);
        }
        while (skillSlots.Count < info.skills.Count)
        {
            var item = Instantiate(skillTemplate, skillBar);
            item.gameObject.SetActive(false);
            skillSlots.Add(item);
        }
        Tool.ActiveFor(skillSlots, info.skills.Count);
        for (int i = 0; i < info.skills.Count; i++)
        {
            skillSlots[i].Refresh(info.skills[i], i == info.selectedIndex, i);
        }
    }

    private void OnBeaconDisplay(SCEntityDisplayInfo info)
    {
        if (beaconPanel == null || beaconTemplate == null) return;
        if (!beaconBars.TryGetValue(info.entityId, out var unit))
        {
            unit = Instantiate(beaconTemplate, beaconPanel);
            beaconBars[info.entityId] = unit;
        }
        unit.gameObject.SetActive(true);
        if (info.health <= 0) unit.SetDestroyed();
        else unit.Refresh(info);
    }

    private void ClearBeacons()
    {
        foreach (var bar in beaconBars.Values)
        {
            if (bar != null) Destroy(bar.gameObject);
        }
        beaconBars.Clear();
    }

    private void OnScoreUpdate(SCScoreInfo info)
    {
        if (info == null) return;
        if (attackScoreLabel != null) attackScoreLabel.text = $"拆塔 {(int)info.attackScore}";
        if (defenseScoreLabel != null) defenseScoreLabel.text = $"防守 {(int)info.defenseScore}";
        if (timeLabel != null) timeLabel.text = FormatTime(Mathf.Max(0f, info.remainTime));
        if (info.gameState != 0)
        {
            ShowFloating(GetEndText(info.gameState), GetEndColor(info.gameState));
            ShowSettlement(info);
        }
    }

    /// <summary>显示结算面板（胜负/比分/经验；玩家关闭后回组队大厅准备下一轮）。</summary>
    private void ShowSettlement(SCScoreInfo info)
    {
        if (settlePanel == null) return;
        if (settleTitle != null)
        {
            settleTitle.text = GetEndText(info.gameState);
            settleTitle.color = GetEndColor(info.gameState);
        }
        if (settleDetail != null)
            settleDetail.text = $"进攻方（拆塔）：{(int)info.attackScore}\n" +
                                $"防守方：{(int)info.defenseScore}（击杀 ×{info.killScore}）\n" +
                                $"本局获得经验：{info.expGain}";
        settlePanel.SetActive(true);
    }

    private void HideSettlement()
    {
        if (settlePanel != null) settlePanel.SetActive(false); // 新对局隐藏结算面板
    }

    private void OnSettleClose()
    {
        HideSettlement();
        Tool.ClientLogicManager?.EntityPlayers.ClearAll();
        Tool.UIManager?.TurnPage(PageType.Lobby); // 组队状态保留，点"准备"开启下一轮
    }

    private void OnBattleEvent(SCBattleEvent e)
    {
        if (e == null) return;
        switch (e.type)
        {
            case SCBattleEvent.Type.Kill:
                ShowFloating("击杀！", new Color(1f, 0.45f, 0.4f));
                break;
            case SCBattleEvent.Type.BeaconDestroyed:
                ShowFloating("守护点被摧毁！", new Color(1f, 0.32f, 0.3f));
                break;
            case SCBattleEvent.Type.CrystalCollected:
                ShowFloating("采集水晶，获得收益", new Color(0.42f, 0.85f, 0.55f));
                break;
            case SCBattleEvent.Type.CrystalBroken:
                ShowFloating("该水晶已被感染，无产出", new Color(1f, 0.62f, 0.28f));
                break;
            case SCBattleEvent.Type.PlagueTreeCaptured:
                ShowFloating("攻占瘟疫树！获得瘟疫祝福", new Color(0.4f, 0.72f, 1f));
                break;
            case SCBattleEvent.Type.ShowText:
                //消息提示控件尚未在 uGUI 重建，暂以飘字代替
                ShowFloating(NoticeMessageMap.Get(e.value), Color.white);
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
        ShowFloating(string.IsNullOrEmpty(msg) ? "该技能无法在此状态下使用" : msg, new Color(1f, 0.62f, 0.28f));
    }

    private void OnDayNightChange(int state)
    {
        RefreshDayNightLabel();
    }
    #endregion

    #region//Local
    private void ShowFloating(string text, Color color)
    {
        if (floatingPanel == null || floatingTextTemplate == null) return;
        var label = Instantiate(floatingTextTemplate, floatingPanel);
        label.text = text;
        label.color = color;
        floatingLabels.Add((label, Time.time));
    }

    private void RefreshDayNightLabel()
    {
        if (phaseLabel == null) return;
        phaseLabel.text = $"{(EnvironmentManager.IsDay ? DayName : NightName)} {Mathf.RoundToInt(EnvironmentManager.Time01 * 100f)}%";
    }

    private static Color GetEndColor(int gameState)
    {
        switch (gameState)
        {
            case 1: return new Color(1f, 0.45f, 0.4f);  // 进攻方胜利：红
            case 2: return new Color(0.4f, 0.72f, 1f);  // 防守方胜利：蓝
            default: return Color.white;                 // 平局
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
