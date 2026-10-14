using System;

namespace Ros.Gm
{
    [Flags]
    public enum GmStatusType
    {
        Offline=1,
        Lobby=2,
        Battle=4
    }
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class GmStatusAttribute : Attribute//不加此项默认仅仅战斗
    {
        public GmStatusType status;
        public GmStatusAttribute(GmStatusType status= GmStatusType.Battle)
        {
            this.status = status;
        }
    }
}