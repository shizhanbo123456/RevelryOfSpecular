/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_SkillListItem : GComponent
    {
        public GLoader m_loader_iconBase;
        public GLoader m_loader_icon;
        public GTextField m_store;
        public GTextField m_key;
        public GList m_starList;
        public const string URL = "ui://q68vr2bfjtpbhf";

        public static UI_SkillListItem CreateInstance()
        {
            return (UI_SkillListItem)UIPackage.CreateObject("Main", "SkillListItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_loader_iconBase = (GLoader)GetChildAt(7);
            m_loader_icon = (GLoader)GetChildAt(8);
            m_store = (GTextField)GetChildAt(10);
            m_key = (GTextField)GetChildAt(11);
            m_starList = (GList)GetChildAt(12);
        }
    }
}