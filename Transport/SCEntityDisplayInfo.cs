using System;
using UnityEngine;

namespace Ros.Transport
{
    public struct SCEntityDisplayInfo
    {
        public ushort id;
        public EntityType type;
        public int level;

        public float x;
        public float y;
        public float z;
        public float yaw;
        public float speed;
        public float angularSpeed;
        public int health;
        public int maxHealth;

        public EntityEffectController.EffectGraphic effectGraphic;
    }

    public struct SCEntityDisplayInfoSerializer
    {
        public static bool Serialize(SCEntityDisplayInfo value, byte[] result, ref int indexStart)
        {
            return UshortSerializer.Serialize(value.id, result, ref indexStart) &&
                   EntityTypeSerializer.Serialize(value.type, result, ref indexStart) &&
                   IntSerializer.Serialize(value.level, result, ref indexStart) &&
                   FloatSerializer.Serialize(value.x, result, ref indexStart) &&
                   FloatSerializer.Serialize(value.y, result, ref indexStart) &&
                   FloatSerializer.Serialize(value.z, result, ref indexStart) &&
                   FloatSerializer.Serialize(value.yaw, result, ref indexStart) &&
                   FloatSerializer.Serialize(value.speed, result, ref indexStart) &&
                   FloatSerializer.Serialize(value.angularSpeed, result, ref indexStart) &&
                   IntSerializer.Serialize(value.health, result, ref indexStart) &&
                   IntSerializer.Serialize(value.maxHealth, result, ref indexStart) &&
                   IntSerializer.Serialize((int)value.effectGraphic, result, ref indexStart);
        }

        public static SCEntityDisplayInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new SCEntityDisplayInfo
            {
                id = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                type = EntityTypeSerializer.Deserialize(data, ref indexStart, invalidIndex),
                level = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                x = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                y = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                z = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                yaw = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                speed = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                angularSpeed = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                health = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                maxHealth = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                effectGraphic = (EntityEffectController.EffectGraphic)IntSerializer.Deserialize(data, ref indexStart, invalidIndex)
            };
        }
    }
}
