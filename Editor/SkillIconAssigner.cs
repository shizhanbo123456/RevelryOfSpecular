#if false // 图标分配表针对旧技能枚举，等新技能定稿后按新技能重写
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ros.Skill;
using UnityEditor;
using UnityEngine;

public static class SkillIconAssigner
{
    private const string MenuPath = "Tools/技能/按分配表为 SkillInfo 录入图标";
    private const string SkillInfoRoot = "Assets/Files/Info/Skill";
    private const string IconFolder = "Assets/Packages/UI/Magic Skill Icons Vol 2";

    private static readonly Dictionary<int, int> IconMap = new()
    {
        // Common 0~49
        [(int)SkillType.SkillGaleSlash] = 5,
        [(int)SkillType.SkillFlyingKnife] = 8,
        [(int)SkillType.SkillCrossSlash] = 44,
        [(int)SkillType.SkillChargeSlash] = 37,
        [(int)SkillType.SkillWhirlSlash] = 46,
        [(int)SkillType.SkillBreakEndureSlash] = 58,
        [(int)SkillType.SkillFanSwordQi] = 88,
        [(int)SkillType.SkillCaptureThrow] = 40,
        [(int)SkillType.SkillShadowStrike] = 24,
        [(int)SkillType.SkillMountainCrash] = 39,
        [(int)SkillType.SkillBladeDance] = 87,
        [(int)SkillType.SkillJavelinThrow] = 7,
        [(int)SkillType.SkillPierceSpear] = 36,
        [(int)SkillType.SkillThunderSpear] = 50,
        [(int)SkillType.SkillBindNail] = 15,
        [(int)SkillType.SkillThrustDash] = 53,
        [(int)SkillType.SkillSweepPole] = 60,
        [(int)SkillType.SkillSpearWall] = 78,
        [(int)SkillType.SkillSpearRain] = 52,
        [(int)SkillType.SkillQuickShot] = 45,
        [(int)SkillType.SkillFanShotgun] = 57,
        [(int)SkillType.SkillArmorPiercer] = 75,
        [(int)SkillType.SkillGrenade] = 76,
        [(int)SkillType.SkillIncendiary] = 82,
        [(int)SkillType.SkillFreezeRound] = 30,
        [(int)SkillType.SkillParalysisRound] = 19,
        [(int)SkillType.SkillToxicRound] = 35,
        [(int)SkillType.SkillHeavyBreaker] = 69,
        [(int)SkillType.SkillInspireRound] = 55,
        [(int)SkillType.SkillImpactRound] = 3,
        [(int)SkillType.SkillBlinkDash] = 79,
        [(int)SkillType.SkillSnipe] = 56,
        [(int)SkillType.SkillBulletSpray] = 10,
        [(int)SkillType.SkillAirstrikeMark] = 65,
        [(int)SkillType.SkillMagicBolt] = 29,
        [(int)SkillType.SkillFireball] = 86,
        [(int)SkillType.SkillIceShard] = 13,
        [(int)SkillType.SkillWindBlade] = 43,
        [(int)SkillType.SkillPoisonOrb] = 48,
        [(int)SkillType.SkillRockfall] = 84,
        [(int)SkillType.SkillThunderOrb] = 4,
        [(int)SkillType.SkillLightShield] = 23,
        [(int)SkillType.SkillPurgeWave] = 25,
        [(int)SkillType.SkillInspireCircle] = 34,
        [(int)SkillType.SkillFrostNova] = 26,
        [(int)SkillType.SkillThunderFall] = 68,
        [(int)SkillType.SkillBlackHoleBomb] = 6,
        [(int)SkillType.SkillStarfall] = 77,
        [(int)SkillType.SkillVoidGrasp] = 63,
        [(int)SkillType.SkillArcaneBarrier] = 32,
        // Defenser 50~72
        [(int)SkillType.SkillRockShield] = 9,
        [(int)SkillType.SkillMushroomInfect] = 51,
        [(int)SkillType.SkillTowerBlazeCast] = 83,
        [(int)SkillType.SkillEyeMark] = 12,
        [(int)SkillType.SkillInfiniteVision] = 66,
        [(int)SkillType.SkillForceNight] = 80,
        [(int)SkillType.SkillSummonZombies] = 72,
        [(int)SkillType.SkillDeathStrollCast] = 28,
        [(int)SkillType.SkillSummonElites] = 85,
        [(int)SkillType.SkillSilenceCast] = 42,
        [(int)SkillType.SkillMireCast] = 67,
        [(int)SkillType.SkillReflectCast] = 16,
        [(int)SkillType.SkillPlagueMarkCast] = 74,
        [(int)SkillType.SkillAbsorbOre] = 20,
        [(int)SkillType.SkillDetonatePlague] = 11,
        [(int)SkillType.SkillPaleLightCast] = 14,
        [(int)SkillType.SkillPaleDarkCast] = 70,
        [(int)SkillType.SkillFogCast] = 27,
        // Others 100~182（101/102、120/121、180/181、103/122 共用图标）
        [(int)SkillType.SkillZombieClawR] = 38,
        [(int)SkillType.SkillZombieClawL] = 38,
        [(int)SkillType.SkillZombieScream] = 22,
        [(int)SkillType.SkillEliteZombieClawR] = 71,
        [(int)SkillType.SkillEliteZombieClawL] = 71,
        [(int)SkillType.SkillEliteZombieScream] = 22,
        [(int)SkillType.SkillTowerSporeShot] = 47,
        [(int)SkillType.SkillPlagueTreeSpore] = 81,
        [(int)SkillType.SkillPunchLeft] = 17,
        [(int)SkillType.SkillPunchRight] = 17,
        [(int)SkillType.SkillPunchSmash] = 31,
    };

    [MenuItem(MenuPath, false, 100)]
    private static void AssignAll()
    {
        var guids = AssetDatabase.FindAssets("t:SkillInfo", new[] { SkillInfoRoot });
        int assigned = 0, unchanged = 0;
        var unmapped = new StringBuilder();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var info = AssetDatabase.LoadAssetAtPath<SkillInfo>(path);
            if (info == null) continue;

            if (!IconMap.TryGetValue(info.id, out int iconIndex))
            {
                unmapped.Append($"\n  {info.id} {info.name}（{path}）");
                continue;
            }

            var sprite = LoadIconSprite(iconIndex);
            if (sprite == null)
            {
                Debug.LogError($"[SkillIconAssigner] 图标加载失败：{IconPath(iconIndex)}（skill {info.id}）");
                continue;
            }

            if (info.icon == sprite)
            {
                unchanged++;
                continue;
            }

            info.icon = sprite;
            EditorUtility.SetDirty(info);
            assigned++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var report = $"[SkillIconAssigner] 完成：新录入 {assigned}，本已一致 {unchanged}。";
        if (unmapped.Length > 0)
            report += $"\n分配表中没有的技能 id（未处理）：{unmapped}";
        Debug.Log(report);
    }

    private static string IconPath(int index) => $"{IconFolder}/icon_{index}.png";

    private static Sprite LoadIconSprite(int index)
    {
        var path = IconPath(index);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;

        // 纹理未导入为 Sprite 时自动修正一次导入设置
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.Sprite) return null;
        importer.textureType = TextureImporterType.Sprite;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
