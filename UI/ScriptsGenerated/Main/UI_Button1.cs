/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_Button1 : GButton
    {
        public Controller m_selected;
        public const string URL = "ui://q68vr2bfvjeqgw";

        public static UI_Button1 CreateInstance()
        {
            return (UI_Button1)UIPackage.CreateObject("Main", "Button1");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_selected = GetControllerAt(1);
        }
    }
}