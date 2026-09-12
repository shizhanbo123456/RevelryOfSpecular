using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    /// <summary>
    /// 技能占位基类：只定 id / CD / 库存（数值来自策划案第二十一章技能池），效果留空待实现。
    /// 实现时继承占位类替换空方法即可（写法范例见 SkillFanShot 的轨迹上下文体系）。
    /// </summary>
    public abstract class SkillStub : SkillBase
    {
        private readonly int id;
        private readonly float cd;
        private readonly int store;
        private readonly WeaponRef weapon;

        /// <summary>weapon 省略时为 <see cref="WeaponRef.None"/>（防守方主动技能/大招/被动均无悬浮武器）。</summary>
        protected SkillStub(int id, float cd, int store, WeaponRef weapon = default)
        {
            this.id = id;
            this.cd = cd;
            this.store = store;
            this.weapon = weapon;
        }

        public override int Id => id;
        public override float CD => cd;
        public override int Store => store;
        public override WeaponRef Weapon => weapon;
        public override void DoDamageActs(EntityData entity, Vector3 dest) { } // TODO: 待实现
        public override void PlayVFX(TrajectoryContext context) { } // TODO: 待实现
    }

    /// <summary>武器技能池注册（49 件，id 见策划案第二十一章）。</summary>
    public static class SkillPoolWeapons
    {
        public static void RegisterAll()
        {
            // 近战刀 0~10
            SkillManager.Register(new SkillGaleSlash());
            SkillManager.Register(new SkillFlyingKnife());
            SkillManager.Register(new SkillCrossSlash());
            SkillManager.Register(new SkillChargeSlash());
            SkillManager.Register(new SkillWhirlSlash());
            SkillManager.Register(new SkillBreakEndureSlash());
            SkillManager.Register(new SkillFanSwordQi());
            SkillManager.Register(new SkillCaptureThrow());
            SkillManager.Register(new SkillShadowStrike());
            SkillManager.Register(new SkillMountainCrash());
            SkillManager.Register(new SkillBladeDance());
            // 长枪 11~17
            SkillManager.Register(new SkillJavelinThrow());
            SkillManager.Register(new SkillPierceSpear());
            SkillManager.Register(new SkillThunderSpear());
            SkillManager.Register(new SkillBindNail());
            SkillManager.Register(new SkillThrustDash());
            SkillManager.Register(new SkillSweepPole());
            SkillManager.Register(new SkillSpearWall());
            // 枪械 18~32
            SkillManager.Register(new SkillQuickShot());
            SkillManager.Register(new SkillFanShotgun());
            SkillManager.Register(new SkillArmorPiercer());
            SkillManager.Register(new SkillGrenade());
            SkillManager.Register(new SkillIncendiary());
            SkillManager.Register(new SkillFreezeRound());
            SkillManager.Register(new SkillParalysisRound());
            SkillManager.Register(new SkillToxicRound());
            SkillManager.Register(new SkillHeavyBreaker());
            SkillManager.Register(new SkillInspireRound());
            SkillManager.Register(new SkillImpactRound());
            SkillManager.Register(new SkillBlinkDash());
            SkillManager.Register(new SkillSnipe());
            SkillManager.Register(new SkillBulletSpray());
            SkillManager.Register(new SkillAirstrikeMark());
            // 魔法球 33~48
            SkillManager.Register(new SkillMagicBolt());
            SkillManager.Register(new SkillFireball());
            SkillManager.Register(new SkillIceShard());
            SkillManager.Register(new SkillWindBlade());
            SkillManager.Register(new SkillPoisonOrb());
            SkillManager.Register(new SkillRockfall());
            SkillManager.Register(new SkillThunderOrb());
            SkillManager.Register(new SkillLightShield());
            SkillManager.Register(new SkillPurgeWave());
            SkillManager.Register(new SkillInspireCircle());
            SkillManager.Register(new SkillFrostNova());
            SkillManager.Register(new SkillThunderFall());
            SkillManager.Register(new SkillBlackHoleBomb());
            SkillManager.Register(new SkillStarfall());
            SkillManager.Register(new SkillVoidGrasp());
            SkillManager.Register(new SkillArcaneBarrier());
        }
    }

    // ---- 近战刀 0~10 ----
    public class SkillGaleSlash : SkillStub { public SkillGaleSlash() : base(0, 1f, 15, new WeaponRef(WeaponCategory.Knife, 0)) { } }          // 疾风斩
    public class SkillFlyingKnife : SkillStub { public SkillFlyingKnife() : base(1, 2f, 12, new WeaponRef(WeaponCategory.Knife, 1)) { } }      // 飞刀投掷
    public class SkillCrossSlash : SkillStub { public SkillCrossSlash() : base(2, 5f, 10, new WeaponRef(WeaponCategory.Knife, 2)) { } }        // 十字斩
    public class SkillChargeSlash : SkillStub { public SkillChargeSlash() : base(3, 5f, 10, new WeaponRef(WeaponCategory.Knife, 3)) { } }      // 冲锋斩（位移）
    public class SkillWhirlSlash : SkillStub { public SkillWhirlSlash() : base(4, 8f, 5, new WeaponRef(WeaponCategory.Knife, 4)) { } }         // 旋风斩
    public class SkillBreakEndureSlash : SkillStub { public SkillBreakEndureSlash() : base(5, 10f, 3, new WeaponRef(WeaponCategory.Knife, 5)) { } } // 破霸重斩
    public class SkillFanSwordQi : SkillStub { public SkillFanSwordQi() : base(6, 3f, 12, new WeaponRef(WeaponCategory.Knife, 6)) { } }        // 剑气纵横
    public class SkillCaptureThrow : SkillStub { public SkillCaptureThrow() : base(7, 12f, 4, new WeaponRef(WeaponCategory.Knife, 7)) { } }    // 捕获投掷
    public class SkillShadowStrike : SkillStub { public SkillShadowStrike() : base(8, 10f, 4, new WeaponRef(WeaponCategory.Knife, 8)) { } }    // 影袭（位移）
    public class SkillMountainCrash : SkillStub { public SkillMountainCrash() : base(9, 6f, 10, new WeaponRef(WeaponCategory.Knife, 9)) { } }  // 崩山击
    public class SkillBladeDance : SkillStub { public SkillBladeDance() : base(10, 4f, 12, new WeaponRef(WeaponCategory.Knife, 10)) { } }       // 刃舞连斩

    // ---- 长枪 11~18 ----
    public class SkillJavelinThrow : SkillStub { public SkillJavelinThrow() : base(11, 1.5f, 15, new WeaponRef(WeaponCategory.Spear, 0)) { } } // 投枪
    public class SkillPierceSpear : SkillStub { public SkillPierceSpear() : base(12, 4f, 10, new WeaponRef(WeaponCategory.Spear, 1)) { } }     // 贯穿之枪
    public class SkillThunderSpear : SkillStub { public SkillThunderSpear() : base(13, 10f, 3, new WeaponRef(WeaponCategory.Spear, 2)) { } }   // 落雷枪
    public class SkillBindNail : SkillStub { public SkillBindNail() : base(14, 12f, 4, new WeaponRef(WeaponCategory.Spear, 3)) { } }           // 束缚钉
    public class SkillThrustDash : SkillStub { public SkillThrustDash() : base(15, 5f, 10, new WeaponRef(WeaponCategory.Spear, 4)) { } }       // 突进刺（位移）
    public class SkillSweepPole : SkillStub { public SkillSweepPole() : base(16, 6f, 10, new WeaponRef(WeaponCategory.Spear, 5)) { } }         // 横扫千军
    public class SkillSpearWall : SkillStub { public SkillSpearWall() : base(17, 20f, 2, new WeaponRef(WeaponCategory.Spear, 6)) { } }         // 枪阵屏障
    /// <summary>枪雨（id 18：升空后从天而降多枚长枪，落点范围穿刺；轨迹用 SkyFallTrajectory 逐个构建，特效 BulletVFX 46 + 落点 RangeMagicVFX 6）。</summary>
    public class SkillSpearRain : SkillStub { public SkillSpearRain() : base(18, 15f, 3, new WeaponRef(WeaponCategory.Spear, 7)) { } }         // 枪雨

    // ---- 枪械 19~33 ----
    public class SkillQuickShot : SkillStub { public SkillQuickShot() : base(19, 0.5f, 20, new WeaponRef(WeaponCategory.Gun, 0)) { } }       // 快速射击
    public class SkillFanShotgun : SkillStub { public SkillFanShotgun() : base(20, 2f, 15, new WeaponRef(WeaponCategory.Gun, 1)) { } }       // 扇形散射
    public class SkillArmorPiercer : SkillStub { public SkillArmorPiercer() : base(21, 2f, 12, new WeaponRef(WeaponCategory.Gun, 2)) { } }   // 穿甲弹
    public class SkillGrenade : SkillStub { public SkillGrenade() : base(22, 3.5f, 10, new WeaponRef(WeaponCategory.Gun, 3)) { } }           // 榴弹
    public class SkillIncendiary : SkillStub { public SkillIncendiary() : base(23, 4f, 12, new WeaponRef(WeaponCategory.Gun, 4)) { } }       // 燃烧弹
    public class SkillFreezeRound : SkillStub { public SkillFreezeRound() : base(24, 5f, 10, new WeaponRef(WeaponCategory.Gun, 5)) { } }     // 冻结弹
    public class SkillParalysisRound : SkillStub { public SkillParalysisRound() : base(25, 5f, 10, new WeaponRef(WeaponCategory.Gun, 6)) { } } // 麻痹弹
    public class SkillToxicRound : SkillStub { public SkillToxicRound() : base(26, 6f, 10, new WeaponRef(WeaponCategory.Gun, 7)) { } }       // 毒气弹
    public class SkillHeavyBreaker : SkillStub { public SkillHeavyBreaker() : base(27, 4f, 12, new WeaponRef(WeaponCategory.Gun, 8)) { } }   // 破甲重弹
    public class SkillInspireRound : SkillStub { public SkillInspireRound() : base(28, 8f, 10, new WeaponRef(WeaponCategory.Gun, 9)) { } }   // 激励弹
    public class SkillImpactRound : SkillStub { public SkillImpactRound() : base(29, 8f, 10, new WeaponRef(WeaponCategory.Gun, 10)) { } }     // 冲击弹
    public class SkillBlinkDash : SkillStub { public SkillBlinkDash() : base(30, 12f, 4, new WeaponRef(WeaponCategory.Gun, 11)) { } }         // 闪现突进（位移）
    public class SkillSnipe : SkillStub { public SkillSnipe() : base(31, 10f, 5, new WeaponRef(WeaponCategory.Gun, 12)) { } }                 // 狙击
    public class SkillBulletSpray : SkillStub { public SkillBulletSpray() : base(32, 15f, 3, new WeaponRef(WeaponCategory.Gun, 13)) { } }     // 弹幕扫射
    public class SkillAirstrikeMark : SkillStub { public SkillAirstrikeMark() : base(33, 20f, 1, new WeaponRef(WeaponCategory.Gun, 14)) { } } // 空袭标记

    // ---- 魔法球 34~49 ----
    public class SkillMagicBolt : SkillStub { public SkillMagicBolt() : base(34, 0.6f, 20, new WeaponRef(WeaponCategory.MagicOrb, 0)) { } }       // 魔弹
    public class SkillFireball : SkillStub { public SkillFireball() : base(35, 3f, 12, new WeaponRef(WeaponCategory.MagicOrb, 1)) { } }           // 火球
    public class SkillIceShard : SkillStub { public SkillIceShard() : base(36, 3f, 12, new WeaponRef(WeaponCategory.MagicOrb, 2)) { } }           // 冰锥术
    public class SkillWindBlade : SkillStub { public SkillWindBlade() : base(37, 2.5f, 12, new WeaponRef(WeaponCategory.MagicOrb, 3)) { } }       // 风刃
    public class SkillPoisonOrb : SkillStub { public SkillPoisonOrb() : base(38, 4f, 10, new WeaponRef(WeaponCategory.MagicOrb, 4)) { } }         // 毒珠
    public class SkillRockfall : SkillStub { public SkillRockfall() : base(39, 5f, 10, new WeaponRef(WeaponCategory.MagicOrb, 5)) { } }           // 岩崩
    public class SkillThunderOrb : SkillStub { public SkillThunderOrb() : base(40, 5f, 10, new WeaponRef(WeaponCategory.MagicOrb, 6)) { } }       // 雷球
    public class SkillLightShield : SkillStub { public SkillLightShield() : base(41, 10f, 8, new WeaponRef(WeaponCategory.MagicOrb, 7)) { } }     // 光盾
    public class SkillPurgeWave : SkillStub { public SkillPurgeWave() : base(42, 10f, 8, new WeaponRef(WeaponCategory.MagicOrb, 8)) { } }         // 净化波动
    public class SkillInspireCircle : SkillStub { public SkillInspireCircle() : base(43, 10f, 8, new WeaponRef(WeaponCategory.MagicOrb, 9)) { } } // 激励法阵
    public class SkillFrostNova : SkillStub { public SkillFrostNova() : base(44, 10f, 6, new WeaponRef(WeaponCategory.MagicOrb, 10)) { } }         // 冰霜新星
    public class SkillThunderFall : SkillStub { public SkillThunderFall() : base(45, 10f, 5, new WeaponRef(WeaponCategory.MagicOrb, 11)) { } }     // 落雷术
    public class SkillBlackHoleBomb : SkillStub { public SkillBlackHoleBomb() : base(46, 5f, 8, new WeaponRef(WeaponCategory.MagicOrb, 12)) { } }  // 黑洞炸弹
    public class SkillStarfall : SkillStub { public SkillStarfall() : base(47, 20f, 3, new WeaponRef(WeaponCategory.MagicOrb, 13)) { } }           // 星辰坠落
    public class SkillVoidGrasp : SkillStub { public SkillVoidGrasp() : base(48, 15f, 4, new WeaponRef(WeaponCategory.MagicOrb, 14)) { } }         // 虚空之握
    public class SkillArcaneBarrier : SkillStub { public SkillArcaneBarrier() : base(49, 20f, 2, new WeaponRef(WeaponCategory.MagicOrb, 15)) { } } // 奥术屏障
}
