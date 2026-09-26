using System;
using Ros.Transport;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 结算面板（战斗 HUD 的全屏遮罩子面板）：对局结束时显示胜负/比分/经验，关闭后由战斗页回组队大厅。
/// 数据来源：服务器 SCScoreInfo（gameState：1=进攻方胜利 / 2=防守方胜利 / 其它=平局）。
/// </summary>
public class SettlementPanel : MonoBehaviour
{
    [SerializeField] private Text titleText;        // 胜负标题（进攻方胜利红 / 防守方胜利蓝 / 平局白）
    [SerializeField] private Text detailText;       // 双方比分 + 击杀数 + 本局获得经验
    [SerializeField] private RosButton closeButton; // "回到组队大厅"按钮（回调由战斗页注入）

    private Action onClose;

    /// <summary>注入关闭回调（战斗页：清实体表现残留并切回组队大厅）。</summary>
    public void SetCloseCallback(Action action)
    {
        onClose = action;
        closeButton.SetCallback(() => onClose?.Invoke());
    }

    /// <summary>显示结算（按服务器最终比分填充内容并激活面板）。</summary>
    public void Show(SCScoreInfo info)
    {
        if (info == null) return;
        if (titleText != null)
        {
            titleText.text = GetEndText(info.gameState);
            titleText.color = GetEndColor(info.gameState);
        }
        if (detailText != null)
            detailText.text = $"进攻方（拆塔）：{(int)info.attackScore}\n" +
                              $"防守方：{(int)info.defenseScore}（击杀 ×{info.killScore}）\n" +
                              $"本局获得经验：{info.expGain}";
        gameObject.SetActive(true);
    }

    /// <summary>隐藏面板（关闭按钮与新一局开始时都会调用）。</summary>
    public void Hide() => gameObject.SetActive(false);

    #region//Local
    private static string GetEndText(int gameState)
    {
        switch (gameState)
        {
            case 1: return "进攻方胜利";
            case 2: return "防守方胜利";
            default: return "平局";
        }
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
    #endregion
}
