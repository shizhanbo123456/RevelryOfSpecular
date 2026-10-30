namespace Ros.Transport
{
    // 客户端 → 服务器：观战自由视角相机位置（仅纯观战方上报）。free=false 表示已切回跟随实体，位置字段无效
    public class CSFreeCamera
    {
        public bool free;
        public float posX;
        public float posZ;
    }

    public struct CSFreeCameraSerializer
    {
        public static bool Serialize(CSFreeCamera value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!BoolSerializer.Serialize(value.free, result, ref indexStart)) return false;
            if (!FloatSerializer.Serialize(value.posX, result, ref indexStart)) return false;
            return FloatSerializer.Serialize(value.posZ, result, ref indexStart);
        }

        public static CSFreeCamera Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSFreeCamera()
            {
                free = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
                posX = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
                posZ = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
