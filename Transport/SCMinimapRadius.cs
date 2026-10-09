namespace Ros.Transport
{
    // 服务器 → 客户端：回应切换后的雷达显示半径（客户端据此调整裁剪与缩放）
    public class SCMinimapRadius
    {
        public float radius;
    }

    public struct SCMinimapRadiusSerializer
    {
        public static bool Serialize(SCMinimapRadius value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            return FloatSerializer.Serialize(value.radius, result, ref indexStart);
        }

        public static SCMinimapRadius Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCMinimapRadius()
            {
                radius = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
