// 玩家阵营分类（组队大厅三选一）：与实体阵营 EntityCamp 分离——观战只是玩家归属，不存在观战实体
// 数值即协议值（LobbyMemberInfo.camp / ChatItem.m_camp 档位 / CSLobbyUpdate.camp）
public enum PlayerCampType
{
    Attack = 0,    // 进攻方
    Defense = 1,   // 防守方
    Spectator = 2, // 观战方（进入组队大厅的默认归属）
}
