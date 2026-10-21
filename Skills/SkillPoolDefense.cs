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
        public override float CD => 20f;
        public override int Store => 5;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

        private const float ShieldDuration = 15f;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            var crystalStatue = TargetSelect.SelectNearest(BattleManager.EntityContainer.CrystalStatues, entity.transform.position,
                Config.defense_ally_cast_radius);
            context.AddInts(crystalStatue != null ? crystalStatue.id : entity.id); // 附近没塔就给自己
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                GiveEffect(BattleManager.GetEntity((ushort)context.ints[i]), EffectType.StatueShield, 1,
                    ShieldDuration, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    // 怪力压制：自身位置连续 3 次范围攻击，命中挂 2 级减速
    public class SkillPowerSuppress : SkillBase
    {
        public override int Id => (int)SkillType.SkillPowerSuppress;
        public override float CD => 15f;
        public override int Store => 4;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

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

        private void Smash(EntityData caster)
        {
            if (caster == null || !caster.Alive) return;
            Vector3 center = caster.transform.position;
            var attack = BuildAttack(caster, Config.power_suppress_rate, Config.power_suppress_radius,
                addEffect: ApplyEffect(EffectType.SlowDown, 2, Config.power_suppress_slow_duration, caster));
            StrikeSphere(caster, center, Config.power_suppress_radius, attack);
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

        private static void PlaySmash(Vector3 center)
        {
            VfxHelper.PlayAt(SkillVfxKind.RangeMagic, Config.power_suppress_vfx_index, center,
                Config.power_suppress_vfx_life);
        }
    }

    public class SkillCrystalStatueBlazeCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillCrystalStatueBlazeCast;
        public override float CD => 60f;
        public override int Store => 2;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Long;

        private const float BlazeDuration = 12f;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            TargetSelect.SetEntitiesToBuffer(BattleManager.EntityContainer.CrystalStatues);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++) context.AddInts(TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                GiveEffect(BattleManager.GetEntity((ushort)context.ints[i]), EffectType.StatueBlaze, 1,
                    BlazeDuration, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }
    #endregion

    #region 白眼伯爵
    public class SkillEyeMark : SkillBase
    {
        public override int Id => (int)SkillType.SkillEyeMark;
        public override float CD => 18f;
        public override int Store => 6;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

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
                GiveEffect(BattleManager.GetEntity((ushort)context.ints[i]), EffectType.EyeMark, 1,
                    Config.buff_duration_buff, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    // 黑夜标记：自身不出现在敌方小地图（世界模型与视野判定不受影响）
    public class SkillNightMark : SkillBase
    {
        public override int Id => (int)SkillType.SkillNightMark;
        public override float CD => 18f;
        public override int Store => 6;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            GiveEffect(caster, EffectType.NightMark, 1, Config.night_mark_duration, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    public class SkillForceNight : SkillBase
    {
        public override int Id => (int)SkillType.SkillForceNight;
        public override float CD => 60f;
        public override int Store => 2;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Long;

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
        public override float CD => 20f;
        public override int Store => 5;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

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
        public override float CD => 40f;
        public override int Store => 3;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Long;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            GiveEffect(caster, EffectType.DeathStroll, 1, Config.buff_duration_buff, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    public class SkillSummonElites : SkillBase
    {
        public override int Id => (int)SkillType.SkillSummonElites;
        public override float CD => 60f;
        public override int Store => 1;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Long;

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
        public override float CD => 30f;
        public override int Store => 6;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

        private const float SilenceDuration = 12f;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            TargetSelect.SetEnemiesInRangeToBuffer(entity.transform.position, Config.defense_nearby_radius, entity.camp);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++) context.AddInts(TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                GiveEffect(BattleManager.GetEntity((ushort)context.ints[i]), EffectType.Silence, 1,
                    SilenceDuration, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    public class SkillMireCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillMireCast;
        public override float CD => 20f;
        public override int Store => 1;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            TargetSelect.SetEntitiesInCampToBuffer(TargetSelect.HostileOf(entity.camp));
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++) context.AddInts(TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                GiveEffect(BattleManager.GetEntity((ushort)context.ints[i]), EffectType.Mire, 1,
                    Config.buff_duration_debuff, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    public class SkillReflectCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillReflectCast;
        public override float CD => 60f;
        public override int Store => 2;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Long;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            TargetSelect.SetEntitiesToBuffer(BattleManager.EntityContainer.GuardStatues);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++) context.AddInts(TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                GiveEffect(BattleManager.GetEntity((ushort)context.ints[i]), EffectType.Reflect, 1,
                    Config.buff_duration_buff, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }
    #endregion

    #region 瘟疫使者
    public class SkillPlagueMarkCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillPlagueMarkCast;
        public override float CastRange => 8f;
        public override float CD => 2f;
        public override int Store => 12;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            TargetSelect.SetEnemiesInRangeToBuffer(entity.transform.position, Config.defense_nearby_radius, entity.camp);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++) context.AddInts(TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                GiveEffect(BattleManager.GetEntity((ushort)context.ints[i]), EffectType.PlagueMark, 1,
                    Config.plague_mark_duration, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    // 瘟疫污染：给场上现有珊瑚挂污染，珊瑚被摧毁时把瘟疫标记扩散给附近进攻方
    public class SkillPlaguePollute : SkillBase
    {
        public override int Id => (int)SkillType.SkillPlaguePollute;
        public override float CD => 30f;
        public override int Store => 2;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

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
            TargetSelect.SetEntitiesToBuffer(BattleManager.EntityContainer.Corals); // 只针对施放瞬间场上的珊瑚
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
                GiveEffect(TargetSelect.TargetBuffer[i], EffectType.PlaguePollute, 1, -1f, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    public class SkillDetonatePlague : SkillBase
    {
        public override int Id => (int)SkillType.SkillDetonatePlague;
        public override float CD => 30f;
        public override int Store => 1;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Long;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            TargetSelect.SetEntitiesInCampToBuffer(TargetSelect.HostileOf(entity.camp));
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
                target.OnDamaged(level * Config.plague_detonate_damage, caster);
                target.effectController.RemoveEffect(EffectType.PlagueMark);
            }
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }
    #endregion

    #region 苍白舞者
    public class SkillPaleLightCast : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillPaleLightCast;
        public override SkillContext SkillLogic(EntityData entity) => ShotContext(entity);
        public override float CastRange => 20f;
        public override float CD => 1f;
        public override int Store => 12;
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 5;
        protected override float SpreadDeg => 12f;
        protected override float BulletLife => 1.2f;
        protected override bool UseMagic => true;
        protected override float BulletRadius => 0.4f;
        protected override int VfxIndex => 2;
        protected override System.Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.PaleLight, 1, Config.buff_duration_debuff, caster);
    }

    public class SkillPaleDarkCast : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillPaleDarkCast;
        public override SkillContext SkillLogic(EntityData entity) => ShotContext(entity);
        public override float CastRange => 20f;
        public override float CD => 2f;
        public override int Store => 12;
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 5;
        protected override float SpreadDeg => 12f;
        protected override float BulletLife => 1.4f;
        protected override bool UseMagic => true;
        protected override float BulletRadius => 0.4f;
        protected override int VfxIndex => 1;
        protected override System.Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.PaleDark, 1, Config.buff_duration_debuff, caster);

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
        public override float CD => 50f;
        public override int Store => 2;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Long;

        private const float FogDuration = 20f;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            TargetSelect.SetEntitiesInCampToBuffer(TargetSelect.HostileOf(entity.camp));
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++) context.AddInts(TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                GiveEffect(BattleManager.GetEntity((ushort)context.ints[i]), EffectType.Fog, 1,
                    FogDuration, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }
    #endregion
}
