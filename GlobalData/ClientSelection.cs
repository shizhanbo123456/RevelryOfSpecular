/// <summary>
/// 客户端全局选择状态（初始界面/匹配大厅读写，进场时 NetworkManager 读取）。
/// 旧 UI 的 HomePage 静态选择字段迁移至此。
/// </summary>
public static class ClientSelection
{
    /// <summary>当前选中角色索引（进攻 0~17 / 防守 18~23，映射规则见 SelectionToEntityType）。</summary>
    public static int selectedCharacterIndex = 0;

    /// <summary>阵营意向：0 进攻 / 1 防守 / 2 任意。</summary>
    public static int campIntention = 2;

    /// <summary>队列选择：true PVP（真人防守方）/ false PVE（AI 防守方）。</summary>
    public static bool pvpQueue = true;

    /// <summary>将 UI 选择索引转换为 EntityType（防守角色索引 = 18 + 防守下标）。</summary>
    public static EntityType SelectionToEntityType(int index)
    {
        if (index >= Config.attack_character_count)
        {
            return EntityType.Defense(index - Config.attack_character_count);
        }
        return EntityType.Attack(index);
    }

    /// <summary>将 EntityType 转回 UI 选择索引。</summary>
    public static int EntityTypeToSelection(EntityType type)
    {
        if (type.category == EntityCategory.Character_Defense) return Config.attack_character_count + type.value;
        return type.value;
    }
}
