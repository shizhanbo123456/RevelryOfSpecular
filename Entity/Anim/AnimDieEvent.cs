// 死亡状态的行为挂载点：输入屏蔽与状态登记录入由基类完成；死亡动画播完的回调已随「死亡即销毁」取消
public class AnimDieEvent : AnimEvent
{
    protected override EntityAnim.AnimState State => EntityAnim.AnimState.Die;
    protected override InputBlockOp blockOp => InputBlockOp.Forward | InputBlockOp.Rotation | InputBlockOp.Jump | InputBlockOp.Slide | InputBlockOp.Attack;
}
