using System.Collections.Generic;
using FairyGUI;
using Ros.UI.Main;
using UnityEngine;

// 结算界面：独立的全局子界面，不挂在任何页面上（挂 GRoot）。
// 战斗结束走 OnSettlementResult 显示；GM「结算界面」演示直接调 PlayDemo。
public class SettlementView
{
    private const float DetailStartDelay = 0.5f;   // 面板转场后首条细节出现
    private const float DetailItemInterval = 0.3f; // 相邻两条细节的间隔
    private const float DetailHoldDelay = 0.6f;    // 最后一条出现后的停留
    private const float DetailFadeDuration = 0.5f; // 面板与明细同步淡出时长
    private const float PanelCenterRatio = 0.35f;  // 面板中心在屏幕高度上的位置
    private const float DetailTopRatio = 0.55f;    // 明细列表顶部在屏幕高度上的位置

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
            guardStatueHealth = 40f,
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

        var loading = ClientLoadingManager.Current;
        Show(r, loading != null && loading.HasSession ? loading.Camp : EntityCamp.None, true);

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
        panel.m_content.text = $"战斗用时：{Mathf.RoundToInt(Mathf.Max(0f, ConfigManager.GlobalVariableConfig.battle_duration - r.remainTime))} 秒";

        if (detail == null)
        {
            detail = UI_BattleResultDetail.CreateInstance();
            panel.AddChild(detail);
        }
        BuildRows(r, camp);
        Layout();

        panel.visible = true;
        panel.alpha = 1f; // 每次显示从完全不透明开始
        panel.m_t0.Play();
        ScheduleAnimations(); // 含淡出与销毁
    }

    // 锚点均为左上角：按根节点尺寸摆放，面板居中偏上，明细列表居中偏下
    private void Layout()
    {
        float sw = GRoot.inst.width;
        float sh = GRoot.inst.height;

        panel.SetXY((sw - panel.width) * 0.5f, sh * PanelCenterRatio - panel.height * 0.5f);
        // 明细是面板子对象，换回局部坐标后让列表首行落在 DetailTopRatio 高度
        detail.SetXY((panel.width - detail.width) * 0.5f,
                     sh * DetailTopRatio - panel.y - detail.m_resultList.y);
    }

    private void BuildRows(SettlementResult r, EntityCamp camp)
    {
        rows.Clear();
        items.Clear();
        rows.Add($"战斗分数：进攻方 {(int)r.attackScore} ／ 防守方 {(int)r.defenseScore}");
        rows.Add($"击杀数：{r.killScore}");
        rows.Add(camp == EntityCamp.Attack
            ? $"对防守点伤害：{(int)r.expGain}"
            : $"防守点剩余血量：{(int)r.guardStatueHealth}");
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
        item.alpha = 0f; // 出现前整体隐藏，到点播放 t0 时解开
        item.m_content.text = index < rows.Count ? rows[index] : string.Empty;
        if (!items.Contains(item)) items.Add(item); // 动画序列按此顺序逐条播放
    }

    // 总时长 = 淡出开始时刻 + 淡出时长，走完即销毁
    private void ScheduleAnimations()
    {
        CancelAnimations();
        int shown = 0;
        float fadeStart = DetailStartDelay + items.Count * DetailItemInterval + DetailHoldDelay;
        float total = fadeStart + DetailFadeDuration;
        transition = Timer.AddTransition(0, total, (_, t01) =>
        {
            if (t01 >= 1f)
            {
                transition = null; // 自身已结束，避免在回调内 Cancel 自身
                Close();
                return;
            }
            if (items.Count == 0) return;
            float elapsed = t01 * total;
            // 到达出现时刻的条目逐条播放自身动画（shown 指针保证每条只播一次）
            while (shown < items.Count && elapsed >= DetailStartDelay + shown * DetailItemInterval)
            {
                var item = items[shown++];
                if (item == null || item.displayObject == null) continue;
                item.m_t0.Play();   // 淡入由 t0 的 Alpha 通道完成
                item.alpha = 1f;    // 显示期间整体保持不透明
            }
            // 面板与明细共用同一条 alpha 曲线：只压面板，明细随父级一起淡出
            if (elapsed >= fadeStart && panel != null)
                panel.alpha = 1f - Mathf.Clamp01((elapsed - fadeStart) / DetailFadeDuration);
        });
    }

    private void CancelAnimations()
    {
        if (transition != null) transition.Cancel();
        transition = null;
    }

    // 淡出结束：销毁面板与明细；战斗结束流程再恢复组队面板
    private void Close()
    {
        CancelAnimations();
        if (panel != null)
        {
            panel.Dispose(); // 明细挂在面板下，随面板一起销毁
            panel = null;
        }
        detail = null;
        items.Clear();
        rows.Clear();
        if (restoreLobby && Tool.UIManager != null) Tool.UIManager.SetLobbyPanelVisible(true);
        restoreLobby = false;
    }

    private static string GetEndText(int gameState, EntityCamp camp)
    {
        bool win = (gameState == 1 && camp == EntityCamp.Attack)
                || (gameState == 2 && camp == EntityCamp.Defense);
        return win ? "胜利" : "结束";
    }
    #endregion
}
