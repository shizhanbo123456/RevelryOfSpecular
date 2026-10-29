/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_BattleInfoPanel : GComponent
    {
        public GImage m_icon_day_night;
        public GTextField m_label_time_left;
        public GTextField m_defense_member_count;
        public GTextField m_attacker_member_count;
        public GTextField m_spectater_member_count;
        public GButton m_btn_setting;
        public const string URL = "ui://q68vr2bfiq0omi";

        public static UI_BattleInfoPanel CreateInstance()
        {
            return (UI_BattleInfoPanel)UIPackage.CreateObject("Main", "BattleInfoPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_icon_day_night = (GImage)GetChildAt(2);
            m_label_time_left = (GTextField)GetChildAt(5);
            m_defense_member_count = (GTextField)GetChildAt(6);
            m_attacker_member_count = (GTextField)GetChildAt(7);
            m_spectater_member_count = (GTextField)GetChildAt(8);
            m_btn_setting = (GButton)GetChildAt(12);
        }
    }
}