public class CrystalStatueEntityData : AutoCastEntityData
{
    public override float AutoCastInterval => ConfigManager.GlobalVariableConfig.status_auto_cast_interval_crystal;

    protected override float CastSearchRange => Config.crystal_statue_attack_range;
}
