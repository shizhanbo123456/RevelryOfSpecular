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

        protected SkillStub(int id, float cd, int store)
        {
            this.id = id;
            this.cd = cd;
            this.store = store;
        }

        public override int Id => id;
        public override float CD => cd;
        public override int Store => store;
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
    public class SkillGaleSlash : SkillStub { public SkillGaleSlash() : base(0, 1f, 15) { } }          // 疾风斩
    public class SkillFlyingKnife : SkillStub { public SkillFlyingKnife() : base(1, 2f, 12) { } }      // 飞刀投掷
    public class SkillCrossSlash : SkillStub { public SkillCrossSlash() : base(2, 5f, 10) { } }        // 十字斩
    public class SkillChargeSlash : SkillStub { public SkillChargeSlash() : base(3, 5f, 10) { } }      // 冲锋斩（位移）
    public class SkillWhirlSlash : SkillStub { public SkillWhirlSlash() : base(4, 8f, 5) { } }         // 旋风斩
    public class SkillBreakEndureSlash : SkillStub { public SkillBreakEndureSlash() : base(5, 10f, 3) { } } // 破霸重斩
    public class SkillFanSwordQi : SkillStub { public SkillFanSwordQi() : base(6, 3f, 12) { } }        // 剑气纵横
    public class SkillCaptureThrow : SkillStub { public SkillCaptureThrow() : base(7, 12f, 4) { } }    // 捕获投掷
    public class SkillShadowStrike : SkillStub { public SkillShadowStrike() : base(8, 10f, 4) { } }    // 影袭（位移）
    public class SkillMountainCrash : SkillStub { public SkillMountainCrash() : base(9, 6f, 10) { } }  // 崩山击
    public class SkillBladeDance : SkillStub { public SkillBladeDance() : base(10, 4f, 12) { } }       // 刃舞连斩

    // ---- 长枪 11~17 ----
    public class SkillJavelinThrow : SkillStub { public SkillJavelinThrow() : base(11, 1.5f, 15) { } } // 投枪
    public class SkillPierceSpear : SkillStub { public SkillPierceSpear() : base(12, 4f, 10) { } }     // 贯穿之枪
    public class SkillThunderSpear : SkillStub { public SkillThunderSpear() : base(13, 10f, 3) { } }   // 落雷枪
    public class SkillBindNail : SkillStub { public SkillBindNail() : base(14, 12f, 4) { } }           // 束缚钉
    public class SkillThrustDash : SkillStub { public SkillThrustDash() : base(15, 5f, 10) { } }       // 突进刺（位移）
    public class SkillSweepPole : SkillStub { public SkillSweepPole() : base(16, 6f, 10) { } }         // 横扫千军
    public class SkillSpearWall : SkillStub { public SkillSpearWall() : base(17, 20f, 2) { } }         // 枪阵屏障

    // ---- 枪械 18~32 ----
    public class SkillQuickShot : SkillStub { public SkillQuickShot() : base(18, 0.5f, 20) { } }       // 快速射击
    public class SkillFanShotgun : SkillStub { public SkillFanShotgun() : base(19, 2f, 15) { } }       // 扇形散射
    public class SkillArmorPiercer : SkillStub { public SkillArmorPiercer() : base(20, 2f, 12) { } }   // 穿甲弹
    public class SkillGrenade : SkillStub { public SkillGrenade() : base(21, 3.5f, 10) { } }           // 榴弹
    public class SkillIncendiary : SkillStub { public SkillIncendiary() : base(22, 4f, 12) { } }       // 燃烧弹
    public class SkillFreezeRound : SkillStub { public SkillFreezeRound() : base(23, 5f, 10) { } }     // 冻结弹
    public class SkillParalysisRound : SkillStub { public SkillParalysisRound() : base(24, 5f, 10) { } } // 麻痹弹
    public class SkillToxicRound : SkillStub { public SkillToxicRound() : base(25, 6f, 10) { } }       // 毒气弹
    public class SkillHeavyBreaker : SkillStub { public SkillHeavyBreaker() : base(26, 4f, 12) { } }   // 破甲重弹
    public class SkillInspireRound : SkillStub { public SkillInspireRound() : base(27, 8f, 10) { } }   // 激励弹
    public class SkillImpactRound : SkillStub { public SkillImpactRound() : base(28, 8f, 10) { } }     // 冲击弹
    public class SkillBlinkDash : SkillStub { public SkillBlinkDash() : base(29, 12f, 4) { } }         // 闪现突进（位移）
    public class SkillSnipe : SkillStub { public SkillSnipe() : base(30, 10f, 5) { } }                 // 狙击
    public class SkillBulletSpray : SkillStub { public SkillBulletSpray() : base(31, 15f, 3) { } }     // 弹幕扫射
    public class SkillAirstrikeMark : SkillStub { public SkillAirstrikeMark() : base(32, 20f, 1) { } } // 空袭标记

    // ---- 魔法球 33~48 ----
    public class SkillMagicBolt : SkillStub { public SkillMagicBolt() : base(33, 0.6f, 20) { } }       // 魔弹
    public class SkillFireball : SkillStub { public SkillFireball() : base(34, 3f, 12) { } }           // 火球
    public class SkillIceShard : SkillStub { public SkillIceShard() : base(35, 3f, 12) { } }           // 冰锥术
    public class SkillWindBlade : SkillStub { public SkillWindBlade() : base(36, 2.5f, 12) { } }       // 风刃
    public class SkillPoisonOrb : SkillStub { public SkillPoisonOrb() : base(37, 4f, 10) { } }         // 毒珠
    public class SkillRockfall : SkillStub { public SkillRockfall() : base(38, 5f, 10) { } }           // 岩崩
    public class SkillThunderOrb : SkillStub { public SkillThunderOrb() : base(39, 5f, 10) { } }       // 雷球
    public class SkillLightShield : SkillStub { public SkillLightShield() : base(40, 10f, 8) { } }     // 光盾
    public class SkillPurgeWave : SkillStub { public SkillPurgeWave() : base(41, 10f, 8) { } }         // 净化波动
    public class SkillInspireCircle : SkillStub { public SkillInspireCircle() : base(42, 10f, 8) { } } // 激励法阵
    public class SkillFrostNova : SkillStub { public SkillFrostNova() : base(43, 10f, 6) { } }         // 冰霜新星
    public class SkillThunderFall : SkillStub { public SkillThunderFall() : base(44, 10f, 5) { } }     // 落雷术
    public class SkillBlackHoleBomb : SkillStub { public SkillBlackHoleBomb() : base(45, 5f, 8) { } }  // 黑洞炸弹
    public class SkillStarfall : SkillStub { public SkillStarfall() : base(46, 20f, 3) { } }           // 星辰坠落
    public class SkillVoidGrasp : SkillStub { public SkillVoidGrasp() : base(47, 15f, 4) { } }         // 虚空之握
    public class SkillArcaneBarrier : SkillStub { public SkillArcaneBarrier() : base(48, 20f, 2) { } } // 奥术屏障
}
