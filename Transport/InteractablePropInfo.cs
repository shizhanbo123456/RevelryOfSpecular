public struct InteractablePropInfo
{
    public enum CharacterUnlockProp
    {
        None = 0,
        Character0 = 1 << 0,
        Character1 = 1 << 1,
        Character2 = 1 << 2,
        Character3 = 1 << 3,
        Character4 = 1 << 4,
        Character5 = 1 << 5,
        Character6 = 1 << 6,
        Character7 = 1 << 7,
        Character8 = 1 << 8,
        Character9 = 1 << 9,
        Character10 = 1 << 10,
        Character11 = 1 << 11,
        Character12 = 1 << 12,
        Character13 = 1 << 13,
    }
    public enum ExitPort
    {
        None = 0,
        Index0 = 1 << 0,
        Index1 = 1 << 1,
        Index2 = 1 << 2,
        Index3 = 1 << 3,
        Index4 = 1 << 4,
        Index5 = 1 << 5,
        Index6 = 1 << 6,
        Index7 = 1 << 7,
        Index8 = 1 << 8,
        Index9 = 1 << 9,
        Index10 = 1 << 10,
        Index11 = 1 << 11,
        Index12 = 1 << 12,
        Index13 = 1 << 13,
        Index14 = 1 << 14,
        Index15 = 1 << 15,
    }

    public CharacterUnlockProp characterUnlockProp;
    public ExitPort maxExitPort;
    public ExitPort normalExitPort;
    public ExitPort minExitPort;
}

public struct InteractablePropInfoSerializer
{
    public static bool Serialize(InteractablePropInfo value, byte[] result, ref int indexStart)
    {
        return IntSerializer.Serialize((int)value.characterUnlockProp, result, ref indexStart) &&
               IntSerializer.Serialize((int)value.maxExitPort, result, ref indexStart) &&
               IntSerializer.Serialize((int)value.normalExitPort, result, ref indexStart) &&
               IntSerializer.Serialize((int)value.minExitPort, result, ref indexStart);
    }

    public static InteractablePropInfo Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        return new InteractablePropInfo
        {
            characterUnlockProp = (InteractablePropInfo.CharacterUnlockProp)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            maxExitPort = (InteractablePropInfo.ExitPort)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            normalExitPort = (InteractablePropInfo.ExitPort)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            minExitPort = (InteractablePropInfo.ExitPort)IntSerializer.Deserialize(data, ref indexStart, invalidIndex)
        };
    }
}
