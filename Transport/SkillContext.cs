using System.Collections.Generic;
using UnityEngine;

namespace Ros.Transport
{
    public class SkillContext
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
    public struct SkillContextSerializer
    {
        public static bool Serialize(SkillContext value, byte[] result, ref int indexStart)
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

        public static SkillContext Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            var context = new SkillContext();
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
