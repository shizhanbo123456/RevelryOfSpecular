/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_buffItem : GComponent
    {
        public Controller m_negative;
        public UI_buffItemMask m_mask;
        public GTextField m_layer_count;
        public const string URL = "ui://q68vr2bfncwmlm";

        public static UI_buffItem CreateInstance()
        {
            return (UI_buffItem)UIPackage.CreateObject("Main", "buffItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_negative = GetControllerAt(0);
            m_mask = (UI_buffItemMask)GetChildAt(1);
            m_layer_count = (GTextField)GetChildAt(4);
        }
    }
}