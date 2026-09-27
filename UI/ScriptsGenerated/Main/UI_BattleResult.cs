/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_BattleResult : GComponent
    {
        public GTextField m_title;
        public GTextField m_content;
        public Transition m_t0;
        public const string URL = "ui://q68vr2bfjtpbie";

        public static UI_BattleResult CreateInstance()
        {
            return (UI_BattleResult)UIPackage.CreateObject("Main", "BattleResult");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_title = (GTextField)GetChildAt(2);
            m_content = (GTextField)GetChildAt(3);
            m_t0 = GetTransitionAt(0);
        }
    }
}