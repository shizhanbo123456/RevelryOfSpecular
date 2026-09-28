using Ros.Transport;

/// <summary>
/// 可采集水晶：被摧毁时按「蘑菇感染 + 进攻方摧毁」判定产出。
/// 生成由 BattleManager 的"玩家邻近"机制驱动（不再有固定重生计时）：
/// 本类只负责摧毁时的产出判定，并通知 BattleManager 释放该刷新点占用（见 BattleManager.NotifyCrystalDestroyed）。
/// 蘑菇感染是水晶上的 Buff（服务器不存在蘑菇实体），故判定写在这里。
/// </summary>
public class CrystalEntityData : EntityData
{
    /// <summary>本水晶所在的刷新点下标（生成时由 BattleManager 写入；-1 = 非刷新点生成）。</summary>
    public int spawnPointIndex = -1;

    public override void OnKilled()
    {
        base.OnKilled();
        var battle = Tool.BattleManager;
        if (battle == null) return;

        // 被「蘑菇感染」的水晶被进攻方摧毁 → 无产出（不掉武器，广播 CrystalBroken）；
        // 防守方摧毁或未感染 → 正常产出（策划案 11.3 蘑菇感染）
        bool infectedAndBrokenByAttack = effectController != null &&
                                         effectController.HasEffect(EffectType.MushroomInfect) &&
                                         lastAttacker != null &&
                                         lastAttacker.camp == EntityCamp.Attack;
        if (infectedAndBrokenByAttack)
        {
            Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.CrystalBroken);
        }
        else
        {
            Tool.NetworkManager.SendBattleEvent(SCBattleEvent.Type.CrystalCollected);
            battle.TryDropCrystalWeapon(this);
        }

        battle.NotifyCrystalDestroyed(this); // 释放刷新点占用，可被邻近机制再次生成（策划案第七章已移除固定重生计时）
    }
}
