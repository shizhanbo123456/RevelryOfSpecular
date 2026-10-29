/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_empty_container : GComponent
    {
        public GGraph m_only_make_sure_this_component_can_be_exported;
        public const string URL = "ui://q68vr2bfkwfdmt";

        public static UI_empty_container CreateInstance()
        {
            return (UI_empty_container)UIPackage.CreateObject("Main", "empty_container");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_only_make_sure_this_component_can_be_exported = (GGraph)GetChildAt(0);
        }
    }
}