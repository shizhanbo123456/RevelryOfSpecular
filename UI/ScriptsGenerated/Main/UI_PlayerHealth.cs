/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_PlayerHealth : GComponent
    {
        public GImage m_bar_fill_health;
        public GTextField m_label_health;
        public UI_avaterMask m_avater_mask;
        public UI_buffList m_buffList;
        public const string URL = "ui://q68vr2bfjtpbhg";

        public static UI_PlayerHealth CreateInstance()
        {
            return (UI_PlayerHealth)UIPackage.CreateObject("Main", "PlayerHealth");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_bar_fill_health = (GImage)GetChildAt(1);
            m_label_health = (GTextField)GetChildAt(2);
            m_avater_mask = (UI_avaterMask)GetChildAt(4);
            m_buffList = (UI_buffList)GetChildAt(6);
        }
    }
}