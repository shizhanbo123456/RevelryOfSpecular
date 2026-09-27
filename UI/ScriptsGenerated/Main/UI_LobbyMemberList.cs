/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_LobbyMemberList : GComponent
    {
        public UI_Panel_1 m_panel;
        public GList m_defenserPlayers;
        public UI_Button1 m_btn_joinAttacker;
        public GList m_attackerPlayers;
        public UI_Button1 m_btn_joinDefenser;
        public UI_Button1 m_btn_battleStart;
        public const string URL = "ui://q68vr2bftwqeh9";

        public static UI_LobbyMemberList CreateInstance()
        {
            return (UI_LobbyMemberList)UIPackage.CreateObject("Main", "LobbyMemberList");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_panel = (UI_Panel_1)GetChildAt(0);
            m_defenserPlayers = (GList)GetChildAt(3);
            m_btn_joinAttacker = (UI_Button1)GetChildAt(4);
            m_attackerPlayers = (GList)GetChildAt(5);
            m_btn_joinDefenser = (UI_Button1)GetChildAt(6);
            m_btn_battleStart = (UI_Button1)GetChildAt(15);
        }
    }
}