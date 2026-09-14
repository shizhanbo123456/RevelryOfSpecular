using Ros.Transport;

/// <summary>
/// 对局结算子管理器（客户端逻辑）：终局判定与局外经验入账。
/// 局外经验只在这一处写入存档（UI 只负责显示），避免多处结算重复加经验。
/// </summary>
public class SettlementManager : ClientSubManager
{
    private bool settled;

    protected override void BindEvents()
    {
        EventManager.AddEvent<SCScoreInfo>(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.AddEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
    }

    protected override void UnbindEvents()
    {
        EventManager.RemoveEvent<SCScoreInfo>(ClientEvent.OnScoreUpdate, OnScoreUpdate);
        EventManager.RemoveEvent<int>(ClientEvent.OnBattleStart, OnBattleStart);
    }

    private void OnBattleStart(int camp) => settled = false;

    private void OnScoreUpdate(SCScoreInfo info)
    {
        if (info == null || info.gameState == 0 || settled) return;
        settled = true;
        SettleExp(info);
        EventManager.TrigEvent(ClientEvent.OnBattleEnd, info.gameState);
    }

    #region//Local
    /// <summary>局外经验入账（策划案 17.3：获得经验 = 对水晶造成的伤害量，服务器随 SCScoreInfo 下发）。</summary>
    private static void SettleExp(SCScoreInfo info)
    {
        if (Tool.SaveManager == null || info.expGain <= 0) return;
        Tool.SaveManager.AddPlayerExp(info.expGain);
        var battle = NetworkManager.battleInfo;
        if (battle == null) return;
        // 全局角色索引：进攻 0~17 / 防守 18~23（SaveManager 双等级制）
        int index = battle.camp == EntityCamp.Attack
            ? battle.characterType.value
            : Config.attack_character_count + battle.characterType.value;
        Tool.SaveManager.AddCharacterExp(index, info.expGain);
    }
    #endregion
}
