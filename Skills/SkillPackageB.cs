using System.Collections.Generic;
using UnityEngine;
using static EntityEffectController;

//植物/矿石，烟雾
namespace Ros.Skill.PackageB
{
    public static class PackageManager//1000-1018
    {
        /// <summary>
        /// 烟雾 buff 映射（与 SkillVFX 颜色语义对应）：
        /// id-1000 → 0深红生命流失 / 1深红瞬间流失 / 2品红狂化 / 3品红致命 / 4浅红生命恢复 / 5浅红瞬间恢复 /
        /// 6深蓝移速降低 / 7浅蓝冰冻 / 8青色速度提升 / 9浅橙体力恢复 / 10浅橙防御提升 / 11浅橙燃烧 /
        /// 12深橙体力流失 / 13深橙防御降低 / 14白色净化 / 15深绿中毒 / 16粉色瘟疫 / 17红色爆发爆炸 / 18大范围白色雾气
        /// </summary>
        private static readonly (EffectType type, int level, float time)[] BuffMap = new[]
        {
            (EffectType.HealthDegeneration, 1, 5f),        //1000 深红 生命流失
            (EffectType.InstantDamage, 1, 0f),             //1001 深红 瞬间流失
            (EffectType.Berserk, 1, 5f),                   //1002 品红 狂化
            (EffectType.Deadly, 1, 5f),                    //1003 品红 致命
            (EffectType.HealthRegeneration, 1, 5f),        //1004 浅红 生命恢复
            (EffectType.InstantHealth, 1, 0f),             //1005 浅红 瞬间恢复
            (EffectType.Slowness, 1, 5f),                  //1006 深蓝 移速降低
            (EffectType.Freeze, 1, 5f),                    //1007 浅蓝 冰冻
            (EffectType.Speed, 1, 5f),                     //1008 青色 速度提升
            (EffectType.EnduranceRegeneration, 1, 5f),     //1009 浅橙 体力恢复
            (EffectType.DefenseBoost, 1, 5f),              //1010 浅橙 防御提升
            (EffectType.Burning, 1, 5f),                   //1011 浅橙 燃烧
            (EffectType.EnduranceDegeneration, 1, 5f),     //1012 深橙 体力流失
            (EffectType.DefenseReduce, 1, 5f),             //1013 深橙 防御降低
            (EffectType.Clear, 1, 0f),                     //1014 白色 净化
            (EffectType.Poison, 1, 5f),                    //1015 深绿 中毒
            (EffectType.Infection, 1, 5f),                 //1016 粉色 瘟疫
            (EffectType.InstantDamage, 1, 0f),             //1017 红色爆发 爆炸
            (EffectType.Clear, 1, 0f),                     //1018 大范围白色雾气（净化）
        };

        public static bool TryDoDamageActs(int id, EntityData entity, Vector3 dest, out (Vector3, Vector3) output)
        {
            if (id>=1000&&id<=1018)
            {
                Vector3 source = entity.BulletShootPos();
                var curve = BezierCurve.GetPoint(dest);
                // 命中时附加对应烟雾 buff
                Tool.BattleManager.ShootBullet(entity, 2f, curve, 5f, GetLifeTime(id), Damageable.Once(), add => AddBuff(id, add));
                output = (source, dest);
                return true;
            }
            else
            {
                output = (Vector3.zero, Vector3.zero);
                return false;
            }
        }
        private static void AddBuff(int id, System.Action<EffectType, int, float> addEffect)
        {
            int idx = id - 1000;
            if (idx < 0 || idx >= BuffMap.Length) return;
            var b = BuffMap[idx];
            addEffect(b.type, b.level, b.time);
        }
        public static bool TryPlayVFX(int id, Vector3 pos, Vector3 dest)
        {
            if (id >= 1000 && id <= 1018)
            {
                var obj = Object.Instantiate(Tool.AssetsManager.Dusts[id-1000], dest, Quaternion.identity);
                var curve = BezierCurve.GetPoint(dest);
                BulletPlayer.Create(obj, curve, GetLifeTime(id), BulletPlayer.RotationMode.Identity);
                return true;
            }
            else
            {
                dest = Vector3.zero;
                return false;
            }
        }
        private static float GetLifeTime(int id)
        {
            if (id == 1016) return 0.4f;
            if (id == 1017) return 17;
            return 1.2f;
        }
    }
}
