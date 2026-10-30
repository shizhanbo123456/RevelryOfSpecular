/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_SkillSelectPanel : GComponent
    {
        public UI_panel_2 m_panel;
        public GList m_list;
        public const string URL = "ui://q68vr2bfjatfn3";

        public static UI_SkillSelectPanel CreateInstance()
        {
            return (UI_SkillSelectPanel)UIPackage.CreateObject("Main", "SkillSelectPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_panel = (UI_panel_2)GetChildAt(1);
            m_list = (GList)GetChildAt(2);
        }
    }
}