using System.Collections.Generic;
using UnityEngine;

//纯特效
namespace Ros.Skill.PackageC
{
    public static class PackageManager//2000-2014 示例配置；新增技能时直接向 ConfigMap 添加
    {
        public static bool TryDoDamageActs(int id, EntityData entity, Vector3 dest, out (Vector3, Vector3) output)
        {
            if (id >= 2000 && id <= 2999)
            {
                if (!ConfigMap.TryGetValue(id, out var c))
                {
                    output = default;
                    return false;
                }
                Vector3 source = entity.BulletShootPos();
                // GetProjection 的 offset 参数是相对位移：终点 = source + (dest - source) = dest
                var curve=BezierCurve.GetProjection(source, dest - source, c.projectionRadius, c.roll);
                Tool.BattleManager.ShootBullet(entity, 0.2f, curve, 0.3f, c.distance*0.2f, Damageable.Once(), null);
                output = (source, dest);
                return true;
            }
            else
            {
                output = default;
                return false;
            }
        }
        /// <summary>从指定 source 发射 C 包子弹（服务端，供 A 包自定义发射源调用）。</summary>
        public static bool TryShootFrom(int id, EntityData entity, Vector3 source, Vector3 dest)
        {
            if (id < 2000 || id > 2999 || !ConfigMap.TryGetValue(id, out var c)) return false;
            var curve = BezierCurve.GetProjection(source, dest - source, c.projectionRadius, c.roll);
            Tool.BattleManager.ShootBullet(entity, 0.2f, curve, 0.3f, c.distance*0.2f, Damageable.Once(), null);
            return true;
        }
        public static bool TryPlayVFX(int id, Vector3 pos, Vector3 dest)
        {
            if (id >= 2000 && id <= 2999)
            {
                if (!ConfigMap.TryGetValue(id, out var c))
                {
                    return false;
                }
                var obj = Object.Instantiate(Tool.AssetsManager.Projections[(int)c.color+(int)c.style*5], pos, Quaternion.identity);
                var curve = BezierCurve.GetProjection(pos, dest - pos, c.projectionRadius, c.roll);
                BulletPlayer.Create(obj, curve,c.distance*0.2f, BulletPlayer.RotationMode.CompleteTangent);
                return true;
            }
            else
            {
                dest = Vector3.zero;
                return false;
            }
        }
        /// <summary>从指定 source 播放 C 包子弹特效（客户端，与 TryShootFrom 成对）。</summary>
        public static bool TryPlayFrom(int id, Vector3 source, Vector3 dest)
        {
            if (id < 2000 || id > 2999 || !ConfigMap.TryGetValue(id, out var c)) return false;
            var obj = Object.Instantiate(Tool.AssetsManager.Projections[(int)c.color+(int)c.style*5], source, Quaternion.identity);
            var curve = BezierCurve.GetProjection(source, dest - source, c.projectionRadius, c.roll);
            BulletPlayer.Create(obj, curve, c.distance*0.2f, BulletPlayer.RotationMode.CompleteTangent);
            return true;
        }
        public enum Color
        {
            Blue,Green,Purple,Red,Yellow
        }
        public enum Style
        {
            Explode,Star,Shine
        }
        public struct PackageC_Config
        {
            public Color color;
            public Style style;
            public float roll;
            public float projectionRadius;
            public float distance;
        }
        //新增技能时需要直接添加此表
        //id 布局：id = 2000 + color + style*5，与 Projections[color+style*5] 对齐
        //style 语义：Star=有轨迹（中距 8m）、Shine=长轨迹拖尾（远距 10m）
        //roll 语义：正 roll 抛物线向左弯，负 roll 向右弯（绕投射方向轴旋转）
        private static Dictionary<int, PackageC_Config> ConfigMap = new()
        {
            //--- Star 星星（5-9）---
            [2005] = new() { color = Color.Blue, style = Style.Star, roll = 0f, projectionRadius = 2f, distance = 8f },
            [2006] = new() { color = Color.Green, style = Style.Star, roll = 0f, projectionRadius = 2f, distance = 8f },
            [2008] = new() { color = Color.Red, style = Style.Star, roll = 0f, projectionRadius = 2f, distance = 8f },
            //--- Shine 闪耀轨迹（10-14）---
            [2010] = new() { color = Color.Blue, style = Style.Shine, roll = 0f, projectionRadius = 3f, distance = 10f },
            [2012] = new() { color = Color.Purple, style = Style.Shine, roll = 0f, projectionRadius = 3f, distance = 10f },
            [2013] = new() { color = Color.Red, style = Style.Shine, roll = 0f, projectionRadius = 3f, distance = 10f },
            //--- 红星左右偏 roll 变体（Skill11 聚能：左上/右上）---
            [2019] = new() { color = Color.Red, style = Style.Star, roll = 45f, projectionRadius = 2f, distance = 8f },  // 左偏
            [2020] = new() { color = Color.Red, style = Style.Star, roll = 90f, projectionRadius = 2f, distance = 8f },  // 左偏
            [2021] = new() { color = Color.Red, style = Style.Star, roll = -45f, projectionRadius = 2f, distance = 8f }, // 右偏
            [2022] = new() { color = Color.Red, style = Style.Star, roll = -90f, projectionRadius = 2f, distance = 8f }, // 右偏
        };
    }
}
