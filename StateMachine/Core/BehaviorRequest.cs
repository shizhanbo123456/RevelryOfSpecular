using UnityEngine;

// 期望行为种类：None = 决策层本拍没意见，保持字段里上一次的请求
public enum BehaviorKind
{
    None,
    Idle,
    Move,
    Attack,
}

// 决策层唯一能写的东西：期望行为 + 目标，不含任何执行动作
public struct BehaviorRequest
{
    public BehaviorKind Kind;
    public Vector3 Point;     // Move 的静止坐标目的地；Target 非空时忽略
    public EntityData Target; // Move 跟随的目标物体；Attack 的攻击对象；Idle 的站桩朝向
}
