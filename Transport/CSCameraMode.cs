namespace Ros.Transport
{
    // 客户端 → 服务器：同步本地相机是否处于俯视模式（俯视下移动语义为四向，服务器须按同一口径解释 WASD）
    public class CSCameraMode
    {
        public bool topDown;
    }

    public struct CSCameraModeSerializer
    {
        public static bool Serialize(CSCameraMode value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            return BoolSerializer.Serialize(value.topDown, result, ref indexStart);
        }

        public static CSCameraMode Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSCameraMode()
            {
                topDown = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
