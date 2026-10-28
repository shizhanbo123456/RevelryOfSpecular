/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_BuffTip : GComponent
    {
        public GTextField m_buff_name;
        public GTextField m_buff_des;
        public const string URL = "ui://q68vr2bfu3iymc";

        public static UI_BuffTip CreateInstance()
        {
            return (UI_BuffTip)UIPackage.CreateObject("Main", "BuffTip");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_buff_name = (GTextField)GetChildAt(1);
            m_buff_des = (GTextField)GetChildAt(2);
        }
    }
}