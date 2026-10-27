/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_buffItemMask : GComponent
    {
        public GLoader m_loader;
        public const string URL = "ui://q68vr2bfncwmln";

        public static UI_buffItemMask CreateInstance()
        {
            return (UI_buffItemMask)UIPackage.CreateObject("Main", "buffItemMask");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_loader = (GLoader)GetChildAt(1);
        }
    }
}