using System;

[Serializable]
public struct CSInputCommand
{
    public bool moving;
    public float yaw;

    public CSInputCommand(bool moving, float yaw)
    {
        this.moving = moving;
        this.yaw = yaw;
    }
}

public struct CSInputCommandSerializer
{
    public static bool Serialize(CSInputCommand value, byte[] result, ref int indexStart)
    {
        return BoolSerializer.Serialize(value.moving, result, ref indexStart) &&
               FloatSerializer.Serialize(value.yaw, result, ref indexStart);
    }

    public static CSInputCommand Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        return new CSInputCommand(
            BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            FloatSerializer.Deserialize(data, ref indexStart, invalidIndex));
    }
}
