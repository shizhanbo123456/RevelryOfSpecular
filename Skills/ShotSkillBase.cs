using System;
using Ros.Skill.Utils;
using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    // 射击类技能公共骨架：一次施放 = WaveCount 波、每波 ShotsOfWave 发扇形；
    // 包里只放「施法者 id + 种子 + 施放瞬间的锚点位置 + 瞄准点」，发数/夹角/间隔/时长全部是本类常量。
    public abstract class ShotSkillBase : SkillBase
    {
        protected const int CasterIndex = 0;
        protected const int SeedIndex = 1;
        protected const int OriginVector = 0;
        protected const int AimVector = 1;

        protected abstract EntityAnchor OriginAnchor { get; }
        protected virtual Vector3 OriginOffset => Vector3.zero;
        protected abstract int Shots { get; }
        protected abstract float SpreadDeg { get; }
        protected abstract float BulletLife { get; }

        protected virtual float BulletRadius => 0.45f;
        protected virtual float DamageRate => 1f;
        protected virtual bool UseMagic => false;
        protected virtual float Knockback => 0f;
        protected virtual float DamageWindowFrom => 0f;
        protected virtual float DamageWindowTo => 1f;
        protected virtual float DamageInterval => 0f;

        protected virtual int WaveCount => 1;
        protected virtual float WaveInterval => 0.15f;
        protected virtual int ShotsOfWave(int wave) => Shots;
        protected virtual float SpreadOfWave(int wave) => SpreadDeg;

        protected virtual SkillVfxKind VfxKind => SkillVfxKind.Bullet;
        protected virtual int VfxIndex => -1;

        protected virtual Action<EntityEffectController> HitEffect(EntityData caster) => null;
        protected virtual Action<EntityData> HitAction(EntityData caster) => null;

        // 包内容由子类决定：显式声明要不要 seed / aim；需要别的内容就自己写 SkillLogic
        protected SkillContext ShotContext(EntityData entity, bool seed = false, bool aim = true)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (seed) context.AddInts(SkillSeed.New());
            context.AddVectors(OriginPos(entity, OriginAnchor, OriginOffset));
            if (aim) context.AddVectors(AimPoint(entity));
            return context;
        }

        // 文档标 [索敌] 的技能覆盖为 true，其余一律朝自身面向
        protected virtual bool AutoAim => false;
        protected virtual float ForwardAimDistance => CastRange > 0f ? CastRange : 8f;

        // 未锁定的落点：默认正前方 ForwardAimDistance；自身周围之类由技能覆盖
        protected virtual Vector3 AimPoint(EntityData entity)
            => AutoAim
                ? Utils.TargetSelect.AimPos(entity, CastRange)
                : entity.transform.position + entity.transform.forward * ForwardAimDistance;

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[CasterIndex]);
            if (caster == null || Tool.BattleManager == null) return;
            for (int wave = 0; wave < WaveCount; wave++)
            {
                int captured = wave;
                if (wave == 0) ShootWave(caster, context, 0);
                else Timer.AddTimer(captured, _ => ShootWave(caster, context, captured), WaveInterval * wave, 1, false);
            }
        }

        public override void PlayVFX(SkillContext context)
        {
            for (int wave = 0; wave < WaveCount; wave++)
            {
                int captured = wave;
                if (wave == 0) PlayWave(context, 0);
                else Timer.AddTimer(captured, _ => PlayWave(context, captured), WaveInterval * wave, 1, false);
            }
        }

        protected virtual void ShootWave(EntityData caster, SkillContext context, int wave)
        {
            if (caster == null || Tool.BattleManager == null) return;
            var attack = BuildAttack(caster, DamageRate, BulletRadius, useMagic: UseMagic,
                knockback: Knockback, addEffect: HitEffect(caster), onHit: HitAction(caster));
            int shots = Mathf.Max(1, ShotsOfWave(wave));
            float spread = SpreadOfWave(wave);
            for (int i = 0; i < shots; i++)
            {
                Tool.BattleManager.ShootBullet(caster, attack,
                    TrajectoryFor(context, wave, i, shots, spread), BulletLife,
                    DamageWindowFrom, DamageWindowTo, DamageInterval);
            }
        }

        protected virtual void PlayWave(SkillContext context, int wave)
        {
            if (VfxKind == SkillVfxKind.None || VfxIndex < 0) return;
            int shots = Mathf.Max(1, ShotsOfWave(wave));
            float spread = SpreadOfWave(wave);
            for (int i = 0; i < shots; i++)
                VfxHelper.PlayOne(VfxKind, VfxIndex, TrajectoryFor(context, wave, i, shots, spread));
        }

        // 双端同源：起点按锚点实时位置（没有实体就用包里冻结点），终点按扇形常量从瞄准点展开
        protected virtual BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
        {
            ushort casterId = (ushort)context.ints[CasterIndex];
            Vector3 origin = OriginPos(casterId, OriginAnchor, OriginOffset, context.vectors[OriginVector]);
            Vector3 aim = context.vectors[AimVector];
            Vector3 dest = FanDest(origin, aim, shotIndex, shotsInWave, spread);
            return TrialConstructor.Line(origin, dest, BulletLife);
        }

        protected static Vector3 FanDest(Vector3 origin, Vector3 aim, int index, int count, float spreadDeg)
        {
            if (count > 1)
            {
                Vector3[] dests = SpreadStyle.FanDests(origin, aim, count, spreadDeg);
                return dests[Mathf.Clamp(index, 0, dests.Length - 1)];
            }
            // 单发时 spread 表示整体偏转（扫射靠逐波改这个值实现）
            if (Mathf.Abs(spreadDeg) < 0.01f) return aim;
            return SpreadStyle.RotateTargetAroundDirection(origin, aim, spreadDeg);
        }

        // 服务器：实体锚点（含偏移）；客户端：表现体锚点；两者都拿不到才用包里冻结点
        protected static Vector3 OriginPos(EntityData entity, EntityAnchor anchor, Vector3 offset)
        {
            Vector3 p = anchor.GetTransform(entity.ModelInfo, entity.anim, entity.springWeapon).position;
            return p + entity.transform.rotation * offset;
        }

        protected static Vector3 OriginPos(ushort casterId, EntityAnchor anchor, Vector3 offset, Vector3 fallback)
        {
            var entity = BattleManager.GetEntity(casterId);
            if (entity != null) return OriginPos(entity, anchor, offset);
            var players = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.EntityPlayers : null;
            if (players != null && players.TryGetAnchorPos(casterId, anchor, out var pos))
            {
                if (BulletTrajectory.TryGetEntityTransform(casterId, out _, out var rot)) return pos + rot * offset;
                return pos + offset;
            }
            return fallback + offset;
        }
    }

    // 伪随机：种子由服务器在施放时生成并随包下发，两端按同一序列取数（禁止 UnityEngine.Random）
    public static class SkillSeed
    {
        public static int New() => UnityEngine.Random.Range(1, int.MaxValue);

        public static System.Random Rng(int seed) => new System.Random(seed);
    }
}
