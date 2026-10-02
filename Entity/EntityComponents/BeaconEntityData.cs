public class BeaconEntityData : EntityData
{
    protected override void OnDamageApplied(float finalDamage, EntityData attacker)
    {
        var battle = Tool.BattleManager;
        if (battle == null) return;

        battle.AddBeaconDamage(finalDamage);
        if (attacker == null || finalDamage <= 0f) return;
        if (battle.EntityOwnerClient.TryGetValue(attacker.id, out var harvester))
        {
            battle.AddHarvest(harvester, finalDamage);
        }
    }

    public override void OnKilled()
    {
        base.OnKilled();
        if (Tool.BattleManager != null) Tool.BattleManager.UpdateCoreBeaconReduce();
    }
}
