using System.Collections.Generic;
using UnityEngine;

namespace Ros.Transport
{
    public class SkillContext
    {
        public List<int> ints = new();
        public List<Vector3> vectors = new();

        public void AddInts(params int[] values)
        {
            foreach (var v in values) ints.Add(v);
        }

        public void AddVectors(params Vector3[] values)
        {
            foreach (var v in values) vectors.Add(v);
        }
    }

    public struct SkillContextSerializer
    {
        public static bool Serialize(SkillContext value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;

            int intCount = value.ints != null ? value.ints.Count : 0;
            if (!IntSerializer.Serialize(intCount, result, ref indexStart)) return false;
            if (value.ints != null)
            {
                foreach (var v in value.ints)
                {
                    if (!IntSerializer.Serialize(v, result, ref indexStart)) return false;
                }
            }

            int vectorCount = value.vectors != null ? value.vectors.Count : 0;
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
