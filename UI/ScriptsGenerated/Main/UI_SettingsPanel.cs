/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_SettingsPanel : GComponent
    {
        public UI_panel_2 m_panrl;
        public GButton m_btn_exit;
        public GList m_button_list;
        public GList m_content;
        public const string URL = "ui://q68vr2bfiq0omn";

        public static UI_SettingsPanel CreateInstance()
        {
            return (UI_SettingsPanel)UIPackage.CreateObject("Main", "SettingsPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_panrl = (UI_panel_2)GetChildAt(0);
            m_btn_exit = (GButton)GetChildAt(1);
            m_button_list = (GList)GetChildAt(2);
            m_content = (GList)GetChildAt(3);
        }
    }
}