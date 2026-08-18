using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ros.Transport
{
    public class NetworkEvent
    {
        public const byte KillPlayer = 0;
        public const byte KillZombie = 1;
        public const byte KillPlant = 2;
        public const byte KillInfectedPlant = 3;
        public const byte KillOre = 4;
        public const byte KillInfectedOre = 5;
        public const byte KillInfection = 6;

        //事件类型->事件值
        public Dictionary<byte,byte>type=new();
    }

    public struct NetworkEventSerializer
    {
        public static bool Serialize(NetworkEvent value, byte[] result, ref int indexStart)
        {
            bool hasValue = value != null;
            if (!BoolSerializer.Serialize(hasValue, result, ref indexStart)) return false;
            if (!hasValue) return true;

            int count = value.type == null ? 0 : value.type.Count;
            if (!IntSerializer.Serialize(count, result, ref indexStart)) return false;
            if (value.type == null) return true;

            foreach (var pair in value.type)
            {
                if (!ByteSerializer.Serialize(pair.Key, result, ref indexStart) ||
                    !ByteSerializer.Serialize(pair.Value, result, ref indexStart))
                {
                    return false;
                }
            }
            return true;
        }

        public static NetworkEvent Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            bool hasValue = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (!hasValue) return null;

            NetworkEvent value = new();
            int count = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            for (int i = 0; i < count; i++)
            {
                byte key = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
                byte eventValue = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
                value.type[key] = eventValue;
            }
            return value;
        }
    }
}
