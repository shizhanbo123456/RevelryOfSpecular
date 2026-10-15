using UnityEngine;

public class ChainTrajectory : BulletTrajectory
{
    private readonly ushort casterId;
    private readonly ushort firstId;
    private readonly ushort secondId;
    private Vector3 lastFirst = Vector3.zero;
    private Vector3 lastSecond = Vector3.zero;

    public ChainTrajectory(ushort casterId, ushort firstId, ushort secondId)
    {
        this.casterId = casterId;
        this.firstId = firstId;
        this.secondId = secondId;
    }

    public override Vector3 Lerp(float factor)
    {
        if (TryGetEntityPosition(firstId, out var first)) lastFirst = first;
        if (TryGetEntityPosition(secondId, out var second)) lastSecond = second;
        if (factor < 0.5f)
        {
            Vector3 from = lastFirst;
            if (TryGetEntityPosition(casterId, out var caster)) from = caster;
            return Vector3.Lerp(from, lastFirst, factor * 2f);
        }
        return Vector3.Lerp(lastFirst, lastSecond, (factor - 0.5f) * 2f);
    }
}
