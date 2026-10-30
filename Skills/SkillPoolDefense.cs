using Ros.Skill.Utils;
using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    // 防守方 6 角色 × （主动1 / 主动2 / 大招），行为沿用策划案第五章，持续表现按《特效清单分别表1019》
    public static class SkillPoolDefense
    {
        public static void RegisterAll()
        {
            SkillManager.Register(new SkillRockShield());      // 鹿铠怪人 岩石护盾
            SkillManager.Register(new SkillPowerSuppress());   // 鹿铠怪人 怪力压制
            SkillManager.Register(new SkillCrystalStatueBlazeCast());  // 鹿铠怪人 灵火

            SkillManager.Register(new SkillEyeMark());         // 白眼伯爵 白眼标记
            SkillManager.Register(new SkillNightMark());       // 白眼伯爵 黑夜标记
            SkillManager.Register(new SkillForceNight());      // 白眼伯爵 立即入夜

            SkillManager.Register(new SkillSummonZombies());   // 死灵漫步者 召唤僵尸
            SkillManager.Register(new SkillDeathStrollCast()); // 死灵漫步者 死灵漫步
            SkillManager.Register(new SkillSummonElites());    // 死灵漫步者 召唤精英

            SkillManager.Register(new SkillSilenceCast());     // 蒙面教皇 沉默
            SkillManager.Register(new SkillMireCast());        // 蒙面教皇 泥沼
            SkillManager.Register(new SkillReflectCast());     // 蒙面教皇 反伤

            SkillManager.Register(new SkillPlagueMarkCast());  // 瘟疫使者 瘟疫标记
            SkillManager.Register(new SkillPlaguePollute());   // 瘟疫使者 瘟疫污染
            SkillManager.Register(new SkillDetonatePlague());  // 瘟疫使者 引爆瘟疫

            SkillManager.Register(new SkillPaleLightCast());   // 苍白舞者 苍白之光
            SkillManager.Register(new SkillPaleDarkCast());    // 苍白舞者 苍白之暗
            SkillManager.Register(new SkillFogCast());         // 苍白舞者 迷雾
        }
    }

    #region 鹿铠怪人
    public class SkillRockShield : SkillBase
    {
        public override int Id => (int)SkillType.SkillRockShield;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            var crystalStatue = TargetSelect.SelectNearest(BattleManager.EntityContainer.CrystalStatues, entity.transform.position,
                TargetRadius);
            context.AddInts(crystalStatue != null ? crystalStatue.id : entity.id); // 附近没塔就给自己
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                ApplyConfigBuff(caster, BattleManager.GetEntity((ushort)context.ints[i]), 0);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }

    // 怪力压制：自身位置连续 3 次范围攻击，命中挂 2 级减速
    public class SkillPowerSuppress : SkillBase
    {
        public override int Id => (int)SkillType.SkillPowerSuppress;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null) return;
            for (int i = 0; i < Config.power_suppress_hits; i++)
            {
                int hit = i;
                if (hit == 0) Smash(caster);
                else Timer.AddTimer(hit, _ => Smash(caster), Config.power_suppress_interval * hit, 1, false);
            }
        }

        // 有特效：每次砸击发一颗定在原地的子弹，伤害范围取 RM[5] 的命中球
        private void Smash(EntityData caster)
        {
            if (caster == null || !caster.Alive) return;
            Tool.BattleManager.ShootBullet(caster,
                BuildAttack(caster, Config.power_suppress_rate, addEffect: ConfigHitBuff(caster, 0)),
                TrialConstructor.Point(caster.transform.position, Config.power_suppress_vfx_life),
                Config.power_suppress_vfx_life,
                HitSpec(SkillVfxKind.RangeMagic, Config.power_suppress_vfx_index, VfxScale()));
        }

        public override void PlayVFX(SkillContext context)
        {
            Vector3 center = context.vectors[0];
            for (int i = 0; i < Config.power_suppress_hits; i++)
            {
                int hit = i;
                if (hit == 0) PlaySmash(center);
                else Timer.AddTimer(hit, _ => PlaySmash(center), Config.power_suppress_interval * hit, 1, false);
            }
        }

        private void PlaySmash(Vector3 center)
        {
            VfxHelper.PlayAt(SkillVfxKind.RangeMagic, Config.power_suppress_vfx_index, center,
                Config.power_suppress_vfx_life, VfxScale());
        }
    }

    public class SkillCrystalStatueBlazeCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillCrystalStatueBlazeCast;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            CollectInRange(entity, BattleManager.EntityContainer.CrystalStatues);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++) context.AddInts(TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                ApplyConfigBuff(caster, BattleManager.GetEntity((ushort)context.ints[i]), 0);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }
    #endregion

    #region 白眼伯爵
    public class SkillEyeMark : SkillBase
    {
        public override int Id => (int)SkillType.SkillEyeMark;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            var markTarget = Tool.BattleManager != null ? Tool.BattleManager.GetTopHarvester() : null;
            if (markTarget != null) context.AddInts(markTarget.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                ApplyConfigBuff(caster, BattleManager.GetEntity((ushort)context.ints[i]), 0);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }

    // 黑夜标记：自身不出现在敌方小地图（世界模型与视野判定不受影响）
    public class SkillNightMark : SkillBase
    {
        public override int Id => (int)SkillType.SkillNightMark;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ApplyConfigBuff(caster, caster, 0);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }

    public class SkillForceNight : SkillBase
    {
        public override int Id => (int)SkillType.SkillForceNight;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            if (Tool.EnvironmentManager != null) Tool.EnvironmentManager.SetCycleTime(0f);
        }

        public override void PlayVFX(SkillContext context) { } // 昼夜切换本身即表现
    }
    #endregion

    #region 死灵漫步者
    public class SkillSummonZombies : SkillBase
    {
        public override int Id => (int)SkillType.SkillSummonZombies;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null || Tool.BattleManager == null) return;
            int level = Tool.BattleManager.GetZombieSpawnLevel(); // 跟随自然刷新等级
            for (int i = 0; i < Config.summon_zombie_count; i++)
            {
                var type = EntityType.Zombie(UnityEngine.Random.Range(0, Config.zombie_variant_count));
                Tool.BattleManager.SpawnEntity(type, level,
                    caster.transform.position + TargetSelect.SummonOffset(i), EntityCamp.Zombie);
            }
        }

        public override void PlayVFX(SkillContext context) { } // 1019：无特效
    }

    public class SkillDeathStrollCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillDeathStrollCast;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ApplyConfigBuff(caster, caster, 0);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }

    public class SkillSummonElites : SkillBase
    {
        public override int Id => (int)SkillType.SkillSummonElites;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null || Tool.BattleManager == null) return;
            int level = Tool.BattleManager.GetZombieSpawnLevel(); // 跟随自然刷新等级
            for (int i = 0; i < Config.summon_elite_count; i++)
            {
                var type = EntityType.EliteZombie(UnityEngine.Random.Range(0, Config.elite_zombie_variant_count));
                Tool.BattleManager.SpawnEntity(type, level,
                    caster.transform.position + TargetSelect.SummonOffset(i, 3f), EntityCamp.Zombie);
            }
        }

        public override void PlayVFX(SkillContext context) { } // 1019：无特效
    }
    #endregion

    #region 蒙面教皇
    public class SkillSilenceCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillSilenceCast;
        public override float CastRange => 8f;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            CollectEnemies(entity, entity.transform.position);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++) context.AddInts(TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                ApplyConfigBuff(caster, BattleManager.GetEntity((ushort)context.ints[i]), 0);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }

    public class SkillMireCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillMireCast;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            CollectEnemies(entity, entity.transform.position);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++) context.AddInts(TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                ApplyConfigBuff(caster, BattleManager.GetEntity((ushort)context.ints[i]), 0);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }

    public class SkillReflectCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillReflectCast;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            CollectInRange(entity, BattleManager.EntityContainer.GuardStatues);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++) context.AddInts(TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                ApplyConfigBuff(caster, BattleManager.GetEntity((ushort)context.ints[i]), 0);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }
    #endregion

    #region 瘟疫使者
    public class SkillPlagueMarkCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillPlagueMarkCast;
        public override float CastRange => 8f;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            CollectEnemies(entity, entity.transform.position);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++) context.AddInts(TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                ApplyConfigBuff(caster, BattleManager.GetEntity((ushort)context.ints[i]), 0);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }

    // 瘟疫污染：给场上现有珊瑚挂污染，珊瑚被摧毁时把瘟疫标记扩散给附近进攻方
    public class SkillPlaguePollute : SkillBase
    {
        public override int Id => (int)SkillType.SkillPlaguePollute;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null) return;
            CollectInRange(caster, BattleManager.EntityContainer.Corals); // 只针对施放瞬间场上的珊瑚
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
                ApplyConfigBuff(caster, TargetSelect.TargetBuffer[i], 0);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }

    public class SkillDetonatePlague : SkillBase
    {
        public override int Id => (int)SkillType.SkillDetonatePlague;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            CollectEnemies(entity, entity.transform.position);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
            {
                var marked = TargetSelect.TargetBuffer[i];
                if (marked != null && marked.effectController != null && marked.effectController.GetLevel(EffectType.PlagueMark) > 0)
                    context.AddInts(marked.id);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
            {
                var target = BattleManager.GetEntity((ushort)context.ints[i]);
                if (target == null || target.effectController == null) continue;
                int level = target.effectController.GetLevel(EffectType.PlagueMark);
                if (level <= 0) continue;
                target.OnDamaged(level * Config.plague_detonate_damage, caster, damageType: CombatLabelType.PlagueBurst);
                target.effectController.RemoveEffect(EffectType.PlagueMark);
            }
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }
    #endregion

    #region 苍白舞者
    public class SkillPaleLightCast : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillPaleLightCast;
        protected override int HitBuffIndex => 0;
        public override SkillContext SkillLogic(EntityData entity) => ShotContext(entity);
        public override float CastRange => 20f;
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 5;
        protected override float SpreadDeg => 12f;
        protected override float BulletLife => 1.2f;
        protected override int VfxIndex => 2;
    }

    public class SkillPaleDarkCast : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillPaleDarkCast;
        protected override int HitBuffIndex => 0;
        public override SkillContext SkillLogic(EntityData entity) => ShotContext(entity);
        public override float CastRange => 20f;
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 5;
        protected override float SpreadDeg => 12f;
        protected override float BulletLife => 1.4f;
        protected override int VfxIndex => 1;

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
        {
            Vector3 origin = OriginPos((ushort)context.ints[0], OriginAnchor, OriginOffset, context.vectors[0]);
            Vector3 dest = FanDest(origin, context.vectors[1], shotIndex, shotsInWave, spread);
            return TrialConstructor.Arc(origin, dest, BulletLife);
        }
    }

    public class SkillFogCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillFogCast;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            CollectEnemies(entity, entity.transform.position);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++) context.AddInts(TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                ApplyConfigBuff(caster, BattleManager.GetEntity((ushort)context.ints[i]), 0);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }
    #endregion
}
