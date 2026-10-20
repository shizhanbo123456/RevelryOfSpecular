namespace Ros.Skill
{
    // 技能 id 唯一来源：值由枚举顺序自动分配（禁止手写数字），全工程引用技能 id 一律读这里
    // 段长固定：刀 11 / 长枪 8 / 枪械 15 / 魔法球 16 / 防守方 18 / 非玩家 9 / 空手 3
    // 前四段与武器栏位一一对应（Config.GetRandomWeaponId、WeaponRef 依赖段连续），调整顺序会打乱武器与技能的对应
    public enum SkillType
    {
        // ===== 刀 11 =====
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

        // ===== 长枪 8 =====
        SkillBoomerangWeapon,
        SkillBackstabWeapon,
        SkillSwiftAura,
        SkillOrbitWeaponKnockback,
        SkillShortDash,
        SkillShieldTen,
        SkillFlyingKick,
        SkillIceFanTiny,

        // ===== 枪械 15 =====
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

        // ===== 魔法球 16 =====
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

        // ===== 防守方 18（6 角色 × 主动1 / 主动2 / 大招，被动不占 id）=====
        SkillRockShield,
        SkillMushroomInfect,
        SkillTowerBlazeCast,
        SkillEyeMark,
        SkillInfiniteVision,
        SkillForceNight,
        SkillSummonZombies,
        SkillDeathStrollCast,
        SkillSummonElites,
        SkillSilenceCast,
        SkillMireCast,
        SkillReflectCast,
        SkillPlagueMarkCast,
        SkillAbsorbOre,
        SkillDetonatePlague,
        SkillPaleLightCast,
        SkillPaleDarkCast,
        SkillFogCast,

        // ===== 非玩家单位 9 =====
        SkillZombieClawR,
        SkillZombieClawL,
        SkillEliteZombieClawR,
        SkillEliteZombieClawL,
        SkillEliteZombieScream,
        SkillTowerSporeShotA,
        SkillTowerSporeShotB,
        SkillTowerSporeShotC,
        SkillPlagueTreeSpore,

        // ===== 空手攻击 3 =====
        SkillPunchLeft,
        SkillPunchRight,
        SkillPunchSmash,
    }
}
