/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_HomeAttrList : GComponent
    {
        public UI_Panel_1 m_panel;
        public GList m_attrList;
        public const string URL = "ui://q68vr2bftwqeh6";

        public static UI_HomeAttrList CreateInstance()
        {
            return (UI_HomeAttrList)UIPackage.CreateObject("Main", "HomeAttrList");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_panel = (UI_Panel_1)GetChildAt(0);
            m_attrList = (GList)GetChildAt(1);
        }
    }
}