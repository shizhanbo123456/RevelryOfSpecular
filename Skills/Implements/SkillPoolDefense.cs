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

        // 附近水晶雕像挂盾；附近没有塔就给自己
        protected override void OnCast(EntityData caster)
        {
            EntityData statue = TargetSelect.SelectNearest(BattleManager.EntityContainer.CrystalStatues,
                caster.transform.position, TargetRadius);
            ApplyConfigBuff(caster, statue != null ? statue : caster, 0);
        }
    }

    // 怪力压制：自身位置连续 3 次范围攻击，命中挂 2 级减速
    public class SkillPowerSuppress : SkillBase
    {
        public override int Id => (int)SkillType.SkillPowerSuppress;

        protected override void OnCast(EntityData caster)
        {
            for (int i = 0; i < Config.power_suppress_hits; i++)
            {
                int hit = i;
                if (hit == 0) Smash(caster);
                else Timer.AddTimer(hit, _ => Smash(caster), Config.power_suppress_interval * hit, 1, false);
            }
            SendWave(caster, 0, context => context.WriteVector3(caster.transform.position));
        }

        // 每次砸击发一颗定在原地的子弹，伤害范围取 RM[5] 的命中球
        private void Smash(EntityData caster)
        {
            if (caster == null || !caster.Alive) return;
            Tool.BattleManager.ShootBullet(caster,
                BuildAttack(caster, addEffect: ConfigHitBuff(caster, 0)),
                new SkillTrajectoryPoint(caster.transform.position, Config.power_suppress_vfx_life),
                Config.power_suppress_vfx_life,
                HitSpec(SkillVfxKind.RangeMagic, Config.power_suppress_vfx_index, VfxScale()));
        }

        public override void PlayWave(SCSkillDisplayContext context)
        {
            Vector3 center = context.ReadVector3();
            for (int i = 0; i < Config.power_suppress_hits; i++)
            {
                int hit = i;
                if (hit == 0) PlaySmash(center);
                else Timer.AddTimer(hit, _ => PlaySmash(center), Config.power_suppress_interval * hit, 1, false);
            }
        }

        private void PlaySmash(Vector3 center)
            => VfxHelper.PlayAt(SkillVfxKind.RangeMagic, Config.power_suppress_vfx_index, center,
                Config.power_suppress_vfx_life, VfxScale());
    }

    public class SkillCrystalStatueBlazeCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillCrystalStatueBlazeCast;

        protected override void OnCast(EntityData caster)
        {
            CollectInRange(caster, BattleManager.EntityContainer.CrystalStatues);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
                ApplyConfigBuff(caster, TargetSelect.TargetBuffer[i], 0);
        }
    }
    #endregion

    #region 白眼伯爵
    public class SkillEyeMark : SkillBase
    {
        public override int Id => (int)SkillType.SkillEyeMark;

        protected override void OnCast(EntityData caster)
        {
            EntityData markTarget = Tool.BattleManager != null ? Tool.BattleManager.GetTopHarvester() : null;
            if (markTarget != null) ApplyConfigBuff(caster, markTarget, 0);
        }
    }

    // 黑夜标记：自身不出现在敌方小地图（世界模型与视野判定不受影响）
    public class SkillNightMark : SkillBase
    {
        public override int Id => (int)SkillType.SkillNightMark;

        protected override void OnCast(EntityData caster) => ApplyConfigBuff(caster, caster, 0);
    }

    public class SkillForceNight : SkillBase
    {
        public override int Id => (int)SkillType.SkillForceNight;

        protected override void OnCast(EntityData caster)
        {
            if (Tool.EnvironmentManager != null) Tool.EnvironmentManager.SetCycleTime(0f);
        }
    }
    #endregion

    #region 死灵漫步者
    public class SkillSummonZombies : SkillBase
    {
        public override int Id => (int)SkillType.SkillSummonZombies;

        protected override void OnCast(EntityData caster)
        {
            if (caster == null || Tool.BattleManager == null) return;
            int level = Tool.BattleManager.GetZombieSpawnLevel(); // 跟随自然刷新等级
            for (int i = 0; i < Config.summon_zombie_count; i++)
            {
                var type = EntityType.Zombie(UnityEngine.Random.Range(0, Config.zombie_variant_count));
                Tool.BattleManager.SpawnEntity(type, level,
                    caster.transform.position + TargetSelect.SummonOffset(i), EntityCamp.Defense);
            }
        }
    }

    public class SkillDeathStrollCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillDeathStrollCast;

        protected override void OnCast(EntityData caster) => ApplyConfigBuff(caster, caster, 0);
    }

    public class SkillSummonElites : SkillBase
    {
        public override int Id => (int)SkillType.SkillSummonElites;

        protected override void OnCast(EntityData caster)
        {
            if (caster == null || Tool.BattleManager == null) return;
            int level = Tool.BattleManager.GetZombieSpawnLevel(); // 跟随自然刷新等级
            for (int i = 0; i < Config.summon_elite_count; i++)
            {
                var type = EntityType.EliteZombie(UnityEngine.Random.Range(0, Config.elite_zombie_variant_count));
                Tool.BattleManager.SpawnEntity(type, level,
                    caster.transform.position + TargetSelect.SummonOffset(i, 3f), EntityCamp.Defense);
            }
        }
    }
    #endregion

    #region 蒙面教皇
    public class SkillSilenceCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillSilenceCast;

        protected override void OnCast(EntityData caster)
        {
            CollectEnemies(caster, caster.transform.position);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
                ApplyConfigBuff(caster, TargetSelect.TargetBuffer[i], 0);
        }
    }

    public class SkillMireCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillMireCast;

        protected override void OnCast(EntityData caster)
        {
            CollectEnemies(caster, caster.transform.position);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
                ApplyConfigBuff(caster, TargetSelect.TargetBuffer[i], 0);
        }
    }

    public class SkillReflectCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillReflectCast;

        protected override void OnCast(EntityData caster)
        {
            CollectInRange(caster, BattleManager.EntityContainer.GuardStatues);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
                ApplyConfigBuff(caster, TargetSelect.TargetBuffer[i], 0);
        }
    }
    #endregion

    #region 瘟疫使者
    public class SkillPlagueMarkCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillPlagueMarkCast;

        protected override void OnCast(EntityData caster)
        {
            CollectEnemies(caster, caster.transform.position);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
                ApplyConfigBuff(caster, TargetSelect.TargetBuffer[i], 0);
        }
    }

    // 瘟疫污染：给场上现有珊瑚挂污染，珊瑚被摧毁时把瘟疫标记扩散给附近进攻方
    public class SkillPlaguePollute : SkillBase
    {
        public override int Id => (int)SkillType.SkillPlaguePollute;

        protected override void OnCast(EntityData caster)
        {
            CollectInRange(caster, BattleManager.EntityContainer.Corals); // 只针对施放瞬间场上的珊瑚
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
                ApplyConfigBuff(caster, TargetSelect.TargetBuffer[i], 0);
        }
    }

    // 引爆：把范围内带瘟疫标记的敌人按标记等级结算固伤并移除标记
    public class SkillDetonatePlague : SkillBase
    {
        public override int Id => (int)SkillType.SkillDetonatePlague;

        protected override void OnCast(EntityData caster)
        {
            CollectEnemies(caster, caster.transform.position);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
            {
                EntityData target = TargetSelect.TargetBuffer[i];
                if (target == null || target.effectController == null) continue;
                int level = target.effectController.GetLevel(EffectType.PlagueMark);
                if (level <= 0) continue;
                target.OnDamaged(level * Config.plague_detonate_damage, caster, damageType: CombatLabelType.PlagueBurst);
                target.effectController.RemoveEffect(EffectType.PlagueMark);
            }
        }
    }
    #endregion

    #region 苍白舞者
    public class SkillPaleLightCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillPaleLightCast;

        private const int Vfx = 2;
        private const int Shots = 5;
        private const float SpreadDeg = 12f;
        private const float BulletLife = 1.2f;

        protected override void OnCast(EntityData caster)
        {
            Vector3 origin = OriginPos(caster, EntityAnchor.UpCenter, Vector3.zero);
            Vector3 aim = Aim(caster, origin);
            var attack = BuildAttack(caster, addEffect: ConfigHitBuff(caster, 0));
            var spec = HitSpec(SkillVfxKind.Bullet, Vfx, VfxScale());
            Spread.Fan(origin, aim, Shots, 1, SpreadDeg, 0f, BulletLife,
                trajectory => Tool.BattleManager.ShootBullet(caster, attack, trajectory, BulletLife, spec));
            SendWave(caster, 0, context => { context.WriteVector3(origin); context.WriteVector3(aim); });
        }

        public override void PlayWave(SCSkillDisplayContext context)
        {
            Vector3 origin = context.ReadVector3();
            Vector3 aim = context.ReadVector3();
            Spread.Fan(origin, aim, Shots, 1, SpreadDeg, 0f, BulletLife,
                trajectory => VfxHelper.PlayOne(SkillVfxKind.Bullet, Vfx, trajectory, VfxScale()));
        }
    }

    public class SkillPaleDarkCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillPaleDarkCast;

        private const int Vfx = 1;
        private const int Shots = 5;
        private const float SpreadDeg = 12f;
        private const float BulletLife = 1.4f;

        protected override void OnCast(EntityData caster)
        {
            Vector3 origin = OriginPos(caster, EntityAnchor.UpCenter, Vector3.zero);
            Vector3 aim = Aim(caster, origin);
            var attack = BuildAttack(caster, addEffect: ConfigHitBuff(caster, 0));
            var spec = HitSpec(SkillVfxKind.Bullet, Vfx, VfxScale());
            SpreadArcs(origin, aim, caster, attack, spec);
            SendWave(caster, 0, context => { context.WriteVector3(origin); context.WriteVector3(aim); });
        }

        // 这一发走贝塞尔弧线：扇形展开后每发各自抬成拱形
        private void SpreadArcs(Vector3 origin, Vector3 aim, EntityData caster, AttackData attack, BulletHitSpec spec)
        {
            Vector3[] dests = SpreadStyle.FanDests(origin, aim, Shots, SpreadDeg);
            for (int i = 0; i < dests.Length; i++)
            {
                Tool.BattleManager.ShootBullet(caster, attack,
                    new SkillTrajectoryBezier(origin, dests[i], BulletLife), BulletLife, spec);
            }
        }

        public override void PlayWave(SCSkillDisplayContext context)
        {
            Vector3 origin = context.ReadVector3();
            Vector3 aim = context.ReadVector3();
            Vector3[] dests = SpreadStyle.FanDests(origin, aim, Shots, SpreadDeg);
            for (int i = 0; i < dests.Length; i++)
            {
                VfxHelper.PlayOne(SkillVfxKind.Bullet, Vfx,
                    new SkillTrajectoryBezier(origin, dests[i], BulletLife), VfxScale());
            }
        }
    }

    public class SkillFogCast : SkillBase
    {
        public override int Id => (int)SkillType.SkillFogCast;

        protected override void OnCast(EntityData caster)
        {
            CollectEnemies(caster, caster.transform.position);
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
                ApplyConfigBuff(caster, TargetSelect.TargetBuffer[i], 0);
        }
    }
    #endregion
}
