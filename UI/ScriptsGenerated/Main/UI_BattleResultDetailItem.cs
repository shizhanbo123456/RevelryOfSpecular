/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_BattleResultDetailItem : GComponent
    {
        public GTextField m_content;
        public Transition m_t0;
        public const string URL = "ui://q68vr2bfjtpbig";

        public static UI_BattleResultDetailItem CreateInstance()
        {
            return (UI_BattleResultDetailItem)UIPackage.CreateObject("Main", "BattleResultDetailItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_content = (GTextField)GetChildAt(1);
            m_t0 = GetTransitionAt(0);
        }
    }
}