/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_SkillListItem : GComponent
    {
        public Controller m_empty;
        public GLoader m_loader_iconBase;
        public GLoader m_loader_icon;
        public GTextField m_key;
        public GList m_storeList;
        public const string URL = "ui://q68vr2bfjtpbhf";

        public static UI_SkillListItem CreateInstance()
        {
            return (UI_SkillListItem)UIPackage.CreateObject("Main", "SkillListItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_empty = GetControllerAt(0);
            m_loader_iconBase = (GLoader)GetChildAt(2);
            m_loader_icon = (GLoader)GetChildAt(3);
            m_key = (GTextField)GetChildAt(4);
            m_storeList = (GList)GetChildAt(5);
        }
    }
}