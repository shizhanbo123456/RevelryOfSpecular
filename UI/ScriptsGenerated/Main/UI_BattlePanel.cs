/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_BattlePanel : GComponent
    {
        public Controller m_showRegenerationBar;
        public Controller m_onlook;
        public UI_SkillList m_skillList;
        public UI_PlayerHealth m_PlayerBar;
        public UI_EventList m_EventList;
        public UI_Minimap m_Minimap;
        public GTextField m_label_time_left;
        public GImage m_icon_day_night;
        public GButton m_btn_exit;
        public UI_DefensivePointBar m_progressMain;
        public UI_DefensivePointBar m_progressSub1;
        public UI_DefensivePointBar m_progressSub2;
        public UI_DefensivePointBar m_progressSub3;
        public GTextField m_attacker_member_count;
        public GTextField m_defense_member_count;
        public GTextField m_spectater_member_count;
        public GImage m_regeneration_progressbar;
        public GComboBox m_onlookComboBox;
        public const string URL = "ui://q68vr2bfjtpbhd";

        public static UI_BattlePanel CreateInstance()
        {
            return (UI_BattlePanel)UIPackage.CreateObject("Main", "BattlePanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_showRegenerationBar = GetControllerAt(0);
            m_onlook = GetControllerAt(1);
            m_skillList = (UI_SkillList)GetChildAt(0);
            m_PlayerBar = (UI_PlayerHealth)GetChildAt(1);
            m_EventList = (UI_EventList)GetChildAt(2);
            m_Minimap = (UI_Minimap)GetChildAt(3);
            m_label_time_left = (GTextField)GetChildAt(6);
            m_icon_day_night = (GImage)GetChildAt(7);
            m_btn_exit = (GButton)GetChildAt(9);
            m_progressMain = (UI_DefensivePointBar)GetChildAt(11);
            m_progressSub1 = (UI_DefensivePointBar)GetChildAt(13);
            m_progressSub2 = (UI_DefensivePointBar)GetChildAt(15);
            m_progressSub3 = (UI_DefensivePointBar)GetChildAt(17);
            m_attacker_member_count = (GTextField)GetChildAt(19);
            m_defense_member_count = (GTextField)GetChildAt(20);
            m_spectater_member_count = (GTextField)GetChildAt(21);
            m_regeneration_progressbar = (GImage)GetChildAt(27);
            m_onlookComboBox = (GComboBox)GetChildAt(31);
        }
    }
}