using UnityEngine;

// 折线：按弧长匀速走完给定点序列（弹跳链等"释放时就算好全部落点"的技能用它）
public class PolylineTrajectory : BulletTrajectory
{
    private readonly Vector3[] points;
    private readonly float[] cumulative;
    private readonly float totalLength;

    public PolylineTrajectory(Vector3[] points)
    {
        if (points == null || points.Length < 2) throw new System.ArgumentException("折线至少需要 2 个点");
        this.points = points;
        cumulative = new float[points.Length];
        float sum = 0f;
        for (int i = 1; i < points.Length; i++)
        {
            sum += Vector3.Distance(points[i - 1], points[i]);
            cumulative[i] = sum;
        }
        totalLength = sum;
    }

    public override Vector3 Lerp(float factor)
    {
        if (totalLength <= 0.0001f) return points[0];
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
}
