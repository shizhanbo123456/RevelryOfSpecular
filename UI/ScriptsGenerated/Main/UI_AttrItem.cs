/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_AttrItem : GComponent
    {
        public Controller m_show_bg;
        public GImage m_base;
        public GImage m_growth_fill;
        public GImage m_next_level_fill;
        public GTextField m_attrName;
        public GTextField m_attrValue;
        public const string URL = "ui://q68vr2bftwqeh5";

        public static UI_AttrItem CreateInstance()
        {
            return (UI_AttrItem)UIPackage.CreateObject("Main", "AttrItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_show_bg = GetControllerAt(0);
            m_base = (GImage)GetChildAt(1);
            m_growth_fill = (GImage)GetChildAt(2);
            m_next_level_fill = (GImage)GetChildAt(3);
            m_attrName = (GTextField)GetChildAt(5);
            m_attrValue = (GTextField)GetChildAt(6);
        }
    }
}