using UnityEngine;

public class ClientLogicManager : MonoBehaviour
{
    public EntityPlayerManager EntityPlayers { get; private set; }

    public SettlementManager Settlement { get; private set; }

    public ClientSkillManager SkillVfx { get; private set; }

    public ClientBattleTimeManager BattleTime { get; private set; }

    public HomePreviewManager HomePreview { get; private set; }

    private ClientSubManager[] subManagers;

    private void Awake()
    {
        Tool.ClientLogicManager = this;

        EntityPlayers = new EntityPlayerManager();
        Settlement = new SettlementManager();
        SkillVfx = new ClientSkillManager();
        BattleTime = new ClientBattleTimeManager();
        HomePreview = new HomePreviewManager();

        // 先全部建好再统一 Init：Init 内可能有跨子管理器的引用
        subManagers = new ClientSubManager[] { EntityPlayers, Settlement, SkillVfx, BattleTime, HomePreview };
        foreach (var sub in subManagers) sub.Init(this);
    }

    private void Update()
    {
        if (subManagers == null) return;
        float deltaTime = Time.deltaTime;
        foreach (var sub in subManagers) sub.Tick(deltaTime);
    }

    private void OnDestroy()
    {
        if (subManagers == null) return;
        foreach (var sub in subManagers) sub.Dispose();
    }
}
