using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    public static class SkillPoolDefense
    {
        public static void RegisterAll()
        {
            SkillManager.Register(new SkillRockShield());        // 50 PC104 岩石护盾
            SkillManager.Register(new SkillMushroomInfect());    // 51 PC104 蘑菇感染
            SkillManager.Register(new SkillTowerBlazeCast());    // 52 PC104 灵火（大招）
            SkillManager.Register(new SkillEyeMark());           // 54 NP114 白眼标记
            SkillManager.Register(new SkillInfiniteVision());    // 55 NP114 无限视野
            SkillManager.Register(new SkillForceNight());        // 56 NP114 立即进入夜晚（大招）
            SkillManager.Register(new SkillSummonZombies());     // 58 PC106 召唤一小波僵尸
            SkillManager.Register(new SkillDeathStrollCast());   // 59 PC106 死灵漫步
            SkillManager.Register(new SkillSummonElites());      // 60 PC106 召唤多个精英僵尸（大招）
            SkillManager.Register(new SkillSilenceCast());       // 62 NP134 沉默
            SkillManager.Register(new SkillMireCast());          // 63 NP134 泥沼
            SkillManager.Register(new SkillReflectCast());       // 64 NP134 反伤（大招）
            SkillManager.Register(new SkillPlagueMarkCast());    // 66 PC102 瘟疫标记
            SkillManager.Register(new SkillAbsorbOre());         // 67 PC102 吸收矿石
            SkillManager.Register(new SkillDetonatePlague());    // 68 PC102 引爆瘟疫标记（大招）
            SkillManager.Register(new SkillPaleLightCast());     // 70 PC103 苍白之光
            SkillManager.Register(new SkillPaleDarkCast());      // 71 PC103 苍白之暗
            SkillManager.Register(new SkillFogCast());           // 72 PC103 迷雾（大招）
        }
    }

    #region PC104 鹿铠怪人（50~52）
    public class SkillRockShield : SkillBase
    {
        public override int Id => 50;
        public override float CD => 15f;
        public override int Store => 8;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            var tower = Utils.TargetSelect.SelectNearest(BattleManager.EntityContainer.Towers, entity.transform.position,
                Config.defense_ally_cast_radius);
            if (tower != null) context.AddInts(tower.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Short, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < SkillBase.SkillContextConventions.TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(SkillBase.SkillContextConventions.GetTargetId(context, i)), EffectType.TowerShield, 1,
                    Config.buff_duration_buff, shieldValue: Config.buff_shield_value, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context)
        {
            if (context.ints.Count > 1) PlayFollow(SkillVfxKind.Shield, 1, SkillBase.SkillContextConventions.GetTargetId(context, 0)); // S2
        }
    }

    public class SkillMushroomInfect : SkillBase
    {
        public override int Id => 51;
        public override float CD => 12f;
        public override int Store => 5;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            var crystal = Utils.TargetSelect.SelectNearest(BattleManager.EntityContainer.Crystals, entity.transform.position,
                Config.defense_ally_cast_radius);
            if (crystal != null) context.AddInts(crystal.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Short, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < SkillBase.SkillContextConventions.TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(SkillBase.SkillContextConventions.GetTargetId(context, i)), EffectType.MushroomInfect, 1,
                    Config.mushroom_infect_duration, negative: true, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) { } // 表现 = 模型替换，不走特效
    }

    public class SkillTowerBlazeCast : SkillBase
    {
        public override int Id => 52;
        public override float CD => 60f;
        public override int Store => 2;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            Utils.TargetSelect.SetEntitiesToBuffer(BattleManager.EntityContainer.Towers); // 全场防御塔
            SkillBase.SkillContextConventions.AddTargets(context, Utils.TargetSelect.TargetBuffer);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Long, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < SkillBase.SkillContextConventions.TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(SkillBase.SkillContextConventions.GetTargetId(context, i)), EffectType.TowerBlaze, 1,
                    Config.buff_duration_debuff, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayFollowAll(context, SkillVfxKind.Buff, 11); // BF12 塔身
    }
    #endregion

    #region NP114 白眼伯爵（54~56）
    public class SkillEyeMark : SkillBase
    {
        public override int Id => 54;
        public override float CD => 18f;
        public override int Store => 6;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            EntityData markTarget = null;
            if (Tool.BattleManager != null) markTarget = Tool.BattleManager.GetTopHarvester(); // 进攻方采集量最高者
            if (markTarget != null) context.AddInts(markTarget.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Short, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < SkillBase.SkillContextConventions.TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(SkillBase.SkillContextConventions.GetTargetId(context, i)), EffectType.EyeMark, 1,
                    Config.buff_duration_buff, negative: true, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context)
        {
            if (context.ints.Count > 1) PlayFollow(SkillVfxKind.Buff, 15, SkillBase.SkillContextConventions.GetTargetId(context, 0)); // BF16
        }
    }

    public class SkillInfiniteVision : SkillBase
    {
        public override int Id => 55;
        public override float CD => 45f;
        public override int Store => 3;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Short, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            if (caster == null) return;
            Utils.TargetSelect.SetEntitiesInCampToBuffer(caster.camp); // 己方全体
            for (int i = 0; i < Utils.TargetSelect.TargetBuffer.Count; i++)
            {
                GiveEffect(Utils.TargetSelect.TargetBuffer[i], EffectType.AttrViewDistance, 1, Config.buff_duration_buff,
                    value: Config.infinite_view_distance, sourceId: caster.id);
            }
        }

        public override void PlayVFX(SkillContext context) { } // 无特效
    }

    public class SkillForceNight : SkillBase
    {
        public override int Id => 56;
        public override float CD => 60f;
        public override int Store => 2;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Long, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            if (Tool.EnvironmentManager != null) Tool.EnvironmentManager.SetCycleTime(0f); // 周期值 0 = 午夜，即夜晚起点
        }

        public override void PlayVFX(SkillContext context) { } // 昼夜切换本身即表现
    }
    #endregion

    #region PC106 死灵漫步者（58~60）
    public class SkillSummonZombies : SkillBase
    {
        public override int Id => 58;
        public override float CD => 20f;
        public override int Store => 5;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position); // 召唤点 = 施放者位置（客户端在法阵处播特效）
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Short, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            if (caster == null) return;
            for (int i = 0; i < Config.summon_zombie_count; i++)
            {
                var type = EntityType.Zombie(UnityEngine.Random.Range(0, Config.zombie_variant_count));
                if (Tool.BattleManager != null) Tool.BattleManager.SpawnEntity(type, Config.summon_zombie_level,
                    caster.transform.position + Utils.TargetSelect.SummonOffset(i), EntityCamp.Zombie);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayAt(SkillVfxKind.MagicCircle, 6, context.vectors[0], 1.5f); // MC7
    }

    public class SkillDeathStrollCast : SkillBase
    {
        public override int Id => 59;
        public override float CD => 40f;
        public override int Store => 3;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Long, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            GiveEffect(caster, EffectType.DeathStroll, 1, Config.buff_duration_buff,
                value: Config.death_stroll_radius, damage: Config.buff_dot_damage,
                sourceId: caster != null ? caster.id : (ushort)0);
        }

        public override void PlayVFX(SkillContext context) => PlayFollow(SkillVfxKind.Buff, 3, SkillBase.SkillContextConventions.GetCasterId(context)); // BF4
    }

    public class SkillSummonElites : SkillBase
    {
        public override int Id => 60;
        public override float CD => 60f;
        public override int Store => 1;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Long, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            if (caster == null) return;
            for (int i = 0; i < Config.summon_elite_count; i++)
            {
                // 种类范围是精英僵尸自己的 14 种，不是普通僵尸的 21 种外观变体
                var type = EntityType.EliteZombie(UnityEngine.Random.Range(0, Config.elite_zombie_variant_count));
                if (Tool.BattleManager != null) Tool.BattleManager.SpawnEntity(type, Config.summon_elite_level,
                    caster.transform.position + Utils.TargetSelect.SummonOffset(i, 3f), EntityCamp.Zombie);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayAt(SkillVfxKind.MagicCircle, 6, context.vectors[0], 2f); // MC7
    }
    #endregion

    #region NP134 蒙面教皇（62~64）
    public class SkillSilenceCast : SkillBase
    {
        public override int Id => 62;
        public override float CastRange => 8f;
        public override float CD => 15f;
        public override int Store => 6;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            Utils.TargetSelect.SetEnemiesInRangeToBuffer(entity.transform.position, Config.defense_nearby_radius, entity.camp);
            SkillBase.SkillContextConventions.AddTargets(context, Utils.TargetSelect.TargetBuffer);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Short, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < SkillBase.SkillContextConventions.TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(SkillBase.SkillContextConventions.GetTargetId(context, i)), EffectType.Silence, 1,
                    Config.buff_duration_control, negative: true, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayFollowAll(context, SkillVfxKind.Buff, 4); // BF5
    }

    public class SkillMireCast : SkillBase
    {
        public override int Id => 63;
        public override float CD => 20f;
        public override int Store => 5;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            Utils.TargetSelect.SetEntitiesInCampToBuffer(Utils.TargetSelect.HostileOf(entity.camp)); // 全场敌方
            SkillBase.SkillContextConventions.AddTargets(context, Utils.TargetSelect.TargetBuffer);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Short, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < SkillBase.SkillContextConventions.TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(SkillBase.SkillContextConventions.GetTargetId(context, i)), EffectType.Mire, 1,
                    Config.buff_duration_debuff, negative: true, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayFollowAll(context, SkillVfxKind.Buff, 25); // BF26 水花
    }

    public class SkillReflectCast : SkillBase
    {
        public override int Id => 64;
        public override float CD => 60f;
        public override int Store => 2;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            Utils.TargetSelect.SetEntitiesToBuffer(BattleManager.EntityContainer.Beacons); // 所有守护点
            SkillBase.SkillContextConventions.AddTargets(context, Utils.TargetSelect.TargetBuffer);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Long, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < SkillBase.SkillContextConventions.TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(SkillBase.SkillContextConventions.GetTargetId(context, i)), EffectType.Reflect, 1,
                    Config.buff_duration_buff, damage: Config.buff_dot_damage, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayFollowAll(context, SkillVfxKind.MagicCircle, 8); // MC9
    }
    #endregion

    #region PC102 瘟疫使者（66~68）
    public class SkillPlagueMarkCast : SkillBase
    {
        public override int Id => 66;
        public override float CastRange => 8f;
        public override float CD => 2f;
        public override int Store => 12;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            Utils.TargetSelect.SetEnemiesInRangeToBuffer(entity.transform.position, Config.defense_nearby_radius, entity.camp);
            SkillBase.SkillContextConventions.AddTargets(context, Utils.TargetSelect.TargetBuffer);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Short, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < SkillBase.SkillContextConventions.TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(SkillBase.SkillContextConventions.GetTargetId(context, i)), EffectType.PlagueMark, 1,
                    Config.buff_duration_buff, negative: true, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayFollowAll(context, SkillVfxKind.Buff, 18); // BF19
    }

    public class SkillAbsorbOre : SkillBase
    {
        public override int Id => 67;
        public override float CastRange => 6f;
        public override float CD => 30f;
        public override int Store => 2;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Short, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            // 「矿石」即可采集水晶：直接造成巨量伤害 = 击败水晶，走概率产出流程
            Utils.TargetSelect.SetEntitiesInRangeToBuffer(BattleManager.EntityContainer.Crystals,
                caster != null ? caster.transform.position : context.vectors[0], Config.absorb_crystal_radius);
            for (int i = 0; i < Utils.TargetSelect.TargetBuffer.Count; i++)
            {
                if (Utils.TargetSelect.TargetBuffer[i] != null) Utils.TargetSelect.TargetBuffer[i].OnDamaged(Config.absorb_crystal_damage, caster);
            }
        }

        public override void PlayVFX(SkillContext context)
            => PlayAt(SkillVfxKind.RangeMagic, 1, context.vectors[0], 1f); // RM2
    }

    public class SkillDetonatePlague : SkillBase
    {
        public override int Id => 68;
        public override float CD => 30f;
        public override int Store => 1;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            // 目标 = 全场带「瘟疫标记」的敌人
            Utils.TargetSelect.SetEntitiesInCampToBuffer(Utils.TargetSelect.HostileOf(entity.camp));
            for (int i = 0; i < Utils.TargetSelect.TargetBuffer.Count; i++)
            {
                var marked = Utils.TargetSelect.TargetBuffer[i];
                if (marked != null && marked.effectController != null
                    && marked.effectController.GetLevel(EffectType.PlagueMark) > 0)
                {
                    context.AddInts(marked.id);
                }
            }
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Long, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < SkillBase.SkillContextConventions.TargetCount(context); i++)
            {
                var target = BattleManager.GetEntity(SkillBase.SkillContextConventions.GetTargetId(context, i));
                if (target == null || target.effectController == null) continue;
                int level = target.effectController.GetLevel(EffectType.PlagueMark);
                if (level <= 0) continue;
                target.OnDamaged(level * Config.plague_detonate_damage, caster); // 按层数结算
                target.effectController.RemoveEffect(EffectType.PlagueMark);
            }
            // 伤害归属用 cid（避免未使用告警）
            _ = cid;
        }

        public override void PlayVFX(SkillContext context) => PlayFollowAll(context, SkillVfxKind.Buff, 17); // BF18 黑绿喷发
    }
    #endregion

    #region PC103 苍白舞者（70~72）
    public class SkillPaleLightCast : SkillBase
    {
        public override int Id => 70;
        public override float CastRange => 20f;
        public override float CD => 1f;
        public override int Store => 12;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line,
                Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), shots, spreadDeg));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Hand_R, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, duration);

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            var attack = BuildAttack(caster, rate: 1f, radius: radius, useMagic: true,
                addEffect: ApplyEffect(EffectType.PaleLight, 1, Config.buff_duration_debuff, negative: true, sourceId: cid));
            ShootAll(caster, context, attack);
        }

        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 2 }); // B2

        private const int shots = 5;
        private const float spreadDeg = 12f;
        private const float radius = 0.4f;
        private const float duration = 1.2f;
    }

    public class SkillPaleDarkCast : SkillBase
    {
        public override int Id => 71;
        public override float CastRange => 20f;
        public override float CD => 2f;
        public override int Store => 12;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Bezier,
                Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), shots, spreadDeg));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Hand_R, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Arc(context, index, duration);

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            var attack = BuildAttack(caster, rate: 1f, radius: radius, useMagic: true,
                addEffect: ApplyEffect(EffectType.PaleDark, 1, Config.buff_duration_debuff, negative: true, sourceId: cid));
            ShootAll(caster, context, attack);
        }

        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 1 }); // B1

        private const int shots = 5;
        private const float spreadDeg = 12f;
        private const float radius = 0.4f;
        private const float duration = 1.4f;
    }

    public class SkillFogCast : SkillBase
    {
        public override int Id => 72;
        public override float CD => 50f;
        public override int Store => 2;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            Utils.TargetSelect.SetEntitiesInCampToBuffer(Utils.TargetSelect.HostileOf(entity.camp)); // 全体敌方
            SkillBase.SkillContextConventions.AddTargets(context, Utils.TargetSelect.TargetBuffer);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Long, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < SkillBase.SkillContextConventions.TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(SkillBase.SkillContextConventions.GetTargetId(context, i)), EffectType.Fog, 1,
                    Config.buff_duration_buff, negative: true, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayFollowAll(context, SkillVfxKind.Buff, 6); // BF7 紫雾喷发
    }
    #endregion
}
