using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    /// <summary>
    /// 防守方角色专属技能池（id 50~73）。拥有者：防守方 6 角色，每角色「主动1 / 主动2 / 大招」各 1。
    /// 被动不占 id、不属技能系统（其效果直接插在战斗逻辑里，见《代码架构说明》）。
    /// 施法动作按策划案 12 章「小技能短施法、越大的技能越长」：主动用 Mega_Short，大招（CD ≥ 40）用 Mega_Long（待策划确认）。
    /// 特效下标换算：B* = BulletVFX 下标；S* / RM* / MC* / BF* = 编号 − 1（见《特效清单与分配表》）。
    /// 效果参数（Buff 类型/层数/时长、索敌半径）为占位初值，集中在 Config 的通用 Buff 数值段。
    /// </summary>
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
    /// <summary>岩石护盾（id 50）：为附近防御塔附加护盾。特效 S2 橙构筑。</summary>
    public class SkillRockShield : SkillBase
    {
        public override int Id => 50;
        public override float CD => 15f;
        public override int Store => 8;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            var tower = NearestIn(BattleManager.EntityContainer.Towers, entity.transform.position,
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
            var caster = Caster(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(TargetId(context, i)), EffectType.TowerShield, 1,
                    Config.buff_duration_buff, shieldValue: Config.buff_shield_value, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context)
        {
            if (context.ints.Count > 1) PlayFollow(SkillVfxKind.Shield, 1, (ushort)context.ints[1]); // S2
        }
    }

    /// <summary>蘑菇感染（id 51）：为附近水晶附加蘑菇感染，被进攻方摧毁时无产出。特效为模型替换（非特效）。</summary>
    public class SkillMushroomInfect : SkillBase
    {
        public override int Id => 51;
        public override float CD => 12f;
        public override int Store => 5;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            var crystal = NearestIn(BattleManager.EntityContainer.Crystals, entity.transform.position,
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
            var caster = Caster(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(TargetId(context, i)), EffectType.MushroomInfect, 1,
                    Config.mushroom_infect_duration, negative: true, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) { } // 表现 = 模型替换，不走特效
    }

    /// <summary>灵火（id 52·大招）：为全场防御塔附加灵火，使其攻击附带爆炸。特效 BF12 塔身附着 + RM8 岩浆连环爆炸。</summary>
    public class SkillTowerBlazeCast : SkillBase
    {
        public override int Id => 52;
        public override float CD => 60f;
        public override int Store => 2;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            AllIn(BattleManager.EntityContainer.Towers); // 全场防御塔
            AddTargets(context, TargetBuffer);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Long, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(TargetId(context, i)), EffectType.TowerBlaze, 1,
                    Config.buff_duration_debuff, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayFollowAll(context, SkillVfxKind.Buff, 11); // BF12 塔身
    }
    #endregion

    #region NP114 白眼伯爵（54~56）
    /// <summary>白眼标记（id 54）：为进攻方采集量最高者附加标记，己方小地图持续可见。特效 BF16 黄色周身泛光。</summary>
    public class SkillEyeMark : SkillBase
    {
        public override int Id => 54;
        public override float CD => 18f;
        public override int Store => 6;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            var markTarget = Tool.BattleManager?.GetTopHarvester(); // 进攻方采集量最高者
            if (markTarget != null) context.AddInts(markTarget.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Short, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(TargetId(context, i)), EffectType.EyeMark, 1,
                    Config.buff_duration_buff, negative: true, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context)
        {
            if (context.ints.Count > 1) PlayFollow(SkillVfxKind.Buff, 15, (ushort)context.ints[1]); // BF16
        }
    }

    /// <summary>无限视野（id 55）：己方视野短暂扩大到全图。无特效（属性修改：可见距离 +99999）。</summary>
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
            var caster = Caster(context);
            if (caster == null) return;
            AllInCamp(caster.camp); // 己方全体
            for (int i = 0; i < TargetBuffer.Count; i++)
            {
                GiveEffect(TargetBuffer[i], EffectType.AttrViewDistance, 1, Config.buff_duration_buff,
                    value: Config.infinite_view_distance, sourceId: caster.id);
            }
        }

        public override void PlayVFX(SkillContext context) { } // 无特效
    }

    /// <summary>立即进入夜晚（id 56·大招）：直接把昼夜推到夜晚起点，不改阶段时长。</summary>
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
            Tool.EnvironmentManager?.SetCycleTime(0f); // 周期值 0 = 午夜，即夜晚起点
        }

        public override void PlayVFX(SkillContext context) { } // 昼夜切换本身即表现
    }
    #endregion

    #region PC106 死灵漫步者（58~60）
    /// <summary>召唤一小波僵尸（id 58）：在施放者附近召唤。特效 MC7 深紫召唤法阵。</summary>
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
            var caster = Caster(context);
            if (caster == null) return;
            for (int i = 0; i < Config.summon_zombie_count; i++)
            {
                var type = EntityType.Zombie(UnityEngine.Random.Range(0, Config.zombie_variant_count));
                Tool.BattleManager?.SpawnEntity(type, Config.summon_zombie_level,
                    caster.transform.position + SummonOffset(i), EntityCamp.Defense);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayAt(SkillVfxKind.MagicCircle, 6, context.vectors[0], 1.5f); // MC7
    }

    /// <summary>死灵漫步（id 59）：自身获得强位移/绝对霸体 + 周围周期伤害。特效 BF4 血色缠绕。</summary>
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
            var caster = Caster(context);
            GiveEffect(caster, EffectType.DeathStroll, 1, Config.buff_duration_buff,
                value: Config.death_stroll_radius, damage: Config.buff_dot_damage,
                sourceId: caster != null ? caster.id : (ushort)0);
        }

        public override void PlayVFX(SkillContext context) => PlayFollow(SkillVfxKind.Buff, 3, (ushort)context.ints[0]); // BF4
    }

    /// <summary>召唤多个精英僵尸（id 60·大招）：大型召唤法阵（复用 MC7）。</summary>
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
            var caster = Caster(context);
            if (caster == null) return;
            for (int i = 0; i < Config.summon_elite_count; i++)
            {
                // 种类范围是精英僵尸自己的 14 种，不是普通僵尸的 21 种外观变体
                var type = EntityType.EliteZombie(UnityEngine.Random.Range(0, Config.elite_zombie_variant_count));
                Tool.BattleManager?.SpawnEntity(type, Config.summon_elite_level,
                    caster.transform.position + SummonOffset(i, 3f), EntityCamp.Defense);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayAt(SkillVfxKind.MagicCircle, 6, context.vectors[0], 2f); // MC7
    }
    #endregion

    #region NP134 蒙面教皇（62~64）
    /// <summary>沉默（id 62）：为附近敌人附加沉默。特效 BF5 黑色缠绕。</summary>
    public class SkillSilenceCast : SkillBase
    {
        public override int Id => 62;
        public override float CD => 15f;
        public override int Store => 6;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            EnemiesIn(entity.transform.position, Config.defense_nearby_radius, entity.camp);
            AddTargets(context, TargetBuffer);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Short, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(TargetId(context, i)), EffectType.Silence, 1,
                    Config.buff_duration_control, negative: true, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayFollowAll(context, SkillVfxKind.Buff, 4); // BF5
    }

    /// <summary>泥沼（id 63）：为全场敌方附加一层泥沼减速。特效 BF26 水花。</summary>
    public class SkillMireCast : SkillBase
    {
        public override int Id => 63;
        public override float CD => 20f;
        public override int Store => 5;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            AllInCamp(HostileOf(entity.camp)); // 全场敌方
            AddTargets(context, TargetBuffer);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Short, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(TargetId(context, i)), EffectType.Mire, 1,
                    Config.buff_duration_debuff, negative: true, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayFollowAll(context, SkillVfxKind.Buff, 25); // BF26 水花
    }

    /// <summary>反伤（id 64·大招）：为所有守护点附加反伤。特效 MC9 红色防御增益。</summary>
    public class SkillReflectCast : SkillBase
    {
        public override int Id => 64;
        public override float CD => 60f;
        public override int Store => 2;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            AllIn(BattleManager.EntityContainer.Beacons); // 所有守护点
            AddTargets(context, TargetBuffer);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Long, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(TargetId(context, i)), EffectType.Reflect, 1,
                    Config.buff_duration_buff, damage: Config.buff_dot_damage, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayFollowAll(context, SkillVfxKind.MagicCircle, 8); // MC9
    }
    #endregion

    #region PC102 瘟疫使者（66~68）
    /// <summary>瘟疫标记（id 66）：为附近敌人及带标记敌人附近者附加瘟疫标记。特效 BF19 自然（叠层标记）。</summary>
    public class SkillPlagueMarkCast : SkillBase
    {
        public override int Id => 66;
        public override float CD => 2f;
        public override int Store => 12;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            EnemiesIn(entity.transform.position, Config.defense_nearby_radius, entity.camp);
            AddTargets(context, TargetBuffer);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Short, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(TargetId(context, i)), EffectType.PlagueMark, 1,
                    Config.buff_duration_buff, negative: true, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayFollowAll(context, SkillVfxKind.Buff, 18); // BF19
    }

    /// <summary>吸收矿石（id 67）：立即摧毁并吸收周围矿石。特效 RM2 黑洞爆炸。</summary>
    public class SkillAbsorbOre : SkillBase
    {
        public override int Id => 67;
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
            var caster = Caster(context);
            // 「矿石」即可采集水晶：直接造成巨量伤害 = 击败水晶，走概率产出流程
            InRange(BattleManager.EntityContainer.Crystals,
                caster != null ? caster.transform.position : context.vectors[0], Config.absorb_crystal_radius);
            for (int i = 0; i < TargetBuffer.Count; i++)
            {
                TargetBuffer[i]?.OnDamaged(Config.absorb_crystal_damage, caster);
            }
        }

        public override void PlayVFX(SkillContext context)
            => PlayAt(SkillVfxKind.RangeMagic, 1, context.vectors[0], 1f); // RM2
    }

    /// <summary>引爆瘟疫标记（id 68·大招）：按层数造成中毒。特效 BF18 黑绿喷发。</summary>
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
            AllInCamp(HostileOf(entity.camp));
            for (int i = 0; i < TargetBuffer.Count; i++)
            {
                var marked = TargetBuffer[i];
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
            var caster = Caster(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < TargetCount(context); i++)
            {
                var target = BattleManager.GetEntity(TargetId(context, i));
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
    /// <summary>苍白之光（id 70）：多发直线飞弹，命中附加苍白之光标记。特效 B2 红黑能量球 + BF16 黄色泛光。</summary>
    public class SkillPaleLightCast : SkillBase
    {
        public override int Id => 70;
        public override float CD => 1f;
        public override int Store => 12;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line,
                FanDests(entity.transform.position, AimPos(entity), shots, spreadDeg));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Hand_R, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, duration);

        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
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

    /// <summary>苍白之暗（id 71）：多发曲射飞弹，命中附加苍白之暗标记。特效 B1 紫黑能量球 + BF1 紫雾。</summary>
    public class SkillPaleDarkCast : SkillBase
    {
        public override int Id => 71;
        public override float CD => 2f;
        public override int Store => 12;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Bezier,
                FanDests(entity.transform.position, AimPos(entity), shots, spreadDeg));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Hand_R, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Arc(context, index, duration);

        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
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

    /// <summary>迷雾（id 72·大招）：为全体敌方附加迷雾（压缩视野）。特效 BF7 紫雾喷发。</summary>
    public class SkillFogCast : SkillBase
    {
        public override int Id => 72;
        public override float CD => 50f;
        public override int Store => 2;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            AllInCamp(HostileOf(entity.camp)); // 全体敌方
            AddTargets(context, TargetBuffer);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Mega_Long, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort cid = caster != null ? caster.id : (ushort)0;
            for (int i = 0; i < TargetCount(context); i++)
            {
                GiveEffect(BattleManager.GetEntity(TargetId(context, i)), EffectType.Fog, 1,
                    Config.buff_duration_buff, negative: true, sourceId: cid);
            }
        }

        public override void PlayVFX(SkillContext context) => PlayFollowAll(context, SkillVfxKind.Buff, 6); // BF7 紫雾喷发
    }
    #endregion
}
