/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_AttrItem : GComponent
    {
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

            m_attrName = (GTextField)GetChildAt(1);
            m_attrValue = (GTextField)GetChildAt(2);
        }
    }
}