using Ros.Transport;

/// <summary>
/// 可采集水晶：被摧毁时按「蘑菇感染 + 进攻方摧毁」判定产出，并排定 30~60s 随机重生。
/// 蘑菇感染是水晶上的 Buff（服务器不存在蘑菇实体），故判定写在这里。
/// </summary>
public class CrystalEntityData : EntityData
{
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

        battle.ScheduleCrystalRespawn(this); // 被摧毁即照常排重生，与是否感染无关（策划案第七章）
    }
}
