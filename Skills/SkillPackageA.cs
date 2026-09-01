using System.Collections.Generic;
using UnityEngine;

//普通技能
namespace Ros.Skill.PackageA
{
    public static class PackageManager//0-999
    {
        private static readonly List<SkillBase> s_list = new()
        {
            new Skill0(),
            new Skill1(),
            new Skill2(),
            new Skill3(),
            new Skill4(),
            new Skill5(),
            new Skill6(),
            new Skill7(),
            new Skill8(),
            new Skill9(),
            new Skill10(),
            new Skill11(),
            new Skill12(),
            new Skill13(),
            new Skill14(),
            new Skill15(),
            new Skill16(),
            new Skill17(),
            new Skill18(),
            new Skill19(),
            new Skill20(),
            new Skill21(),
            new Skill22(),
            new Skill23(),
            new Skill24(),
            new Skill25(),
            new Skill26(),
            new Skill27(),
            new Skill28(),
            new Skill29(),
            new Skill30(),
        };
        private static Dictionary<int, SkillBase> s_map;
        static PackageManager()
        {
            s_map = new Dictionary<int, SkillBase>();
            foreach(var s in s_list)
            {
                s_map.Add(s.Id, s);
            }
        }
        public static bool TryDoDamageActs(int id, EntityData entity, Vector3 dest, out (Vector3, Vector3) output)
        {
            if(s_map.TryGetValue(id,out var skill))
            {
                output = skill.DoDamageActs(entity, dest);
                return true;
            }
            else
            {
                output = (Vector3.zero, Vector3.zero);
                return false;
            }
        }
        public static bool TryPlayVFX(int id, Vector3 pos, Vector3 dest)
        {
            if (s_map.TryGetValue(id, out var skill))
            {
                skill.PlayVFX(pos, dest);
                return true;
            }
            else
            {
                dest = Vector3.zero;
                return false;
            }
        }
    }
    public class Skill0 : SkillBase
    {
        private const float lifeTime = 1.6f;
        public override int Id => 0;
        public override float CD => 7f;
        public override int Store => 3;
        // 攻击距离延长：终点延伸 2m（剑气穿行更远）
        private static Vector3 ExtendDest(Vector3 pos, Vector3 dest)
        {
            Vector3 dir = dest - pos;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            return dest + dir.normalized * 2f;
        }
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj=Object.Instantiate(Tool.AssetsManager.SkillVFX[51],pos,Quaternion.identity);
            var curve = BezierCurve.GetLine(pos, ExtendDest(pos, dest));
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var end = ExtendDest(entity.BulletShootPos(), dest);
            var curve=BezierCurve.GetLine(entity.BulletShootPos(), end);
            Tool.BattleManager.ShootBullet(entity, 2f, curve, 1f, lifeTime, Damageable.Once(),null);
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill1 : SkillBase
    {
        private const float lifeTime = 2f;
        public override int Id => 1;
        public override float CD => 4f;
        public override int Store => 2;
        // 以 pos→dest 方向为基准计算三线终点（服务端/客户端一致）
        private static Vector3[] TriDests(Vector3 pos, Vector3 dest)
        {
            FigureUtility.transform.position = Vector3.zero;
            FigureUtility.transform.LookAt(dest - pos);
            return new[] { dest, dest + FigureUtility.right * 3, dest - FigureUtility.right * 3 };
        }
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var dests = TriDests(pos, dest);
            var dest1 = dests[1];
            var dest2 = dests[2];
            dest = (dest - pos).normalized + pos;
            dest1 = (dest1 - pos).normalized + pos;
            dest2 = (dest2 - pos).normalized + pos;
            var curve = BezierCurve.GetLine(pos, dest);
            var curve1 = BezierCurve.GetLine(pos, dest1);
            var curve2 = BezierCurve.GetLine(pos, dest2);
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[37], pos, Quaternion.identity);
            var obj1 = Object.Instantiate(Tool.AssetsManager.SkillVFX[37], pos, Quaternion.identity);
            var obj2 = Object.Instantiate(Tool.AssetsManager.SkillVFX[37], pos, Quaternion.identity);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            BulletPlayer.Create(obj1, curve1, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            BulletPlayer.Create(obj2, curve2, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            // C级小改：三线方向各补一颗绿星轨迹
            PlayC(2006, pos, dests);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var pos = entity.BulletShootPos();
            var dests = TriDests(pos, dest);
            var dest1 = dests[1];
            var dest2 = dests[2];
            var curve = BezierCurve.GetLine(pos, dest);
            var curve1 = BezierCurve.GetLine(pos, dest1);
            var curve2 = BezierCurve.GetLine(pos, dest2);
            Tool.BattleManager.ShootBullet(entity, 0.5f, curve, 0.3f, lifeTime, Damageable.Once(), null);
            Tool.BattleManager.ShootBullet(entity, 0.5f, curve1, 0.3f, lifeTime, Damageable.Once(), null);
            Tool.BattleManager.ShootBullet(entity, 0.5f, curve2, 0.3f, lifeTime, Damageable.Once(), null);
            // 与特效一一对应的伤害子弹
            ShootC(2006, entity, dests);
            return (pos, dest);
        }
    }
    public class Skill2 : SkillBase
    {
        private const float lifeTime = 5f;
        public override int Id => 2;
        public override float CD => 15f;
        public override int Store => 2;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[38], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            // B级小改：主冰芒周围再落一圈冰芒（6颗蓝星，半径3与主判定一致）
            PlayC(2005, pos, CircleDests(dest, 3f, 6));
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 3f, curve, 0.5f, lifeTime, Damageable.Once().AvailableLifeTime(2.4f,2.7f), Effect);
            // 与特效对应：一圈冰芒伤害
            ShootC(2005, entity, CircleDests(dest, 3f, 6));
            return (entity.BulletShootPos(), dest);
        }
        private void Effect(System.Action<EntityEffectController.EffectType,int,float>addEffect)
        {
            addEffect(EntityEffectController.EffectType.Freeze, 1, 8f);
        }
    }
    public class Skill3 : SkillBase
    {
        private const float lifeTime = 5f;
        public override int Id => 3;
        public override float CD => 30f;
        public override int Store => 1;
        // 从主特效位置(dest)向周围随机延时/随机角度发射 12 颗紫星（伪随机，两端一致）
        private const int burstCount = 12;
        private static void BurstC(Vector3 dest, System.Action<Vector3, float, float> emit)
        {
            for (int i = 0; i < burstCount; i++)
            {
                float delay = FR(i) * 1.5f;
                float angle = FR(i + 5) * 360f;
                float dist = 2f + FR(i + 9) * 2.5f;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                emit(dir * dist + dest, delay, angle);
            }
        }
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[40], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            // B级明显优化：裂隙向四周延时散射12颗红星，发射源=主特效位置
            BurstC(dest, (target, delay, _) => DelayPlayC(2008, delay, dest, target));
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 0.4f, curve, 0.75f, lifeTime, Damageable.CD(0.3f).AvailableLifeTime(0,2f), null);
            // 与特效对应：从主特效位置延时发射的伤害子弹
            BurstC(dest, (target, delay, _) => DelayShootC(2008, delay, entity, dest, target));
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill4 : SkillBase
    {
        private const float lifeTime = 8f;
        public override int Id => 4;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[14], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            // A级小改：护盾展开时周围一圈红闪环绕（8颗，半径1.5）
            PlayC(2013, pos, CircleDests(dest, 1.5f, 8));
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            dest = entity.transform.position;
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 1f, lifeTime, Damageable.Once(), null);
            // 与特效对应：一圈红闪伤害
            ShootC(2013, entity, CircleDests(dest, 1.5f, 8));
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill5 : SkillBase
    {
        private const float lifeTime = 6f;
        public override int Id => 5;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[21], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 2f, lifeTime, Damageable.Once().AvailableLifeTime(3f,5f), null);
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill6 : SkillBase
    {
        private const float lifeTime = 2.5f;
        public override int Id => 6;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[29], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            // B级小改：阵法向四周延时散射8颗红星，发射源=主特效位置
            for (int i = 0; i < 8; i++)
            {
                float delay = FR(i) * 1.2f;
                float angle = FR(i + 3) * 360f;
                float dist = 1.5f + FR(i + 7) * 2f;
                Vector3 target = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * dist + dest;
                DelayPlayC(2008, delay, dest, target);
            }
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 1.5f, lifeTime, Damageable.Once().AvailableLifeTime(0.5f,2.5f), null);
            // 与特效对应：延时散射的伤害子弹
            for (int i = 0; i < 8; i++)
            {
                float delay = FR(i) * 1.2f;
                float angle = FR(i + 3) * 360f;
                float dist = 1.5f + FR(i + 7) * 2f;
                Vector3 target = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * dist + dest;
                DelayShootC(2008, delay, entity, dest, target);
            }
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill7 : SkillBase
    {
        private const float lifeTime = 2.5f;
        public override int Id => 7;
        // 发射者周围 20 个特效：内圈 8（半径1.2）、外圈 12（半径2.2），两端一致
        private static Vector3[] SurroundDests(Vector3 center)
        {
            var inner = CircleDests(center, 1.2f, 8, FR(2) * 45f);
            var outer = CircleDests(center, 2.2f, 12, FR(4) * 30f);
            var all = new Vector3[20];
            for (int i = 0; i < 8; i++) all[i] = inner[i];
            for (int i = 0; i < 12; i++) all[8 + i] = outer[i];
            return all;
        }
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            // 在发射者周围生成 20 个主特效（两个圈）
            foreach (var d in SurroundDests(pos))
            {
                var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[30], pos, Quaternion.identity);
                var curve = BezierCurve.GetPoint(d);
                BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            }
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var pos = entity.BulletShootPos();
            foreach (var d in SurroundDests(pos))
            {
                var curve = BezierCurve.GetPoint(d);
                Tool.BattleManager.ShootBullet(entity, 1f, curve, 0.5f, lifeTime, Damageable.Once().AvailableLifeTime(0.5f, 2f), null);
            }
            return (pos, dest);
        }
    }
    public class Skill8 : SkillBase
    {
        private const float lifeTime = 2f;
        public override int Id => 8;
        // 目标周围 5 个：1 个立即 + 4 个延时（伪随机延时），位置围绕 dest
        private static Vector3[] AroundDests(Vector3 dest)
        {
            var ring = CircleDests(dest, 1.8f, 4, FR(3) * 45f);
            return new[] { dest, ring[0], ring[1], ring[2], ring[3] };
        }
        private static float DelayOf(int i) => i == 0 ? 0f : 0.3f + FR(i) * 0.8f;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var dests = AroundDests(dest);
            for (int i = 0; i < dests.Length; i++)
            {
                int idx = i;
                DelayActs(DelayOf(idx), (pos, dests[idx]), p =>
                {
                    var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[31], p.Item1, Quaternion.identity);
                    BulletPlayer.Create(obj, BezierCurve.GetPoint(p.Item2), lifeTime, BulletPlayer.RotationMode.CompleteTangent);
                });
            }
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var pos = entity.BulletShootPos();
            var dests = AroundDests(dest);
            for (int i = 0; i < dests.Length; i++)
            {
                int idx = i;
                DelayActs(DelayOf(idx), (entity, pos, dests[idx]), p =>
                {
                    var curve = BezierCurve.GetPoint(p.Item3);
                    Tool.BattleManager.ShootBullet(p.Item1, 1f, curve, 2f, lifeTime, Damageable.Once().AvailableLifeTime(0f, 0.8f), null);
                });
            }
            return (pos, dest);
        }
    }
    public class Skill9 : SkillBase
    {
        private const float lifeTime = 10f;
        public override int Id => 9;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[1], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            // C级明显优化：蓝色子弹从主特效位置随机延时/随机角度散射（与Skill3同规律）
            for (int i = 0; i < 12; i++)
            {
                float delay = FR(i) * 1.5f;
                float angle = FR(i + 5) * 360f;
                float dist = 2f + FR(i + 9) * 2.5f;
                Vector3 target = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * dist + dest;
                DelayPlayC(2005, delay, dest, target);
            }
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 1.5f, lifeTime, Damageable.CD(0.3f).AvailableLifeTime(0.5f,10f), null);
            // 与特效对应：蓝色子弹伤害
            for (int i = 0; i < 12; i++)
            {
                float delay = FR(i) * 1.5f;
                float angle = FR(i + 5) * 360f;
                float dist = 2f + FR(i + 9) * 2.5f;
                Vector3 target = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * dist + dest;
                DelayShootC(2005, delay, entity, dest, target);
            }
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill10 : SkillBase
    {
        private const float lifeTime = 1.5f;
        public override int Id => 10;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[45], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 2.5f, lifeTime, Damageable.Once().AvailableLifeTime(1.3f, 1.5f), null);
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill11 : SkillBase
    {
        private const float lifeTime = 5f;
        public override int Id => 11;
        // 4 颗红星：2019/2020 正 roll 向左弯（左上），2021/2022 负 roll 向右弯（右上）
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[3], pos, Quaternion.identity);
            obj.transform.LookAt(dest);
            var curve = BezierCurve.GetPoint(pos);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.Constant);
            // A级小改：4颗红星从发射者左上方/右上方弯向目标（roll 正左负右）
            PlayC(2019, pos, dest); PlayC(2020, pos, dest);
            PlayC(2021, pos, dest); PlayC(2022, pos, dest);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var pos = entity.BulletShootPos();
            for(int i = 0; i < 10; i++)
            {
                DelayActs(3+i*0.2f, (entity, pos, dest), LatePlay);
            }
            // 与特效对应：roll 正负控制左右弯的伤害子弹
            ShootC(2019, entity, dest); ShootC(2020, entity, dest);
            ShootC(2021, entity, dest); ShootC(2022, entity, dest);
            return (entity.BulletShootPos(), dest);
        }
        private static void LatePlay((EntityData,Vector3,Vector3)p)
        {
            var curve = BezierCurve.GetLine(p.Item2, p.Item3);
            Tool.BattleManager.ShootBullet(p.Item1, 0.3f, curve, 0.5f, 0.3f, Damageable.Once(), null);
        }
    }
    public class Skill12 : SkillBase
    {
        private const float lifeTime = 1f;
        public override int Id => 12;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[48], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 1, lifeTime, Damageable.Once().AvailableLifeTime(0, 5f), null);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 1.5f, lifeTime, Damageable.Once().AvailableLifeTime(0.25f, 5f), null);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 2f, lifeTime, Damageable.Once().AvailableLifeTime(0.5f, 5f), null);
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill13 : SkillBase
    {
        private const float lifeTime = 3.5f;
        public override int Id => 13;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[9], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 4f, lifeTime, Damageable.Once().AvailableLifeTime(2.5f, 3f), null);
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill14 : SkillBase
    {
        private const float lifeTime = 3f;
        public override int Id => 14;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[50], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            // A级小改：蓝色子弹从主特效位置(dest)向外散开（6颗蓝闪，半径1.5）
            PlayCFrom(2010, dest, CircleDests(dest, 1.5f, 6));
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 1.5f, lifeTime, Damageable.CD(0.2f).AvailableLifeTime(1f, 3f), null);
            // 与特效对应：从主特效位置散开的伤害子弹
            ShootCFrom(2010, entity, dest, CircleDests(dest, 1.5f, 6));
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill15 : SkillBase
    {
        private const float lifeTime = 1.2f;
        public override int Id => 15;
        // 三个方向：前方、左前(30°)、右前(30°)，两端一致
        private static Vector3[] TriDirDests(Vector3 pos, Vector3 dest)
        {
            return new[] { dest, RotateDest(pos, dest, 30f), RotateDest(pos, dest, -30f) };
        }
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            foreach (var d in TriDirDests(pos, dest))
            {
                var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[11], pos, Quaternion.identity);
                BulletPlayer.Create(obj, BezierCurve.GetLine(pos, d), lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            }
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var pos = entity.BulletShootPos();
            foreach (var d in TriDirDests(pos, dest))
            {
                Tool.BattleManager.ShootBullet(entity, 1f, BezierCurve.GetLine(pos, d), 0.75f, lifeTime, Damageable.CD(0.2f), null);
            }
            return (pos, dest);
        }
    }
    public class Skill16 : SkillBase
    {
        private const float lifeTime = 1f;
        public override int Id => 16;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[12], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 1.2f, lifeTime, Damageable.Once(), null);
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill17 : SkillBase
    {
        private const float lifeTime = 3f;
        public override int Id => 17;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[13], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 2f, lifeTime, Damageable.Once().AvailableLifeTime(1f,1.2f), null);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 2f, lifeTime, Damageable.Once().AvailableLifeTime(1.5f,1.7f), null);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 2f, lifeTime, Damageable.Once().AvailableLifeTime(2f,2.2f), null);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 2f, lifeTime, Damageable.Once().AvailableLifeTime(2.5f,2.7f), null);
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill18 : SkillBase
    {
        private const float lifeTime = 1f;
        public override int Id => 18;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[17], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest-Vector3.up);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 2f, lifeTime, Damageable.Once(), null);
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill19 : SkillBase
    {
        private const float lifeTime = 2.5f;
        public override int Id => 19;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[18], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest - Vector3.up);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            // S级明显优化：雷球向四周弹射12颗紫闪（内圈6/外圈6错位）
            PlayC(2012, pos, CircleDests(dest, 2f, 6));
            PlayC(2012, pos, CircleDests(dest, 2.8f, 6, 30f));
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 1.5f, lifeTime, Damageable.CD(0.2f), null);
            // 与特效对应：两圈紫闪伤害
            ShootC(2012, entity, CircleDests(dest, 2f, 6));
            ShootC(2012, entity, CircleDests(dest, 2.8f, 6, 30f));
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill20 : SkillBase
    {
        private const float lifeTime = 1.2f;
        public override int Id => 20;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[19], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 2f, lifeTime, Damageable.Once().AvailableLifeTime(0.8f,1.2f), null);
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill21 : SkillBase
    {
        private const float lifeTime = 3f;
        public override int Id => 21;
        // 发射者周围 20 个主特效：内圈 8（半径1.2）、外圈 12（半径2.2）
        private static Vector3[] SurroundDests(Vector3 center)
        {
            var inner = CircleDests(center, 1.2f, 8, FR(2) * 45f);
            var outer = CircleDests(center, 2.2f, 12, FR(4) * 30f);
            var all = new Vector3[20];
            for (int i = 0; i < 8; i++) all[i] = inner[i];
            for (int i = 0; i < 12; i++) all[8 + i] = outer[i];
            return all;
        }
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            // 发射者周围 20 个主特效，伪随机延时逐个出现（0~1.2s）
            var dests = SurroundDests(pos);
            for (int i = 0; i < dests.Length; i++)
            {
                int idx = i;
                float delay = FR(i + 3) * 1.2f;
                DelayActs(delay, (pos, dests[idx] + Vector3.up), p =>
                {
                    var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[20], p.Item1, Quaternion.identity);
                    BulletPlayer.Create(obj, BezierCurve.GetPoint(p.Item2), lifeTime, BulletPlayer.RotationMode.CompleteTangent);
                });
            }
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var pos = entity.BulletShootPos();
            var dests = SurroundDests(pos);
            for (int i = 0; i < dests.Length; i++)
            {
                int idx = i;
                float delay = FR(i + 3) * 1.2f;
                DelayActs(delay, (entity, pos, dests[idx] + Vector3.up), p =>
                {
                    Tool.BattleManager.ShootBullet(p.Item1, 1f, BezierCurve.GetPoint(p.Item3), 1f, lifeTime, Damageable.CD(0.2f).AvailableLifeTime(0f,3f), null);
                });
            }
            return (pos, dest);
        }
    }
    public class Skill22 : SkillBase
    {
        private const float lifeTime = 1f;
        public override int Id => 22;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[22], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 4f, lifeTime, Damageable.Once().AvailableLifeTime(0f, 0.5f), null);
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill23 : SkillBase
    {
        private const float lifeTime = 4f;
        public override int Id => 23;
        // 加特林扫射：7 个方向，向目标方向但每个伪随机偏移（±12°）
        private static Vector3[] GatlingDests(Vector3 pos, Vector3 dest)
        {
            var dests = new Vector3[7];
            for (int i = 0; i < 7; i++)
            {
                float off = (FR(i) * 2f - 1f) * 12f;
                dests[i] = RotateDest(pos, dest, off);
            }
            return dests;
        }
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            foreach (var d in GatlingDests(pos, dest))
            {
                var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[27], pos, Quaternion.identity);
                obj.transform.LookAt(d);
                BulletPlayer.Create(obj, BezierCurve.GetLine(pos, d), lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            }
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var pos = entity.BulletShootPos();
            foreach (var d in GatlingDests(pos, dest))
            {
                Tool.BattleManager.ShootBullet(entity, 1f, BezierCurve.GetLine(pos, d), 0.3f, lifeTime, Damageable.Once().AvailableLifeTime(0f,3f), null);
            }
            return (pos, dest);
        }
    }
    public class Skill24 : SkillBase
    {
        private const float lifeTime = 7f;
        public override int Id => 24;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[28], pos, Quaternion.identity);
            var curve = BezierCurve.GetPoint(dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var curve = BezierCurve.GetPoint(dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 2.5f, lifeTime, Damageable.CD(0.5f).AvailableLifeTime(2.8f,7f), null);
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill25 : SkillBase
    {
        private const float lifeTime = 0.5f;
        public override int Id => 25;
        // 加特林扫射：7 个方向，向目标方向但每个伪随机偏移（±12°）
        private static Vector3[] GatlingDests(Vector3 pos, Vector3 dest)
        {
            var dests = new Vector3[7];
            for (int i = 0; i < 7; i++)
            {
                float off = (FR(i) * 2f - 1f) * 12f;
                dests[i] = RotateDest(pos, dest, off);
            }
            return dests;
        }
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            foreach (var d in GatlingDests(pos, dest))
            {
                var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[32], pos, Quaternion.identity);
                obj.transform.LookAt(d);
                BulletPlayer.Create(obj, BezierCurve.GetLine(pos, d), lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            }
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var pos = entity.BulletShootPos();
            foreach (var d in GatlingDests(pos, dest))
            {
                Tool.BattleManager.ShootBullet(entity, 1f, BezierCurve.GetLine(pos, d), 0.3f, lifeTime, Damageable.Once(), null);
            }
            return (pos, dest);
        }
    }
    public class Skill26 : SkillBase
    {
        private const float lifeTime = 0.5f;
        public override int Id => 26;
        // 扇形5发：角度 -30/-15/0/+15/+30（两两夹角15°）
        private static readonly float[] s_angles = { -30f, -15f, 0f, 15f, 30f };
        private static Vector3[] Fan5Dests(Vector3 pos, Vector3 dest)
        {
            var dests = new Vector3[5];
            for (int i = 0; i < 5; i++) dests[i] = RotateDest(pos, dest, s_angles[i]);
            return dests;
        }
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            foreach (var d in Fan5Dests(pos, dest))
            {
                var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[33], pos, Quaternion.identity);
                obj.transform.LookAt(d);
                BulletPlayer.Create(obj, BezierCurve.GetLine(pos, d), lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            }
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var pos = entity.BulletShootPos();
            foreach (var d in Fan5Dests(pos, dest))
            {
                Tool.BattleManager.ShootBullet(entity, 1f, BezierCurve.GetLine(pos, d), 0.25f, lifeTime, Damageable.Once(), null);
            }
            return (pos, dest);
        }
    }
    public class Skill27 : SkillBase
    {
        private const float lifeTime = 10f;
        public override int Id => 27;
        // 3 个方向：-30/0/+30（两两夹角30°），与 Skill26 类似
        private static readonly float[] s_angles = { -30f, 0f, 30f };
        private static Vector3[] Tri30Dests(Vector3 pos, Vector3 dest)
        {
            var dests = new Vector3[3];
            for (int i = 0; i < 3; i++) dests[i] = RotateDest(pos, dest, s_angles[i]);
            return dests;
        }
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            foreach (var d in Tri30Dests(pos, dest))
            {
                var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[34], pos, Quaternion.identity);
                BulletPlayer.Create(obj, BezierCurve.GetLine(pos, d), lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            }
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var pos = entity.transform.position;
            foreach (var d in Tri30Dests(pos, dest))
            {
                var curve = BezierCurve.GetLine(pos, d);
                Tool.BattleManager.ShootBullet(entity, 1f, curve, 2f, lifeTime, Damageable.CD(0.3f), null);
            }
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill28 : SkillBase
    {
        private const float lifeTime = 6f;
        public override int Id => 28;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            // 发射者周围 8 个主特效围成一圈（半径3.5）
            foreach (var d in CircleDests(pos, 3.5f, 8, FR(1) * 45f))
            {
                var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[35], pos, Quaternion.identity);
                BulletPlayer.Create(obj, BezierCurve.GetPoint(d), lifeTime, BulletPlayer.RotationMode.CompleteTangent);
            }
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var pos = entity.BulletShootPos();
            foreach (var d in CircleDests(pos, 3.5f, 8, FR(1) * 45f))
            {
                Tool.BattleManager.ShootBullet(entity, 1f, BezierCurve.GetPoint(d), 1.5f, lifeTime, Damageable.CD(0.3f), null);
            }
            return (pos, dest);
        }
    }
    public class Skill29 : SkillBase
    {
        private const float lifeTime = 10f;
        public override int Id => 29;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[36], pos, Quaternion.identity);
            var curve = BezierCurve.GetLine(pos, dest);
            BulletPlayer.Create(obj, curve, lifeTime, BulletPlayer.RotationMode.CompleteTangent);
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var pos = entity.transform.position;
            var curve = BezierCurve.GetLine(pos, dest);
            Tool.BattleManager.ShootBullet(entity, 1f, curve, 2f, lifeTime, Damageable.CD(0.3f), null);
            return (entity.BulletShootPos(), dest);
        }
    }
    public class Skill30 : SkillBase
    {
        private const float lifeTime = 1f;
        public override int Id => 30;
        // 目标周围随机位置 + 随机延时连续释放 12 个雷暴（伪随机，两端一致）
        private const int count = 12;
        public override void PlayVFX(Vector3 pos, Vector3 dest)
        {
            var dests = JitterCircleDests(dest, 1.5f, 1.2f, count, 0);
            for (int i = 0; i < count; i++)
            {
                float delay = FR(i) * 0.8f;
                int idx = i;
                DelayActs(delay, (pos, dests[idx]), p =>
                {
                    var obj = Object.Instantiate(Tool.AssetsManager.SkillVFX[52], p.Item1, Quaternion.identity);
                    BulletPlayer.Create(obj, BezierCurve.GetPoint(p.Item2), lifeTime, BulletPlayer.RotationMode.CompleteTangent);
                });
            }
        }
        public override (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest)
        {
            var pos = entity.BulletShootPos();
            var dests = JitterCircleDests(dest, 1.5f, 1.2f, count, 0);
            for (int i = 0; i < count; i++)
            {
                float delay = FR(i) * 0.8f;
                int idx = i;
                DelayActs(delay, (entity, pos, dests[idx]), p =>
                {
                    var curve = BezierCurve.GetPoint(p.Item3);
                    Tool.BattleManager.ShootBullet(p.Item1, 1f, curve, 1.5f, lifeTime, Damageable.Once().AvailableLifeTime(0f,0.3f), null);
                });
            }
            return (pos, dest);
        }
    }
}
