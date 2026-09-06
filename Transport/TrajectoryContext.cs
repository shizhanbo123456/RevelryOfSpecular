using System.Collections.Generic;
using UnityEngine;

namespace Ros.Transport
{
    /// <summary>
    /// 轨迹上下文：技能释放时由服务器计算并填装，随"使用技能"RPC（技能 id + 上下文）发往客户端。
    /// 【重要】ints 与 vectors 完全无任何具体含义，实际使用时由技能任意填充；
    /// 每个上下文只服务于一次技能释放；技能涉及多种轨迹时，各轨迹的构建函数分别读取自己在上下文中的不同下标段。
    /// 服务器用它构建轨迹做逻辑判定，客户端用同一套构建函数重建轨迹播放表现，确保双方显示逻辑相同。
    /// </summary>
    public class TrajectoryContext
    {
        /// <summary>整型参数（任意含义，由技能自定义）。</summary>
        public List<int> ints = new();
        /// <summary>向量参数（任意含义，由技能自定义）。</summary>
        public List<Vector3> vectors = new();

        /// <summary>追加多个整型参数。</summary>
        public void AddInts(params int[] values)
        {
            foreach (var v in values) ints.Add(v);
        }

        /// <summary>追加多个向量参数。</summary>
        public void AddVectors(params Vector3[] values)
        {
            foreach (var v in values) vectors.Add(v);
        }
    }

    /// <summary>TrajectoryContext 网络序列化器。</summary>
    public struct TrajectoryContextSerializer
    {
        public static bool Serialize(TrajectoryContext value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;

            int intCount = value.ints?.Count ?? 0;
            if (!IntSerializer.Serialize(intCount, result, ref indexStart)) return false;
            if (value.ints != null)
            {
                foreach (var v in value.ints)
                {
                    if (!IntSerializer.Serialize(v, result, ref indexStart)) return false;
                }
            }

            int vectorCount = value.vectors?.Count ?? 0;
            if (!IntSerializer.Serialize(vectorCount, result, ref indexStart)) return false;
            if (value.vectors != null)
            {
                foreach (var v in value.vectors)
                {
                    if (!Vector3Serializer.Serialize(v, result, ref indexStart)) return false;
                }
            }
            return true;
        }

        public static TrajectoryContext Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var context = new TrajectoryContext();
            int intCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < intCount; i++)
            {
                context.ints.Add(IntSerializer.Deserialize(data, ref indexStart, invalidIndex));
            }
            int vectorCount = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < vectorCount; i++)
            {
                context.vectors.Add(Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex));
            }
            return context;
        }
    }
}
