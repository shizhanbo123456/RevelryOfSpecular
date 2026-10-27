/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_panel_2 : GLabel
    {
        public Controller m_hide_title;
        public Controller m_show_close;
        public GButton m_close;
        public const string URL = "ui://q68vr2bfctp8jr";

        public static UI_panel_2 CreateInstance()
        {
            return (UI_panel_2)UIPackage.CreateObject("Main", "panel_2");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_hide_title = GetControllerAt(0);
            m_show_close = GetControllerAt(1);
            m_close = (GButton)GetChildAt(3);
        }
    }
}