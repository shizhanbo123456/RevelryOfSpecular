using UnityEngine;

/// <summary>客户端逻辑管理器（客户端总控）。</summary>
public class ClientLogicManager : MonoBehaviour
{
    private void Awake()
    {
        Tool.ClientLogicManager = this;
    }

    /// <summary>本地玩家实体 id。</summary>
    public ushort LocalPlayerEntityId => NetworkManager.battleInfo != null ? NetworkManager.battleInfo.playerEntityId : (ushort)0;

    // TODO: 子管理器框架（EntityPlayerManager / LabelPlayerManager / SettlementManager /
    //        ClientSkillManager / ClientBattleTimeManager 等）后续按需拆分完善
}
