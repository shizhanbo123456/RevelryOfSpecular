/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_SkillList : GComponent
    {
        public GList m_content;
        public const string URL = "ui://q68vr2bfjtpbhe";

        public static UI_SkillList CreateInstance()
        {
            return (UI_SkillList)UIPackage.CreateObject("Main", "SkillList");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_content = (GList)GetChildAt(2);
        }
    }
}