/// <summary>
/// 守护点（瘟疫信标）：外围 3 个 + 中心 1 个。
/// 受击要把伤害计入进攻方得分与采集量；阵亡要重算中心守护点的分层减伤。
/// 中心守护点被摧毁即判进攻方胜利，属对局级判定，仍在 BattleManager 的销毁流程里。
/// </summary>
public class BeaconEntityData : EntityData
{
    /// <summary>受击：进攻方得分 = 对守护点造成的总伤害；采集量 = 单个角色造成的伤害量（白眼标记与结算经验用）。</summary>
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

    /// <summary>阵亡：重算中心守护点分层减伤（每个存活外围点 25%）。</summary>
    public override void OnKilled()
    {
        base.OnKilled();
        Tool.BattleManager?.UpdateCoreBeaconReduce();
    }
}
