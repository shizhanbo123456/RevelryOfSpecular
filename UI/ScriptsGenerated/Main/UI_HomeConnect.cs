/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_HomeConnect : GComponent
    {
        public UI_InputField m_input_ipaddress;
        public UI_Button1 m_btn_connect;
        public const string URL = "ui://q68vr2bfjtpbhb";

        public static UI_HomeConnect CreateInstance()
        {
            return (UI_HomeConnect)UIPackage.CreateObject("Main", "HomeConnect");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_input_ipaddress = (UI_InputField)GetChildAt(1);
            m_btn_connect = (UI_Button1)GetChildAt(3);
        }
    }
}