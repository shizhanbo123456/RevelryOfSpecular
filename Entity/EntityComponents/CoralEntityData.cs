using Ros.Transport;

public class CoralEntityData : EntityData
{
    public int spawnPointIndex = -1;

    public override void OnKilled()
    {
        base.OnKilled();
        var battle = Tool.BattleManager;
        if (battle == null) return;

        battle.TryDropCoralWeapon(this);
        battle.NotifyCoralDestroyed(this); // 释放刷新点占用，可被邻近机制再次生成
    }
}
