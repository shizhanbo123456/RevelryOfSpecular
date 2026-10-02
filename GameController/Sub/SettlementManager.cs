using Ros.Transport;

public struct SettlementResult
{
    public int gameState;
    public float attackScore;
    public float defenseScore;
    public int killScore;
    public float remainTime;
    public int expGain;
    public float beaconHealth;
    public int playerLevelBefore;
    public int playerLevelAfter;
    public int characterLevelBefore;
    public int characterLevelAfter;
}

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
            beaconHealth = info.beaconHealth,
            playerLevelBefore = playerLvBefore,
            playerLevelAfter = playerLvAfter,
            characterLevelBefore = charLvBefore,
            characterLevelAfter = charLvAfter,
        };
    }

    private static int GetLocalCharacterIndex()
    {
        var battle = NetworkManager.battleInfo;
        if (battle == null) return -1;
        return battle.camp == EntityCamp.Attack
            ? battle.characterType.value
            : Config.attack_character_count + battle.characterType.value;
    }

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
