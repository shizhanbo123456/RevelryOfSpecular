namespace Ros.Transport
{
    // 服务器 → 客户端：开始加载（只用于切到加载界面；实体 id 等预处理结果走 SCLoadingPrepared）
    public class SCLoadingStart
    {
        public EntityCamp camp; // Attack / Defense / None（None = 观战方）
        public bool resync;     // true = 从零补发（断线重连 / 对局中途进入，客户端需清空视图、技能库存按零起算）
    }

    public struct SCLoadingStartSerializer
    {
        public static bool Serialize(SCLoadingStart value, byte[] result, ref int indexStart)
        {
            if (!BoolSerializer.Serialize(value != null, result, ref indexStart)) return false;
            if (value == null) return true;
            if (!IntSerializer.Serialize((int)value.camp, result, ref indexStart)) return false;
            return BoolSerializer.Serialize(value.resync, result, ref indexStart);
        }

        public static SCLoadingStart Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (!BoolSerializer.Deserialize(data, ref indexStart, invalidIndex)) return null;
            return new SCLoadingStart()
            {
                camp = (EntityCamp)IntSerializer.Deserialize(data, ref indexStart, invalidIndex),
                resync = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex),
            };
        }
    }
}
