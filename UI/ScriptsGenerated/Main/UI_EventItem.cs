/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_EventItem : GComponent
    {
        public Controller m_type;
        public GTextField m_type0_label;
        public UI_EventIcon m_type1_loader;
        public GTextField m_type1_label;
        public GTextField m_type2_label1;
        public UI_EventIcon m_type2_loader;
        public GTextField m_type2_label2;
        public const string URL = "ui://q68vr2bfjtpbi4";

        public static UI_EventItem CreateInstance()
        {
            return (UI_EventItem)UIPackage.CreateObject("Main", "EventItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_type = GetControllerAt(0);
            m_type0_label = (GTextField)GetChildAt(1);
            m_type1_loader = (UI_EventIcon)GetChildAt(4);
            m_type1_label = (GTextField)GetChildAt(5);
            m_type2_label1 = (GTextField)GetChildAt(8);
            m_type2_loader = (UI_EventIcon)GetChildAt(9);
            m_type2_label2 = (GTextField)GetChildAt(10);
        }
    }
}