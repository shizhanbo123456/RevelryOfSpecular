using System.Collections.Generic;
using UnityEngine;

public static class RandomMapper
{
    private static Dictionary<int,double>buffer=new Dictionary<int,double>();
    public static int Random(int value,int min,int max)
    {
        if (min > max)
        {
            Debug.LogWarning($"范围大小错误:{min} {max}");
            return value;
        }
        if (min == max) return min;
        double ratio;
        if (!buffer.TryGetValue(value,out ratio))
        {
            long hash = value * 2654435761L ^ (value << 13);
            ratio = (double)(ulong)hash / ulong.MaxValue;
            buffer.Add(value, ratio);
        }
        int range=max- min;
        int res = min + (int)(ratio * range);
        return Mathf.Clamp(res, min, max);
    }
}