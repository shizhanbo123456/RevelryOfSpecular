/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_LobbyMemberList : GComponent
    {
        public UI_panel_2 m_panel;
        public UI_LobbyMemberContent m_camp_attack;
        public UI_LobbyMemberContent m_camp_defense;
        public UI_LobbyMemberContent m_camp_spectate;
        public GButton m_btn_prepare;
        public const string URL = "ui://q68vr2bftwqeh9";

        public static UI_LobbyMemberList CreateInstance()
        {
            return (UI_LobbyMemberList)UIPackage.CreateObject("Main", "LobbyMemberList");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_panel = (UI_panel_2)GetChildAt(0);
            m_camp_attack = (UI_LobbyMemberContent)GetChildAt(1);
            m_camp_defense = (UI_LobbyMemberContent)GetChildAt(2);
            m_camp_spectate = (UI_LobbyMemberContent)GetChildAt(3);
            m_btn_prepare = (GButton)GetChildAt(4);
        }
    }
}