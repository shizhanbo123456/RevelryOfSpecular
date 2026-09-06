using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    /// <summary>防守方角色技能池注册（24 个，id 50~73，见策划案第二十一章；被动不可主动释放）。</summary>
    public static class SkillPoolDefense
    {
        public static void RegisterAll()
        {
            // PC104 鹿铠怪人
            SkillManager.Register(new SkillRockShield());
            SkillManager.Register(new SkillMushroomInfect());
            SkillManager.Register(new SkillTowerBlazeCast());
            SkillManager.Register(new PassiveCritParalysis());
            // NP114 白眼伯爵
            SkillManager.Register(new SkillEyeMark());
            SkillManager.Register(new SkillInfiniteVision());
            SkillManager.Register(new SkillForceNight());
            SkillManager.Register(new PassiveNightExtend());
            // PC106 死灵漫步者
            SkillManager.Register(new SkillSummonZombies());
            SkillManager.Register(new SkillDeathStrollCast());
            SkillManager.Register(new SkillSummonElites());
            SkillManager.Register(new PassiveZombieLevel());
            // NP134 蒙面教皇
            SkillManager.Register(new SkillSilenceCast());
            SkillManager.Register(new SkillMireCast());
            SkillManager.Register(new SkillReflectCast());
            SkillManager.Register(new PassivePopeGuard());
            // PC102 瘟疫使者
            SkillManager.Register(new SkillPlagueMarkCast());
            SkillManager.Register(new SkillAbsorbOre());
            SkillManager.Register(new SkillDetonatePlague());
            SkillManager.Register(new PassiveReviveSlow());
            // PC103 苍白舞者
            SkillManager.Register(new SkillPaleLightCast());
            SkillManager.Register(new SkillPaleDarkCast());
            SkillManager.Register(new SkillFogCast());
            SkillManager.Register(new PassiveLightDarkShift());
        }
    }

    /// <summary>被动技能占位（不可主动释放：store=0 恒不可施放，效果走被动逻辑 TODO）。</summary>
    public abstract class PassiveStub : SkillStub
    {
        protected PassiveStub(int id) : base(id, 0f, 0) { }
        public override bool Ranged => false;
    }

    // ---- PC104 鹿铠怪人 ----
    public class SkillRockShield : SkillStub { public SkillRockShield() : base(50, 15f, 8) { } }      // 岩石护盾
    public class SkillMushroomInfect : SkillStub { public SkillMushroomInfect() : base(51, 12f, 5) { } } // 蘑菇感染
    public class SkillTowerBlazeCast : SkillStub { public SkillTowerBlazeCast() : base(52, 60f, 2) { } } // 灵火（大招）
    public class PassiveCritParalysis : PassiveStub { public PassiveCritParalysis() : base(53) { } }   // 暴击麻痹

    // ---- NP114 白眼伯爵 ----
    public class SkillEyeMark : SkillStub { public SkillEyeMark() : base(54, 18f, 6) { } }             // 白眼标记
    public class SkillInfiniteVision : SkillStub { public SkillInfiniteVision() : base(55, 45f, 3) { } } // 无限视野
    public class SkillForceNight : SkillStub { public SkillForceNight() : base(56, 60f, 2) { } }       // 立即进入夜晚（大招）
    public class PassiveNightExtend : PassiveStub { public PassiveNightExtend() : base(57) { } }       // 夜间时间延长

    // ---- PC106 死灵漫步者 ----
    public class SkillSummonZombies : SkillStub { public SkillSummonZombies() : base(58, 20f, 5) { } } // 召唤一小波僵尸
    public class SkillDeathStrollCast : SkillStub { public SkillDeathStrollCast() : base(59, 40f, 3) { } } // 死灵漫步
    public class SkillSummonElites : SkillStub { public SkillSummonElites() : base(60, 60f, 1) { } }   // 召唤多个精英僵尸（大招）
    public class PassiveZombieLevel : PassiveStub { public PassiveZombieLevel() : base(61) { } }       // 僵尸刷新等级提升

    // ---- NP134 蒙面教皇 ----
    public class SkillSilenceCast : SkillStub { public SkillSilenceCast() : base(62, 15f, 6) { } }     // 沉默
    public class SkillMireCast : SkillStub { public SkillMireCast() : base(63, 20f, 5) { } }           // 泥沼
    public class SkillReflectCast : SkillStub { public SkillReflectCast() : base(64, 60f, 2) { } }     // 反伤（大招）
    public class PassivePopeGuard : PassiveStub { public PassivePopeGuard() : base(65) { } }           // 教皇守护

    // ---- PC102 瘟疫使者 ----
    public class SkillPlagueMarkCast : SkillStub { public SkillPlagueMarkCast() : base(66, 2f, 12) { } } // 瘟疫标记
    public class SkillAbsorbOre : SkillStub { public SkillAbsorbOre() : base(67, 30f, 2) { } }         // 吸收矿石
    public class SkillDetonatePlague : SkillStub { public SkillDetonatePlague() : base(68, 30f, 1) { } } // 引爆瘟疫标记（大招）
    public class PassiveReviveSlow : PassiveStub { public PassiveReviveSlow() : base(69) { } }         // 进攻方复活减速

    // ---- PC103 苍白舞者 ----
    public class SkillPaleLightCast : SkillStub { public SkillPaleLightCast() : base(70, 1f, 12) { } } // 苍白之光
    public class SkillPaleDarkCast : SkillStub { public SkillPaleDarkCast() : base(71, 2f, 12) { } }   // 苍白之暗
    public class SkillFogCast : SkillStub { public SkillFogCast() : base(72, 50f, 2) { } }             // 迷雾（大招）
    public class PassiveLightDarkShift : PassiveStub { public PassiveLightDarkShift() : base(73) { } } // 光暗转化
}
