/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_FullScreenMask : GComponent
    {
        public GTextField m_empty_text_merely_make_sure_this_component_can_be_published;
        public const string URL = "ui://q68vr2bfgwrkiz";

        public static UI_FullScreenMask CreateInstance()
        {
            return (UI_FullScreenMask)UIPackage.CreateObject("Main", "FullScreenMask");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_empty_text_merely_make_sure_this_component_can_be_published = (GTextField)GetChildAt(1);
        }
    }
}