/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_LobbyMemberList : GComponent
    {
        public UI_Panel_1 m_panel;
        public GList m_attackerPlayers;
        public UI_Button1 m_btn_joinAttacker;
        public UI_Button1 m_attackerAI_minus;
        public GTextField m_label_attackerAI_count;
        public UI_Button1 m_attackerAI_add;
        public GList m_defenserPlayers;
        public UI_Button1 m_btn_joinDefenser;
        public UI_Button1 m_defenserAI_minus;
        public GTextField m_label_defenserAI_count;
        public UI_Button1 m_defenserAI_add;
        public GList m_spectatePlayers;
        public UI_Button1 m_btn_joinSpectate;
        public GButton m_btn_prepare;
        public const string URL = "ui://q68vr2bftwqeh9";

        public static UI_LobbyMemberList CreateInstance()
        {
            return (UI_LobbyMemberList)UIPackage.CreateObject("Main", "LobbyMemberList");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_panel = (UI_Panel_1)GetChildAt(0);
            m_attackerPlayers = (GList)GetChildAt(3);
            m_btn_joinAttacker = (UI_Button1)GetChildAt(4);
            m_attackerAI_minus = (UI_Button1)GetChildAt(6);
            m_label_attackerAI_count = (GTextField)GetChildAt(7);
            m_attackerAI_add = (UI_Button1)GetChildAt(8);
            m_defenserPlayers = (GList)GetChildAt(11);
            m_btn_joinDefenser = (UI_Button1)GetChildAt(12);
            m_defenserAI_minus = (UI_Button1)GetChildAt(14);
            m_label_defenserAI_count = (GTextField)GetChildAt(15);
            m_defenserAI_add = (UI_Button1)GetChildAt(16);
            m_spectatePlayers = (GList)GetChildAt(18);
            m_btn_joinSpectate = (UI_Button1)GetChildAt(20);
            m_btn_prepare = (GButton)GetChildAt(21);
        }
    }
}