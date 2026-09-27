/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_BattlePanel : GComponent
    {
        public GComponent m_skillList;
        public UI_PlayerHealth m_PlayerBar;
        public GComponent m_EventList;
        public UI_Minimap m_Minimap;
        public GImage m_icon_day_night;
        public GTextField m_label_time_left;
        public const string URL = "ui://q68vr2bfjtpbhd";

        public static UI_BattlePanel CreateInstance()
        {
            return (UI_BattlePanel)UIPackage.CreateObject("Main", "BattlePanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_skillList = (GComponent)GetChildAt(0);
            m_PlayerBar = (UI_PlayerHealth)GetChildAt(1);
            m_EventList = (GComponent)GetChildAt(2);
            m_Minimap = (UI_Minimap)GetChildAt(3);
            m_icon_day_night = (GImage)GetChildAt(5);
            m_label_time_left = (GTextField)GetChildAt(8);
        }
    }
}