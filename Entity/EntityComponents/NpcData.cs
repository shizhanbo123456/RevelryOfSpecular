using Ros.Transport;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NpcData : EntityData
{
    private static readonly HashSet<int> playerBuffer = new();

    private NavMeshAgent agent;
    private float nextMigrationTime;
    private ushort targetPlayerId;
    private bool hasTarget;
    private bool attackPreparing;
    private float attackHitTime;
    private float nextAttackTime;
    public static int count = 0;

    public override void OnCreate(ushort id, EntityType type, int level)
    {
        base.OnCreate(id, type, level);
        agent = GetComponent<NavMeshAgent>();
        ResetMigrationTime();
        count++;
    }
    public override void OnUpdate()
    {
        base.OnUpdate();
        if (agent == null) return;
        agent.speed = floatingAttribute.speed * 0.1f;
        if (TryUpdateCombat()) return;
        TryMigrate();
    }

    public override void OnDamaged(int value, EntityData attacker)
    {
        base.OnDamaged(value, attacker);
        if (attacker != null && attacker.type.IsCharacter())
        {
            SetTarget(attacker);
        }
    }

    public override void OnKilled()
    {
        base.OnKilled();
        BattleManager.AddChunkInfection(transform.position, Config.zombie_death_infection_delta);
        count--;
    }
    public override SCEntityDisplayInfo GetDisplayInfo()
    {
        return new SCEntityDisplayInfo()
        {
            id = id,
            type = type,
            level = level,
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z,
            yaw = transform.rotation.eulerAngles.y,
            speed = agent == null ? 0 : agent.velocity.magnitude,
            angularSpeed = agent == null ? 0 : agent.angularSpeed,
            health = floatingAttribute.health,
            maxHealth = baseAttribute.health,
            effectGraphic = effectController == null ? 0 : effectController.GetEffectGraphicInfo(),
        };
    }
    public void MoveTo(Vector3 pos)
    {
        if (agent == null || !agent.isOnNavMesh) return;
        agent.isStopped = false;
        agent.SetDestination(pos);
    }

    private bool TryUpdateCombat()
    {
        if (!TryGetTarget(out var target))
        {
            TryAcquireNearbyPlayer();
        }
        if (!TryGetTarget(out target)) return false;

        float attackRangeSqr = Config.npc_attack_range * Config.npc_attack_range;
        if ((target.transform.position - transform.position).sqrMagnitude > attackRangeSqr)
        {
            attackPreparing = false;
            MoveTo(target.transform.position);
            return true;
        }

        StopAgent();
        FaceTarget(target.transform.position);

        if (attackPreparing)
        {
            if (Time.time >= attackHitTime)
            {
                attackPreparing = false;
                nextAttackTime = Time.time + Config.npc_attack_post_time;
                if (TryGetTarget(out target) &&
                    (target.transform.position - transform.position).sqrMagnitude <= attackRangeSqr)
                {
                    target.OnDamaged(floatingAttribute.attack);
                }
            }
            return true;
        }

        if (Time.time >= nextAttackTime)
        {
            attackPreparing = true;
            attackHitTime = Time.time + Config.npc_attack_pre_time;
        }
        return true;
    }

    private bool TryGetTarget(out EntityData target)
    {
        target = null;
        if (!hasTarget) return false;
        if (!BattleManager.EntityContainer.Players.TryGetObject(targetPlayerId, out target) ||
            target == null ||
            !target.Alive)
        {
            ClearTarget();
            target = null;
            return false;
        }
        return true;
    }

    private void TryAcquireNearbyPlayer()
    {
        playerBuffer.Clear();
        BattleManager.EntityContainer.Players.GetIdsInRange(transform.position, Config.npc_attack_range, playerBuffer);
        EntityData nearest = null;
        float nearestSqr = float.MaxValue;
        foreach (int playerId in playerBuffer)
        {
            if (!BattleManager.EntityContainer.Players.TryGetObject((ushort)playerId, out var player) ||
                player == null ||
                !player.Alive)
            {
                continue;
            }

            float sqr = (player.transform.position - transform.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearest = player;
                nearestSqr = sqr;
            }
        }

        if (nearest != null)
        {
            SetTarget(nearest);
        }
    }

    private void SetTarget(EntityData target)
    {
        targetPlayerId = target.id;
        hasTarget = true;
        attackPreparing = false;
        nextAttackTime = 0;
    }

    private void ClearTarget()
    {
        hasTarget = false;
        targetPlayerId = 0;
        attackPreparing = false;
    }

    private void StopAgent()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude <= 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    private void TryMigrate()
    {
        if (Time.time < nextMigrationTime) return;
        ResetMigrationTime();
        if (!BattleManager.TryGetZombieMigrationTarget(transform.position, out var target)) return;
        MoveTo(target);
    }

    private void ResetMigrationTime()
    {
        nextMigrationTime = Time.time + Random.Range(
            Config.zombie_migration_min_cd,
            Config.zombie_migration_max_cd);
    }
}
