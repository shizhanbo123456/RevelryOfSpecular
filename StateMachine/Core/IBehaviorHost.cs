// 行为宿主：三个通用行为只认这个接口，不认具体实体类型
public interface IBehaviorHost
{
    EntityData Owner { get; }

    // 停步：僵尸要连带停动画，玩家只清寻路目的地，雕像空实现
    void Stop();

    // 朝向目标：僵尸按目标 id 锁朝向，玩家写朝向点，雕像空实现
    void FaceTarget(EntityData target);

    // 出手一次，返回是否真的放出了技能
    bool TryAttack(EntityData target);

    // 动作未播完（攻击行为据此压住切换）
    bool IsActing { get; }

    // 离开攻击行为时清掉朝向意图
    void ClearFace();

    // 目的地算不出路，交给决策层换目标
    void NotifyStuck();
}
