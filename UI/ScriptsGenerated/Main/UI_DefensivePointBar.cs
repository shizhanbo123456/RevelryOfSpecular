/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_DefensivePointBar : GComponent
    {
        public GImage m_fill;
        public const string URL = "ui://q68vr2bfrzmhih";

        public static UI_DefensivePointBar CreateInstance()
        {
            return (UI_DefensivePointBar)UIPackage.CreateObject("Main", "DefensivePointBar");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_fill = (GImage)GetChildAt(1);
        }
    }
}