/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_EntityBar : GComponent
    {
        public Controller m_player_camp;
        public Controller m_is_enemy;
        public GImage m_bar_fill_health_partner;
        public GImage m_bar_fill_health_enemy;
        public GTextField m_label_health;
        public UI_buffList m_buff_list;
        public GTextField m_entity_name;
        public UI_PlayerCampIcon m_campIcon;
        public const string URL = "ui://q68vr2bfjtpbia";

        public static UI_EntityBar CreateInstance()
        {
            return (UI_EntityBar)UIPackage.CreateObject("Main", "EntityBar");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_player_camp = GetControllerAt(0);
            m_is_enemy = GetControllerAt(1);
            m_bar_fill_health_partner = (GImage)GetChildAt(1);
            m_bar_fill_health_enemy = (GImage)GetChildAt(2);
            m_label_health = (GTextField)GetChildAt(3);
            m_buff_list = (UI_buffList)GetChildAt(4);
            m_entity_name = (GTextField)GetChildAt(5);
            m_campIcon = (UI_PlayerCampIcon)GetChildAt(6);
        }
    }
}