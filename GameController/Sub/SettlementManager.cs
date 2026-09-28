using Ros.Transport;

/// <summary>结算明细（客户端显示用）：原始分数 + 奖励明细（含升级前后等级）。</summary>
public struct SettlementResult
{
    public int gameState;
    public float attackScore;
    public float defenseScore;
    public int killScore;
    public float remainTime;
    public int expGain;
    public int playerLevelBefore;
    public int playerLevelAfter;
    public int characterLevelBefore;
    public int characterLevelAfter;
}

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
        var result = BuildSettlementResult(info);
        EventManager.TrigEvent(ClientEvent.OnBattleEnd, info.gameState);
        EventManager.TrigEvent(ClientEvent.OnSettlementResult, result);
    }

    #region//Local
    /// <summary>组装结算明细：先采集升级前等级，入账后再采集升级后等级，避免经验写入后丢失前后对照。</summary>
    private SettlementResult BuildSettlementResult(SCScoreInfo info)
    {
        var sm = Tool.SaveManager;
        int charIndex = GetLocalCharacterIndex();
        int playerLvBefore = sm != null ? sm.playerLevel : 1;
        int charLvBefore = charIndex >= 0 && sm != null ? sm.GetCharacterLevel(charIndex) : 1;
        SettleExp(info);
        int playerLvAfter = sm != null ? sm.playerLevel : 1;
        int charLvAfter = charIndex >= 0 && sm != null ? sm.GetCharacterLevel(charIndex) : 1;
        return new SettlementResult
        {
            gameState = info.gameState,
            attackScore = info.attackScore,
            defenseScore = info.defenseScore,
            killScore = info.killScore,
            remainTime = info.remainTime,
            expGain = info.expGain,
            playerLevelBefore = playerLvBefore,
            playerLevelAfter = playerLvAfter,
            characterLevelBefore = charLvBefore,
            characterLevelAfter = charLvAfter,
        };
    }

    /// <summary>本地玩家所用角色的全局索引（进攻 0~17 / 防守 18~23，对应 SaveManager 双等级制）。</summary>
    private static int GetLocalCharacterIndex()
    {
        var battle = NetworkManager.battleInfo;
        if (battle == null) return -1;
        return battle.camp == EntityCamp.Attack
            ? battle.characterType.value
            : Config.attack_character_count + battle.characterType.value;
    }

    /// <summary>局外经验入账（策划案 17.3：获得经验 = 对守护点造成的伤害量，服务器随 SCScoreInfo 下发）。</summary>
    private static void SettleExp(SCScoreInfo info)
    {
        if (Tool.SaveManager == null || info.expGain <= 0) return;
        Tool.SaveManager.AddPlayerExp(info.expGain);
        int index = GetLocalCharacterIndex();
        if (index < 0) return;
        Tool.SaveManager.AddCharacterExp(index, info.expGain);
    }
    #endregion
}
