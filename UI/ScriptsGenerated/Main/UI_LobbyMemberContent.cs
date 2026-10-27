/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_LobbyMemberContent : GComponent
    {
        public Controller m_camp;
        public Controller m_hide_ai_member_operation;
        public GList m_attackerPlayers;
        public GTextField m_label_AI_count;
        public GButton m_btn_join;
        public const string URL = "ui://q68vr2bflu0bls";

        public static UI_LobbyMemberContent CreateInstance()
        {
            return (UI_LobbyMemberContent)UIPackage.CreateObject("Main", "LobbyMemberContent");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_camp = GetControllerAt(0);
            m_hide_ai_member_operation = GetControllerAt(1);
            m_attackerPlayers = (GList)GetChildAt(4);
            m_label_AI_count = (GTextField)GetChildAt(6);
            m_btn_join = (GButton)GetChildAt(10);
        }
    }
}