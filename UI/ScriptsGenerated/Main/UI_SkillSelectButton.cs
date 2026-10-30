/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_SkillSelectButton : GButton
    {
        public GLoader m_loader_icon;
        public const string URL = "ui://q68vr2bfjatfn4";

        public static UI_SkillSelectButton CreateInstance()
        {
            return (UI_SkillSelectButton)UIPackage.CreateObject("Main", "SkillSelectButton");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_loader_icon = (GLoader)GetChildAt(2);
        }
    }
}