// 服务器侧意图转发器：只把客户端意图翻译成行为层请求，不含任何决策；附 BattleManager 的意图入口
using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

public sealed class AiIntentRelay : DecisionMachine, IBehaviorHost
{
    public readonly PlayerEntityData Owner;

    private readonly BehaviorArbiter arbiter;
    private readonly int clientId;

    private AiIntent intent = AiIntent.Stand;
    private Vector2 point;
    private ushort targetId;
    private bool moveBlocked; // 目的地走不到：意图不变就不再重发，避免每帧重算路径并刷回执
    private readonly List<int> castOrder = new();

    public AiIntentRelay(PlayerEntityData owner, int ownerClientId)
    {
        Owner = owner;
        clientId = ownerClientId;
        ThinkInterval = 0f; // 转发层不节流：命令到达当帧就要落到行为层
        arbiter = new BehaviorArbiter(this);
        Initialize(new RelayState(this), 0f);
    }

    public void SetIntent(AiIntent kind, Vector2 position, ushort target)
    {
        if (kind != intent || position != point || target != targetId) moveBlocked = false;
        intent = kind;
        point = position;
        targetId = target;
        ApplyIntent();
    }

    public override void Tick()
    {
        if (!Owner.Alive) return;
        base.Tick();
        arbiter.Tick(Time.deltaTime);
    }

    protected override EntityData MachineOwner => Owner;

    private void ApplyIntent()
    {
        switch (intent)
        {
            case AiIntent.Stand:
            case AiIntent.Stop:
                arbiter.RequestIdle();
                break;
            case AiIntent.MoveTo:
                if (moveBlocked)
                {
                    arbiter.RequestIdle();
                    break;
                }
                arbiter.RequestMove(new Vector3(point.x, Owner.transform.position.y, point.y));
                break;
            case AiIntent.AttackTarget:
                if (BattleManager.EntityContainer.Entities.TryGetObject(targetId, out var target) && target != null && target.Alive)
                {
                    arbiter.RequestAttack(target);
                }
                else
                {
                    arbiter.RequestIdle(); // 目标已消失：站住等客户端下一条意图，不自行换目标
                }
                break;
        }
    }

    // 目的地算不出路：回执给客户端，由客户端的 Lua 状态机换目标
    private void NotifyMoveFailed()
    {
        if (Tool.NetworkManager == null) return;
        Tool.NetworkManager.SendAiFeedback(clientId, new SCAiFeedback
        {
            kind = 0,
            posX = Owner.transform.position.x,
            posZ = Owner.transform.position.z,
            targetId = 0,
        });
    }

    // 出手顺序：持有表按品质降序、同品质 id 降序；全不可用退空手三连（远直拳近崩拳）
    private bool TryCastAny(EntityData target)
    {
        var held = Owner.skillController.GetSkillIds();
        castOrder.Clear();
        castOrder.AddRange(held);
        castOrder.Sort(ComparePriority);
        for (int i = 0; i < castOrder.Count; i++)
        {
            if (TryUseSkill(castOrder[i])) return true;
        }

        bool close = target != null && FlatDistance(target.transform.position) <= Config.unarmed_smash_range;
        int punch = Random.Range(0, 2) == 0 ? Config.unarmed_punch_left : Config.unarmed_punch_right;
        int other = punch == Config.unarmed_punch_left ? Config.unarmed_punch_right : Config.unarmed_punch_left;
        if (close && TryUseSkill(Config.unarmed_attack_smash)) return true;
        if (TryUseSkill(punch)) return true;
        return TryUseSkill(other);
    }

    private int ComparePriority(int a, int b)
    {
        var ca = ConfigManager.SkillConfig.Get(a);
        var cb = ConfigManager.SkillConfig.Get(b);
        if (ca.qualityRank != cb.qualityRank) return cb.qualityRank.CompareTo(ca.qualityRank);
        return b.CompareTo(a);
    }

    private float FlatDistance(Vector3 point)
    {
        Vector3 d = point - Owner.transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    private sealed class RelayState : DecisionState
    {
        private readonly AiIntentRelay relay;

        public RelayState(AiIntentRelay relay) { this.relay = relay; }

        public override DecisionState Decide()
        {
            relay.ApplyIntent(); // 意图常驻：每拍重申，行为层的打断闸自己决定何时执行
            return null;
        }
    }

    #region//IBehaviorHost
    EntityData IBehaviorHost.Owner => Owner;

    void IBehaviorHost.Stop() => Owner.StopMoving();

    void IBehaviorHost.FaceTarget(EntityData target) => Owner.SetRobotFacePoint(target.transform.position);

    bool IBehaviorHost.TryAttack(EntityData target)
    {
        if (Owner.InputBlocked(InputBlockOp.Attack)) return false;
        return TryCastAny(target);
    }

    bool IBehaviorHost.IsActing => Owner.anim != null && Owner.anim.CurrentState == EntityAnim.AnimState.Attack;

    void IBehaviorHost.ClearFace() => Owner.ClearRobotFacePoint();

    void IBehaviorHost.NotifyStuck()
    {
        moveBlocked = true;
        NotifyMoveFailed();
    }
    #endregion
}

public partial class BattleManager
{
    // AI 操控意图入口：客户端 → 服务器。首次意图时建转发器并接管该实体
    public void ReceiveAiIntent(int clientId, CSAiIntent intent)
    {
        if (intent == null) return;
        if (!PlayerEntityId.TryGetValue(clientId, out var entityId)) return;
        if (!EntityContainer.Entities.TryGetObject(entityId, out var entity) || entity == null) return;
        if (entity is not PlayerEntityData player) return;

        // 接管状态记在会话上：复活重建实体时按它恢复，不会掉回「真人」
        AiClients.Add(clientId);
        player.aiControlled = true;
        if (player.AiRelay == null) player.AiRelay = new AiIntentRelay(player, clientId);
        player.AiRelay.SetIntent((AiIntent)intent.intent, new Vector2(intent.posX, intent.posZ), (ushort)intent.targetId);
    }

    // AI 专用技能释放：不走键盘那条链路（接管期间键盘包会被丢弃），校验口径与键盘一致
    public void ReceiveAiSkillCast(int clientId, int skillId)
    {
        if (skillId < 0) return;
        if (!PlayerEntityId.TryGetValue(clientId, out var entityId)) return;
        if (!EntityContainer.Entities.TryGetObject(entityId, out var entity) || entity == null) return;
        if (entity.InputBlocked(InputBlockOp.Attack)) return; // 攻击动画等期间的输入阻断

        // 放技能也算接管：之后键盘照常丢弃
        AiClients.Add(clientId);
        if (entity is PlayerEntityData player) player.aiControlled = true;

        var result = entity.skillController.TryUseSkill(skillId);
        if (result != EntitySkillController.UseResult.Success) NotifySkillUseFailed(entity, result);
    }
}
