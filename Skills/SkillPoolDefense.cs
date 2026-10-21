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
            SkillManager.Register(new SkillRockShield());      // PC104 岩石护盾
            SkillManager.Register(new SkillMushroomInfect());  // PC104 蘑菇感染
            SkillManager.Register(new SkillTowerBlazeCast());  // PC104 灵火

            SkillManager.Register(new SkillEyeMark());         // NP114 白眼标记
            SkillManager.Register(new SkillInfiniteVision());  // NP114 无限视野
            SkillManager.Register(new SkillForceNight());      // NP114 立即入夜

            SkillManager.Register(new SkillSummonZombies());   // PC106 召唤僵尸
            SkillManager.Register(new SkillDeathStrollCast()); // PC106 死灵漫步
            SkillManager.Register(new SkillSummonElites());    // PC106 召唤精英

            SkillManager.Register(new SkillSilenceCast());     // NP134 沉默
            SkillManager.Register(new SkillMireCast());        // NP134 泥沼
            SkillManager.Register(new SkillReflectCast());     // NP134 反伤

            SkillManager.Register(new SkillPlagueMarkCast());  // PC102 瘟疫标记
            SkillManager.Register(new SkillAbsorbOre());       // PC102 吸收矿石
            SkillManager.Register(new SkillDetonatePlague());  // PC102 引爆瘟疫

            SkillManager.Register(new SkillPaleLightCast());   // PC103 苍白之光
            SkillManager.Register(new SkillPaleDarkCast());    // PC103 苍白之暗
            SkillManager.Register(new SkillFogCast());         // PC103 迷雾
        }
    }

    #region PC104 鹿铠怪人
    public class SkillRockShield : SkillBase
    {
        public override int Id => (int)SkillType.SkillRockShield;
        public override float CD => 12f;
        public override int Store => 5;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            var tower = TargetSelect.SelectNearest(BattleManager.EntityContainer.Towers, entity.transform.position,
                Config.defense_ally_cast_radius);
            if (tower != null) context.AddInts(tower.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                GiveEffect(BattleManager.GetEntity((ushort)context.ints[i]), EffectType.TowerShield, 1,
                    Config.buff_duration_buff, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    // 蘑菇感染：效果已移除，保留技能位与 id，待重做
    public class SkillMushroomInfect : SkillBase
    {
        public override int Id => (int)SkillType.SkillMushroomInfect;
        public override float CD => 15f;
        public override int Store => 4;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context) { }

        public override void PlayVFX(SkillContext context) { }
    }

    public class SkillTowerBlazeCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillTowerBlazeCast;
        public override float CD => 60f;
        public override int Store => 2;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Long;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            TargetSelect.SetEntitiesToBuffer(BattleManager.EntityContainer.Towers);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++) context.AddInts(TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                GiveEffect(BattleManager.GetEntity((ushort)context.ints[i]), EffectType.TowerBlaze, 1,
                    Config.buff_duration_debuff, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }
    #endregion

    #region NP114 白眼伯爵
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

    public class SkillInfiniteVision : SkillBase
    {
        public override int Id => (int)SkillType.SkillInfiniteVision;
        public override float CD => 45f;
        public override int Store => 3;
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
            TargetSelect.SetEntitiesInCampToBuffer(caster.camp);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
                GiveEffect(TargetSelect.TargetBuffer[i], EffectType.AttrViewDistanceBoost, 10,
                    Config.buff_duration_buff, caster);
        }

        public override void PlayVFX(SkillContext context) { } // 视野变化本身即表现
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

    #region PC106 死灵漫步者
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
            for (int i = 0; i < Config.summon_zombie_count; i++)
            {
                var type = EntityType.Zombie(UnityEngine.Random.Range(0, Config.zombie_variant_count));
                Tool.BattleManager.SpawnEntity(type, Config.summon_zombie_level,
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
            for (int i = 0; i < Config.summon_elite_count; i++)
            {
                var type = EntityType.EliteZombie(UnityEngine.Random.Range(0, Config.elite_zombie_variant_count));
                Tool.BattleManager.SpawnEntity(type, Config.summon_elite_level,
                    caster.transform.position + TargetSelect.SummonOffset(i, 3f), EntityCamp.Zombie);
            }
        }

        public override void PlayVFX(SkillContext context) { } // 1019：无特效
    }
    #endregion

    #region NP134 蒙面教皇
    public class SkillSilenceCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillSilenceCast;
        public override float CastRange => 8f;
        public override float CD => 15f;
        public override int Store => 6;
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
                GiveEffect(BattleManager.GetEntity((ushort)context.ints[i]), EffectType.Silence, 1,
                    Config.buff_duration_control, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    public class SkillMireCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillMireCast;
        public override float CD => 20f;
        public override int Store => 5;
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
            TargetSelect.SetEntitiesToBuffer(BattleManager.EntityContainer.Beacons);
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

    #region PC102 瘟疫使者
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
                    Config.buff_duration_buff, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    public class SkillAbsorbOre : SkillBase
    {
        public override int Id => (int)SkillType.SkillAbsorbOre;
        public override float CastRange => 6f;
        public override float CD => 30f;
        public override int Store => 2;
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
            // 「矿石」即可采集水晶：巨量伤害直接击碎，走原有产出流程
            TargetSelect.SetEntitiesInRangeToBuffer(BattleManager.EntityContainer.Crystals,
                caster != null ? caster.transform.position : context.vectors[0], Config.absorb_crystal_radius);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
                if (TargetSelect.TargetBuffer[i] != null) TargetSelect.TargetBuffer[i].OnDamaged(Config.absorb_crystal_damage, caster);
        }

        public override void PlayVFX(SkillContext context) { } // 1019 标为「由 buff 承担」，本技能不挂 buff
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

    #region PC103 苍白舞者
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
                    Config.buff_duration_buff, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }
    #endregion
}
