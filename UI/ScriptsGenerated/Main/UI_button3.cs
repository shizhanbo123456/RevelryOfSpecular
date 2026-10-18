/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_button3 : GButton
    {
        public Controller m_selected;
        public const string URL = "ui://q68vr2bfctp8jv";

        public static UI_button3 CreateInstance()
        {
            return (UI_button3)UIPackage.CreateObject("Main", "button3");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_selected = GetControllerAt(1);
        }
    }
}