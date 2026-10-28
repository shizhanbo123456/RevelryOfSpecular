/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_GmPanel : GComponent
    {
        public UI_panel_2 m_panel;
        public UI_GmButtonList m_firstMenu;
        public UI_GmButtonList m_secondMenu;
        public GList m_GmItemList;
        public const string URL = "ui://q68vr2bftjktio";

        public static UI_GmPanel CreateInstance()
        {
            return (UI_GmPanel)UIPackage.CreateObject("Main", "GmPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_panel = (UI_panel_2)GetChildAt(0);
            m_firstMenu = (UI_GmButtonList)GetChildAt(1);
            m_secondMenu = (UI_GmButtonList)GetChildAt(2);
            m_GmItemList = (GList)GetChildAt(5);
        }
    }
}