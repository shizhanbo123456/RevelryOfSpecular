/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_BattlePanel : GComponent
    {
        public Controller m_showRegenerationBar;
        public UI_SkillList m_skillList;
        public UI_PlayerHealth m_PlayerBar;
        public GComponent m_EventList;
        public UI_Minimap m_Minimap;
        public GImage m_icon_day_night;
        public GTextField m_label_time_left;
        public GImage m_regeneration_progressbar;
        public UI_DefensivePointBar m_progressMain;
        public UI_DefensivePointBar m_progressSub1;
        public UI_DefensivePointBar m_progressSub2;
        public UI_DefensivePointBar m_progressSub3;
        public GButton m_btn_exit;
        public const string URL = "ui://q68vr2bfjtpbhd";

        public static UI_BattlePanel CreateInstance()
        {
            return (UI_BattlePanel)UIPackage.CreateObject("Main", "BattlePanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_showRegenerationBar = GetControllerAt(0);
            m_skillList = (UI_SkillList)GetChildAt(0);
            m_PlayerBar = (UI_PlayerHealth)GetChildAt(1);
            m_EventList = (GComponent)GetChildAt(2);
            m_Minimap = (UI_Minimap)GetChildAt(3);
            m_icon_day_night = (GImage)GetChildAt(5);
            m_label_time_left = (GTextField)GetChildAt(8);
            m_regeneration_progressbar = (GImage)GetChildAt(11);
            m_progressMain = (UI_DefensivePointBar)GetChildAt(15);
            m_progressSub1 = (UI_DefensivePointBar)GetChildAt(16);
            m_progressSub2 = (UI_DefensivePointBar)GetChildAt(17);
            m_progressSub3 = (UI_DefensivePointBar)GetChildAt(18);
            m_btn_exit = (GButton)GetChildAt(20);
        }
    }
}