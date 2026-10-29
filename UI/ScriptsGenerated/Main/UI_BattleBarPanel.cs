/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_BattleBarPanel : GComponent
    {
        public UI_DefensivePointBar m_progressMain;
        public UI_DefensivePointBar m_progressSub1;
        public UI_DefensivePointBar m_progressSub2;
        public UI_DefensivePointBar m_progressSub3;
        public const string URL = "ui://q68vr2bfiq0omj";

        public static UI_BattleBarPanel CreateInstance()
        {
            return (UI_BattleBarPanel)UIPackage.CreateObject("Main", "BattleBarPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_progressMain = (UI_DefensivePointBar)GetChildAt(2);
            m_progressSub1 = (UI_DefensivePointBar)GetChildAt(4);
            m_progressSub2 = (UI_DefensivePointBar)GetChildAt(6);
            m_progressSub3 = (UI_DefensivePointBar)GetChildAt(8);
        }
    }
}