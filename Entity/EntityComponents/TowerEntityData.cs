public class TowerEntityData : AutoCastEntityData
{
    public override float AutoCastInterval => 5f;

    protected override float CastSearchRange => Config.tower_attack_range;
}
