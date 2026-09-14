using UnityEngine;

/// <summary>
/// 客户端逻辑管理器（客户端总控）：服务器摘要到达客户端后统一由它分发给各子管理器。
/// 子管理器是普通 C# 类（GameController/Sub），在这里被创建、初始化、逐帧驱动与解绑。
/// </summary>
public class ClientLogicManager : MonoBehaviour
{
    /// <summary>实体表现（Sub/EntityPlayerManager）：视图创建与移除、位姿动画同步、武器与持续特效。</summary>
    public EntityPlayerManager EntityPlayers { get; private set; }

    /// <summary>头顶信息（Sub/LabelPlayerManager）：玩家名字与血条。</summary>
    public LabelPlayerManager Labels { get; private set; }

    /// <summary>对局结算（Sub/SettlementManager）：终局判定与局外经验入账。</summary>
    public SettlementManager Settlement { get; private set; }

    /// <summary>技能表现（Sub/ClientSkillManager）：按服务器下发的上下文重建轨迹播放。</summary>
    public ClientSkillManager SkillVfx { get; private set; }

    /// <summary>战斗时间（Sub/ClientBattleTimeManager）：昼夜快照与推演、分数快照。</summary>
    public ClientBattleTimeManager BattleTime { get; private set; }

    private ClientSubManager[] subManagers;

    private void Awake()
    {
        Tool.ClientLogicManager = this;

        EntityPlayers = new EntityPlayerManager();
        Labels = new LabelPlayerManager();
        Settlement = new SettlementManager();
        SkillVfx = new ClientSkillManager();
        BattleTime = new ClientBattleTimeManager();

        // 先全部建好再统一 Init：Init 内可能有跨子管理器的引用
        subManagers = new ClientSubManager[] { EntityPlayers, Labels, Settlement, SkillVfx, BattleTime };
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
