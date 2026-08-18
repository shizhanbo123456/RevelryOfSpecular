using UnityEngine;

namespace Ros.Transport
{
    public struct TextValueInfo
    {
        public ushort value;
        public TextColor color;
        public Vector3 pos;
    }
    public struct TextLabelInfo
    {
        public string label;
        public TextColor color;
        public Vector3 pos;
    }
    public struct UseSkillInfo
    {
        public int id;
        public Vector3 pos;
        public Vector3 dest;
    }

    public struct TextValueInfoSerializer
    {
        public static bool Serialize(TextValueInfo value, byte[] result, ref int indexStart)
        {
            return UshortSerializer.Serialize(value.value, result, ref indexStart) &&
                   IntSerializer.Serialize((int)value.color, result, ref indexStart) &&
                   Vector3Serializer.Serialize(value.pos, result, ref indexStart);
        }

        public static TextValueInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new TextValueInfo
            {
                value = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex),
                color = (TextColor)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                pos = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex)
            };
        }
    }

    public struct TextLabelInfoSerializer
    {
        public static bool Serialize(TextLabelInfo value, byte[] result, ref int indexStart)
        {
            return StringSerializer.Serialize(value.label, result, ref indexStart) &&
                   IntSerializer.Serialize((int)value.color, result, ref indexStart) &&
                   Vector3Serializer.Serialize(value.pos, result, ref indexStart);
        }

        public static TextLabelInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new TextLabelInfo
            {
                label = StringSerializer.Deserialize(data, ref indexStart, invalidIndex),
                color = (TextColor)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                pos = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex)
            };
        }
    }

    public struct UseSkillInfoSerializer
    {
        public static bool Serialize(UseSkillInfo value, byte[] result, ref int indexStart)
        {
            return IntSerializer.Serialize(value.id, result, ref indexStart) &&
                   Vector3Serializer.Serialize(value.pos, result, ref indexStart) &&
                   Vector3Serializer.Serialize(value.dest, result, ref indexStart);
        }

        public static UseSkillInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new UseSkillInfo
            {
                id = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                pos = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex),
                dest = Vector3Serializer.Deserialize(data, ref indexStart, invalidIndex)
            };
        }
    }
}
