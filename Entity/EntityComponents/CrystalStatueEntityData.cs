public class CrystalStatueEntityData : AutoCastEntityData
{
    public override float AutoCastInterval => 5f;

    protected override float CastSearchRange => Config.crystal_statue_attack_range;
}
