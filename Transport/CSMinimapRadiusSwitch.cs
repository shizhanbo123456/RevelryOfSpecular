namespace Ros.Transport
{
    // 客户端 → 服务器：请求切换雷达显示半径（100/200/300 循环），无载荷
    public class CSMinimapRadiusSwitch
    {
    }

    public struct CSMinimapRadiusSwitchSerializer
    {
        // 空包：无内容，不占字节，与位传输开关无关
        public static bool Serialize(CSMinimapRadiusSwitch value, byte[] result, ref int indexStart)
        {
            return true;
        }

        public static CSMinimapRadiusSwitch Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new CSMinimapRadiusSwitch();
        }
    }
}
