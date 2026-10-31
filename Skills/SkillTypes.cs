namespace Ros.Skill
{
    // 技能 id 唯一来源：段首显式锚定，段内按枚举顺序自动递增，全工程引用技能 id 一律读这里
    // 段区间：刀 0~10 / 长枪 11~18 / 枪械 19~33 / 魔法球 34~49 / 防守方 60~77 / 僵尸 90~94 / 防御塔 110~112 / 中立雕像 113 / 空手 130~132
    // 段间留空供插入，段内必须连续：WeaponRef 用「段起点 + index」换算，Config.GetRandomWeaponId 依赖 0~49 整段随机
    public enum SkillType
    {
        // ===== 刀 0~10 =====
        SkillFanIceCone,
        SkillTripleThunderCone,
        SkillFreezeShot,
        SkillStrengthBreakShot,
        SkillDoubleFanShot,
        SkillOrbitBlades,
        SkillBackstepShot,
        SkillLeapScatter,
        SkillThrowWeapon,
        SkillFanWeaponThrow,
        SkillSkyfallWeapon,

        // ===== 长枪 11~18 =====
        SkillBoomerangWeapon,
        SkillBackstabWeapon,
        SkillSwiftAura,
        SkillOrbitWeaponKnockback,
        SkillShortDash,
        SkillShieldTen,
        SkillFlyingKick,
        SkillIceFanTiny,

        // ===== 枪械 19~33 =====
        SkillBackVolley,
        SkillIceSweep,
        SkillRootShot,
        SkillDriftOrb,
        SkillTripleWaveIce,
        SkillForwardSpray,
        SkillSilenceBolt,
        SkillSkyfallVolley,
        SkillOrbitWeaponVolley,
        SkillOrbitWeaponRain,
        SkillPullSmash,
        SkillRainBarrage,
        SkillDashSpeed,
        SkillAllyShieldEight,
        SkillShieldFour,

        // ===== 魔法球 34~49 =====
        SkillSplitShot,
        SkillPullBolt,
        SkillStaticBarrage,
        SkillFanFreeze,
        SkillSweepPoison,
        SkillBounceShot,
        SkillSweepBurn,
        SkillCircleVolley,
        SkillHookFan,
        SkillVortexBomb,
        SkillSixOrbs,
        SkillSkyfallSilence,
        SkillBlastKnockback,
        SkillBackArcWeapons,
        SkillAllyCritBoost,
        SkillAllyShieldTwentyFive,

        // ===== 防守方 60~77（6 角色 × 主动1 / 主动2 / 大招，被动不占 id）=====
        SkillRockShield = 60,
        SkillPowerSuppress,
        SkillCrystalStatueBlazeCast,
        SkillEyeMark,
        SkillNightMark,
        SkillForceNight,
        SkillSummonZombies,
        SkillDeathStrollCast,
        SkillSummonElites,
        SkillSilenceCast,
        SkillMireCast,
        SkillReflectCast,
        SkillPlagueMarkCast,
        SkillPlaguePollute,
        SkillDetonatePlague,
        SkillPaleLightCast,
        SkillPaleDarkCast,
        SkillFogCast,

        // ===== 僵尸 90~94 =====
        SkillZombieClawR = 90,
        SkillZombieClawL,
        SkillEliteZombieClawR,
        SkillEliteZombieClawL,
        SkillEliteZombieScream,

        // ===== 防御塔 110~112 =====
        SkillCrystalStatueSporeA = 110,
        SkillCrystalStatueSporeB,
        SkillCrystalStatueSporeC,

        // ===== 中立雕像 113 =====
        SkillNeutralStatueSpore,

        // ===== 空手攻击 130~132 =====
        SkillPunchLeft = 130,
        SkillPunchRight,
        SkillPunchSmash,
    }
}
