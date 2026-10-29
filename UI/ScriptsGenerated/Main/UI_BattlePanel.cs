/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_BattlePanel : GComponent
    {
        public Controller m_showRegenerationBar;
        public Controller m_onlook;
        public UI_empty_container m_world_element_container;
        public UI_SkillList m_skillList;
        public UI_PlayerHealth m_PlayerBar;
        public UI_Minimap m_Minimap;
        public UI_EventBanner m_eventBanner;
        public GImage m_regeneration_progressbar;
        public GComboBox m_onlookComboBox;
        public UI_BattleBarPanel m_barPanel;
        public UI_BattleInfoPanel m_batleInfoPanel;
        public UI_SettingsPanel m_settings_panel;
        public UI_ImportantEventBanner m_important_event_banner;
        public Transition m_showBanner;
        public Transition m_showHit;
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
            m_world_element_container = (UI_empty_container)GetChildAt(1);
            m_skillList = (UI_SkillList)GetChildAt(2);
            m_PlayerBar = (UI_PlayerHealth)GetChildAt(3);
            m_Minimap = (UI_Minimap)GetChildAt(4);
            m_eventBanner = (UI_EventBanner)GetChildAt(5);
            m_regeneration_progressbar = (GImage)GetChildAt(7);
            m_onlookComboBox = (GComboBox)GetChildAt(11);
            m_barPanel = (UI_BattleBarPanel)GetChildAt(13);
            m_batleInfoPanel = (UI_BattleInfoPanel)GetChildAt(14);
            m_settings_panel = (UI_SettingsPanel)GetChildAt(15);
            m_important_event_banner = (UI_ImportantEventBanner)GetChildAt(16);
            m_showBanner = GetTransitionAt(0);
            m_showHit = GetTransitionAt(1);
        }
    }
}