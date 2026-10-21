using Ros.Transport;

public class CrystalEntityData : EntityData
{
    public int spawnPointIndex = -1;

    public override void OnKilled()
    {
        base.OnKilled();
        var battle = Tool.BattleManager;
        if (battle == null) return;

        battle.TryDropCrystalWeapon(this);
        battle.NotifyCrystalDestroyed(this); // 释放刷新点占用，可被邻近机制再次生成（策划案第七章已移除固定重生计时）
    }
}
