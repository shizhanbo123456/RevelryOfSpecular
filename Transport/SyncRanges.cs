// 位级同步使用的量化范围/精度：优先取配表 global_variables，配表无对应项的在下方写死并注明
public static class SyncRanges
{
    public static float PositionXZMax => ConfigManager.GlobalVariableConfig.terrain_size;      // 地图边长，坐标 0~边长
    public static float PositionXZStep => ConfigManager.GlobalVariableConfig.sync_precision_entity_xz;
    public static float PositionYMax => ConfigManager.GlobalVariableConfig.terrain_height;     // 地形高度上限
    public static float PositionYStep => ConfigManager.GlobalVariableConfig.sync_precision_entity_y;
    public static float YawMax => 360f;                                                        // 欧拉角 Y 的取值范围，配表无对应项
    public static float YawStep => ConfigManager.GlobalVariableConfig.sync_precision_entity_yaw;

    // 速度按正负对称理解：字段值 ∈ [-Range, Range]
    public static float VelocityXZRange => ConfigManager.GlobalVariableConfig.sync_entity_velocity_xz;
    public static float VelocityXZStep => ConfigManager.GlobalVariableConfig.sync_precision_entity_velocity_xz;
    public static float VelocityYRange => ConfigManager.GlobalVariableConfig.sync_entity_velocity_y;
    public static float VelocityYStep => ConfigManager.GlobalVariableConfig.sync_precision_entity_velocity_y;
    public static float YawSpeedRange => ConfigManager.GlobalVariableConfig.move_turn_rate;     // 角速度不会超过转向角速度

    public static float MinimapPositionStep => ConfigManager.GlobalVariableConfig.sync_precision_minimap_xz;
    public static float MoveSpeedScaleMax => 32f;   // GM 极速 10 × 死灵漫步 2.2 × 加速 1.2 的余量，配表无对应项

    public static int MaxHealth => ConfigManager.GlobalVariableConfig.max_entity_health;
    public static int MaxBuffCount => ConfigManager.GlobalVariableConfig.sync_max_buff_count;
    public static int EntityIdMax => Config.entity_id_max;

    // 单次飘字数值上限：武器经验加伤不设上限，配表无对应项，留足余量
    public const int CombatLabelAmountMax = 1000000;
}
