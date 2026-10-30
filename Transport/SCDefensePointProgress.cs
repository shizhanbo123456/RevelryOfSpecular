namespace Ros.Transport
{
    // 服务器 → 客户端：四个守护点的开采进度（0~100，被打掉的血量百分比），与实体可见性无关，值变更时下发
    public class SCDefensePointProgress
    {
        public int main;
        public int sub1;
        public int sub2;
        public int sub3;
    }

    public struct SCDefensePointProgressSerializer
    {
        public static bool Serialize(SCDefensePointProgress value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize(value.main, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.sub1, result, ref indexStart)) return false;
            if (!IntSerializer.Serialize(value.sub2, result, ref indexStart)) return false;
            return IntSerializer.Serialize(value.sub3, result, ref indexStart);
        }

        public static SCDefensePointProgress Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCDefensePointProgress()
            {
                main = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                sub1 = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                sub2 = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                sub3 = IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
