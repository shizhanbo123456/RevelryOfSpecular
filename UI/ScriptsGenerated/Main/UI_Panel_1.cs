/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_Panel_1 : GComponent
    {
        public Controller m_hideTitle;
        public const string URL = "ui://q68vr2bftwqeh4";

        public static UI_Panel_1 CreateInstance()
        {
            return (UI_Panel_1)UIPackage.CreateObject("Main", "Panel_1");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_hideTitle = GetControllerAt(0);
        }
    }
}