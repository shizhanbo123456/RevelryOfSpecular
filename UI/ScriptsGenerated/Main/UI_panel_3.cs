/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_panel_3 : GLabel
    {
        public Controller m_hide_title;
        public Controller m_show_close;
        public const string URL = "ui://q68vr2bfu158nl";

        public static UI_panel_3 CreateInstance()
        {
            return (UI_panel_3)UIPackage.CreateObject("Main", "panel_3");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_hide_title = GetControllerAt(0);
            m_show_close = GetControllerAt(1);
        }
    }
}