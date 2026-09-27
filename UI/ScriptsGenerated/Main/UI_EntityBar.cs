/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_EntityBar : GComponent
    {
        public GImage m_fill;
        public GTextField m_label;
        public const string URL = "ui://q68vr2bfjtpbia";

        public static UI_EntityBar CreateInstance()
        {
            return (UI_EntityBar)UIPackage.CreateObject("Main", "EntityBar");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_fill = (GImage)GetChildAt(1);
            m_label = (GTextField)GetChildAt(4);
        }
    }
}