/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_SkillTip : GComponent
    {
        public GTextField m_SkillName;
        public GTextField m_label_cd;
        public GTextField m_label_store;
        public GList m_score_list;
        public const string URL = "ui://q68vr2bfgmlkm0";

        public static UI_SkillTip CreateInstance()
        {
            return (UI_SkillTip)UIPackage.CreateObject("Main", "SkillTip");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_SkillName = (GTextField)GetChildAt(1);
            m_label_cd = (GTextField)GetChildAt(3);
            m_label_store = (GTextField)GetChildAt(5);
            m_score_list = (GList)GetChildAt(6);
        }
    }
}