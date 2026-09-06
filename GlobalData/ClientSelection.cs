/// <summary>
/// 客户端全局选择状态（初始界面/组队大厅读写，进场时 NetworkManager 读取）。
/// 进攻方与防守方角色各自选择；队伍在对局房间（组队大厅）内选择。
/// </summary>
public static class ClientSelection
{
    /// <summary>进攻方所选角色下标（0 ~ attack_character_count-1）。</summary>
    public static int selectedAttackIndex = 0;

    /// <summary>防守方所选角色下标（0 ~ defense_character_count-1）。</summary>
    public static int selectedDefenseIndex = 0;
}
