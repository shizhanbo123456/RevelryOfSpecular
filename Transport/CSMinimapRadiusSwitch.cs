namespace Ros.Transport
{
    // 客户端 → 服务器：请求切换雷达显示半径（100/200/300 循环），无载荷
    public class CSMinimapRadiusSwitch
    {
    }

    public struct CSMinimapRadiusSwitchSerializer
    {
        public static bool Serialize(CSMinimapRadiusSwitch value, byte[] result, ref int indexStart)
        {
            return BoolSerializer.Serialize(value != null, result, ref indexStart);
        }

        public static CSMinimapRadiusSwitch Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new CSMinimapRadiusSwitch();
        }
    }
}
