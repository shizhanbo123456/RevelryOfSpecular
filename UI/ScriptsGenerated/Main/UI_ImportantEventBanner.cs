/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_ImportantEventBanner : GComponent
    {
        public GTextField m_title;
        public Transition m_t0;
        public const string URL = "ui://q68vr2bfn0xwme";

        public static UI_ImportantEventBanner CreateInstance()
        {
            return (UI_ImportantEventBanner)UIPackage.CreateObject("Main", "ImportantEventBanner");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_title = (GTextField)GetChildAt(2);
            m_t0 = GetTransitionAt(0);
        }
    }
}