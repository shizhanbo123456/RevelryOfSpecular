using System.Collections.Generic;
using Ros.Skill;
using Ros.Skill.Utils;
using Ros.Transport;
using UnityEngine;

// 跳跃散布：子弹按 skillTargets 顺序逐点穿过（链首通常放施法者当起点），每段按距离/速度匀速
public struct SkillSpreadJump
{
    public List<SkillTarget> skillTargets;
    public float bulletSpeed;

    public SkillSpreadJump(List<SkillTarget> skillTargets, float bulletSpeed)
    {
        if (skillTargets == null || skillTargets.Count < 2)
            throw new System.ArgumentException(
                $"跳跃散布至少需要 2 个点，收到 {(skillTargets == null ? 0 : skillTargets.Count)}");
        if (bulletSpeed <= 0f)
            throw new System.ArgumentException($"跳跃散布需要正的子弹速度，收到 {bulletSpeed}");
        this.skillTargets = skillTargets;
        this.bulletSpeed = bulletSpeed;
    }

    // 服务器：一条子弹走完整条链，命中判定仍由 spec 驱动
    public void ShootBullet(EntityData caster, AttackData attack, BulletHitSpec spec)
    {
        var trajectory = new Trajectory(this);
        Tool.BattleManager.ShootBullet(caster, attack, trajectory, trajectory.Duration, spec);
    }

    // 客户端：同一条弹道播特效
    public void PlayVfx(SkillVfxKind vfxKind, int vfxIndex)
    {
        VfxHelper.PlayOne(vfxKind, vfxIndex, new Trajectory(this));
    }

    private class Trajectory : BulletTrajectory
    {
        private readonly Vector3[] points;
        private readonly float[] cumulative;
        private readonly float totalLength;

        public Trajectory(SkillSpreadJump spread)
        {
            int count = spread.skillTargets.Count;
            points = new Vector3[count];
            cumulative = new float[count];
            float sum = 0f;
            for (int i = 0; i < count; i++)
            {
                points[i] = Resolve(spread.skillTargets[i]);
                if (i == 0) continue;
                sum += Vector3.Distance(points[i - 1], points[i]);
                cumulative[i] = sum;
            }
            totalLength = sum;
            if (totalLength <= 0.0001f)
                throw new System.ArgumentException("跳跃散布的链长为 0：所有点位重合，检查索敌结果");
            Duration = totalLength / spread.bulletSpeed;
        }

        public override Vector3 Lerp(float factor)
        {
            float target = Mathf.Clamp01(factor) * totalLength;
            for (int i = 1; i < points.Length; i++)
            {
                if (target > cumulative[i]) continue;
                float segment = cumulative[i] - cumulative[i - 1];
                float t = segment <= 0.0001f ? 0f : (target - cumulative[i - 1]) / segment;
                return Vector3.Lerp(points[i - 1], points[i], t);
            }
            return points[points.Length - 1];
        }

        // id 优先取实时位置（跟随移动），查不到才用包里冻结点
        private static Vector3 Resolve(SkillTarget target)
        {
            return BulletTrajectory.TryGetEntityPosition(target.id, out var pos) ? pos : target.pos;
        }
    }
}
