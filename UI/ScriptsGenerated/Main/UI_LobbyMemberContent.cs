/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_LobbyMemberContent : GComponent
    {
        public Controller m_spectate;
        public Controller m_is_in_this_camp;
        public UI_PlayerCampIcon m_player_camp;
        public GTextField m_camp_name;
        public GList m_attackerPlayers;
        public GTextField m_label_AI_count;
        public GButton m_add_ai;
        public GButton m_remove_ai;
        public GButton m_btn_join;
        public GTextField m_spectate_current_member_count;
        public const string URL = "ui://q68vr2bflu0bls";

        public static UI_LobbyMemberContent CreateInstance()
        {
            return (UI_LobbyMemberContent)UIPackage.CreateObject("Main", "LobbyMemberContent");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_spectate = GetControllerAt(0);
            m_is_in_this_camp = GetControllerAt(1);
            m_player_camp = (UI_PlayerCampIcon)GetChildAt(0);
            m_camp_name = (GTextField)GetChildAt(1);
            m_attackerPlayers = (GList)GetChildAt(2);
            m_label_AI_count = (GTextField)GetChildAt(4);
            m_add_ai = (GButton)GetChildAt(5);
            m_remove_ai = (GButton)GetChildAt(6);
            m_btn_join = (GButton)GetChildAt(8);
            m_spectate_current_member_count = (GTextField)GetChildAt(11);
        }
    }
}