using System.Collections.Generic;
using FairyGUI;
using Ros.UI.Main;
using UnityEngine;

// 结算界面：独立的全局子界面，不挂在任何页面上（挂 GRoot）。
// 战斗结束走 OnSettlementResult 显示；GM「结算界面」演示直接调 PlayDemo。
public class SettlementView
{
    private const float AutoClose = 5f;            // 面板存活总时长
    private const float DetailStartDelay = 0.5f;   // 面板转场后首条细节出现
    private const float DetailItemInterval = 0.3f; // 相邻两条细节的间隔
    private const float DetailHoldDelay = 0.6f;    // 最后一条出现后的停留
    private const float DetailFadeDuration = 0.5f; // 全部细节同步淡出时长

    private static readonly Color CampAttackColor = new Color(1f, 0.45f, 0.4f);
    private static readonly Color CampDefenseColor = new Color(0.4f, 0.72f, 1f);

    private UI_BattleResult panel;
    private UI_BattleResultDetail detail;
    private readonly List<UI_BattleResultDetailItem> items = new();
    private readonly List<string> rows = new();
    private Timer.TransitionHandle transition;
    private bool restoreLobby; // 战斗结束流程：关闭时恢复组队面板

    public void Construct()
    {
        EventManager.AddEvent<SettlementResult>(ClientEvent.OnSettlementResult, OnSettlementResult);
    }

    public void Shutdown()
    {
        EventManager.RemoveEvent<SettlementResult>(ClientEvent.OnSettlementResult, OnSettlementResult);
        CancelAnimations();
    }

    // GM 演示：本地填演示数据 + 播放结算动画；不切页、不动战斗实体
    public void PlayDemo()
    {
        var demo = new SettlementResult
        {
            gameState = 1, // 进攻方胜
            attackScore = 1280f,
            defenseScore = 960f,
            killScore = 8,
            remainTime = 120f,
            expGain = 800,
            beaconHealth = 40f,
            playerLevelBefore = 5,
            playerLevelAfter = 6,
            characterLevelBefore = 3,
            characterLevelAfter = 4,
        };
        Show(demo, EntityCamp.Attack, false);
    }

    #region//Local
    private void OnSettlementResult(SettlementResult r)
    {
        // 先清战斗表现，防过时内容残留
        if (Tool.ClientLogicManager != null) Tool.ClientLogicManager.EntityPlayers.ClearAll();

        var battle = NetworkManager.battleInfo;
        Show(r, battle != null ? battle.camp : EntityCamp.None, true);

        // 立即切回组队界面（战斗 HUD 随 Exit 隐藏），再隐藏组队面板等结算播完
        if (Tool.UIManager != null)
        {
            Tool.UIManager.TurnPage(PageType.Lobby);
            Tool.UIManager.SetLobbyPanelVisible(false);
        }
    }

    private void Show(SettlementResult r, EntityCamp camp, bool restoreLobbyAfterClose)
    {
        restoreLobby = restoreLobbyAfterClose;
        if (panel == null)
        {
            panel = UI_BattleResult.CreateInstance();
            GRoot.inst.AddChild(panel); // 挂全局层：结算要跨页面盖在组队界面之上
            panel.visible = false;
        }
        panel.m_title.text = GetEndText(r.gameState, camp);
        panel.m_title.color = GetEndColor(r.gameState);
        panel.m_content.text = $"战斗用时：{Mathf.RoundToInt(Mathf.Max(0f, Config.battle_duration - r.remainTime))} 秒";

        if (detail == null)
        {
            detail = UI_BattleResultDetail.CreateInstance();
            panel.AddChild(detail);
        }
        BuildRows(r, camp);

        panel.visible = true;
        panel.m_t0.Play();
        ScheduleAnimations(); // 含播完自动关闭
    }

    private void BuildRows(SettlementResult r, EntityCamp camp)
    {
        rows.Clear();
        items.Clear();
        rows.Add($"战斗分数：进攻方 {(int)r.attackScore} ／ 防守方 {(int)r.defenseScore}");
        rows.Add($"击杀数：{r.killScore}");
        rows.Add(camp == EntityCamp.Attack
            ? $"对防守点伤害：{(int)r.expGain}"
            : $"防守点剩余血量：{(int)r.beaconHealth}");
        rows.Add($"获得的角色经验：+{r.expGain}");
        rows.Add($"获得的玩家经验：+{r.expGain}");
        if (r.characterLevelAfter > r.characterLevelBefore)
            rows.Add($"角色升级：{r.characterLevelBefore} → {r.characterLevelAfter}");
        if (r.playerLevelAfter > r.playerLevelBefore)
            rows.Add($"玩家升级：{r.playerLevelBefore} → {r.playerLevelAfter}");

        var list = detail.m_resultList;
        list.itemRenderer = RenderRow;
        list.numItems = rows.Count; // 非虚拟列表同步建条目并回调渲染器
    }

    private void RenderRow(int index, GObject obj)
    {
        if (obj is not UI_BattleResultDetailItem item) return;
        item.alpha = 0f; // 播放自身动画前保持透明
        item.m_content.text = index < rows.Count ? rows[index] : string.Empty;
        if (!items.Contains(item)) items.Add(item); // 动画序列按此顺序逐条播放
    }

    // 总时长固定为自动关闭时间：明细播完后留白，transition 走完即关闭结算
    private void ScheduleAnimations()
    {
        CancelAnimations();
        int shown = 0;
        float fadeStart = DetailStartDelay + items.Count * DetailItemInterval + DetailHoldDelay;
        transition = Timer.AddTransition(0, AutoClose, (_, t01) =>
        {
            if (t01 >= 1f)
            {
                transition = null; // 自身已结束，避免在回调内 Cancel 自身
                Close();
                return;
            }
            if (items.Count == 0) return;
            float elapsed = t01 * AutoClose;
            // 到达出现时刻的条目逐条播放自身动画（shown 指针保证每条只播一次）
            while (shown < items.Count && elapsed >= DetailStartDelay + shown * DetailItemInterval)
            {
                var item = items[shown++];
                if (item != null && item.displayObject != null) item.m_t0.Play();
            }
            // 全部出现后同步淡出直至消失
            if (elapsed >= fadeStart)
            {
                float alpha = 1f - Mathf.Clamp01((elapsed - fadeStart) / DetailFadeDuration);
                foreach (var item in items)
                {
                    if (item != null && item.displayObject != null) item.alpha = alpha;
                }
            }
        });
    }

    private void CancelAnimations()
    {
        if (transition != null) transition.Cancel();
        transition = null;
    }

    // 播完：隐藏结算；战斗结束流程再恢复组队面板
    private void Close()
    {
        CancelAnimations();
        if (panel != null) panel.visible = false;
        if (restoreLobby && Tool.UIManager != null) Tool.UIManager.SetLobbyPanelVisible(true);
        restoreLobby = false;
    }

    private static Color GetEndColor(int gameState)
    {
        switch (gameState)
        {
            case 1: return CampAttackColor;
            case 2: return CampDefenseColor;
            default:
                Debug.LogError($"[SettlementView] 未处理的结算状态：{gameState}");
                return Color.white;
        }
    }

    private static string GetEndText(int gameState, EntityCamp camp)
    {
        bool win = (gameState == 1 && camp == EntityCamp.Attack)
                || (gameState == 2 && camp == EntityCamp.Defense);
        return win ? "胜利" : "结束";
    }
    #endregion
}
