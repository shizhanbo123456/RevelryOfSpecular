// 玩家阵营分类（四选一：进攻/防守/观战/中立）：与实体阵营 EntityCamp 分离——观战只是玩家归属，不存在观战实体
// 数值即协议值（LobbyMemberInfo.camp / ChatItem.m_camp 档位 / CSLobbyUpdate.camp / PlayerCampIcon.m_player_camp 档位）
public enum PlayerCampType
{
    Attack = 0,    // 进攻方
    Defense = 1,   // 防守方
    Spectator = 2, // 观战方（进入组队大厅的默认归属）
    Neutral = 3,   // 中立方
}
