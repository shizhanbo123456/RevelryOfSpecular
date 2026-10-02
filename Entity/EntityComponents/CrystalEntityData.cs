using Ros.Transport;

public class CrystalEntityData : EntityData
{
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
