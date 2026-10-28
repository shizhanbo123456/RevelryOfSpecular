/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_SkillTip : GComponent
    {
        public GLoader m_icon;
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

            m_icon = (GLoader)GetChildAt(1);
            m_SkillName = (GTextField)GetChildAt(2);
            m_label_cd = (GTextField)GetChildAt(4);
            m_label_store = (GTextField)GetChildAt(6);
            m_score_list = (GList)GetChildAt(7);
        }
    }
}