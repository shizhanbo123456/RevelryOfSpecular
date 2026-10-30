using UnityEngine;

public class CoralEntityData : EntityData
{
    public int spawnPointIndex = -1;

    public override void OnKilled()
    {
        SpreadPlaguePollute(); // 必须在实体销毁（OnDestroyed 会清 Buff）之前结算
        base.OnKilled();
        var battle = Tool.BattleManager;
        if (battle == null) return;

        battle.OnCoralDestroyed(this); // 回血 + 掉武器
        battle.NotifyCoralDestroyed(this); // 释放刷新点占用，可被邻近机制再次生成
    }

    // 瘟疫污染：被打碎时给附近进攻方单位各挂 1 层瘟疫标记，归属施加污染的瘟疫使者
    private void SpreadPlaguePollute()
    {
        if (Tool.BattleManager == null) return;
        if (effectController == null || !effectController.HasEffect(EffectType.PlaguePollute)) return;
        var source = BattleManager.GetEntity(effectController.GetSourceId(EffectType.PlaguePollute));
        if (source == null) return;
        Vector3 center = transform.position;
        float sqr = Config.plague_pollute_radius * Config.plague_pollute_radius;
        foreach (var entity in BattleManager.EntityContainer.Entities)
        {
            if (entity == null || !entity.Alive || entity.camp != EntityCamp.Attack) continue;
            if ((entity.transform.position - center).sqrMagnitude > sqr) continue;
            if (entity.effectController == null) continue;
            entity.effectController.AddEffect(EffectType.PlagueMark, 1, Config.plague_mark_duration, source);
        }
    }
}
