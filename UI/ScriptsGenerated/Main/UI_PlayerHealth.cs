/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_PlayerHealth : GComponent
    {
        public GGraph m_bar_fill;
        public GTextField m_label_level;
        public GTextField m_label_health;
        public const string URL = "ui://q68vr2bfjtpbhg";

        public static UI_PlayerHealth CreateInstance()
        {
            return (UI_PlayerHealth)UIPackage.CreateObject("Main", "PlayerHealth");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_bar_fill = (GGraph)GetChildAt(2);
            m_label_level = (GTextField)GetChildAt(9);
            m_label_health = (GTextField)GetChildAt(10);
        }
    }
}