public static class ClientEvent
{
    public static int OnConnect = 10201;
    public static int OnEnterWorld = 10202;
    public static int OnRestartGame = 10200;

    public static int ShowNotice = 10000;//param=string
    public static int ShowSideNotice = 10001;//param=string
    public static int ShowSceneLabel = 10002;//param=(Vector3,string,TextColor)
    public static int UpdateEntityDisplayInfo = 10003;//param=SCEntityDisplayInfo
    public static int RemoveEntityDisplayInfo = 10004;//param=int
    public static int UpdateInteractablePropInfo = 10005;//param=SCInteractablePropInfo

    public static int UpdateExitProcess = 10100;//param=float
    public static int ExitMax = 10101;
    public static int ExitNormal = 10102;
    public static int ExitMin = 10103;
    public static int ExitLost = 10104;

    public static int PickUnlockProp = 10300;
    public static int PvpKillReward = 10301;//param=PvpKillRewardInfo
    public static int UpdateBattleKillSummary = 10302;//param=BattleKillSummary
    public static int UpdateBattleHealth = 10303;//param=BattleHealthInfo
    public static int UpdateBattleStamina = 10304;//param=BattleStaminaInfo
    public static int UpdateBattleSkillRuntime = 10305;//param=BattleSkillRuntimeInfo
    public static int UpdateBattleTime = 10306;//param=int
    public static int UpdateSettlementReward = 10307;//param=SettleRewardInfo
    public static int OnPostEntityPlayerUpdate = 10310;//param=无，客户端所有角色位置更新完成后触发（EntityPlayerManager.OnUpdate 末尾）
    public static int OnPostCameraControllerUpdate = 10311;//param=无，相机位置更新完成后触发（CameraController 更新相机后）
}

public struct BattleKillSummary
{
    public int player;
    public int zombie;
    public int plant;
    public int infectedPlant;
    public int ore;
    public int infectedOre;
    public int infection;
}

public struct BattleHealthInfo
{
    public int health;
    public int maxHealth;
}

public struct BattleStaminaInfo
{
    public float stamina;
    public float maxStamina;
}

public struct BattleSkillRuntimeInfo
{
    public int slot;
    public int remainingCount;
    public float cooldownRate;
    public bool selected;
}
