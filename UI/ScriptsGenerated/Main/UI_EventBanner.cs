/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_EventBanner : GLabel
    {
        public Controller m_type;
        public const string URL = "ui://q68vr2bfdqflka";

        public static UI_EventBanner CreateInstance()
        {
            return (UI_EventBanner)UIPackage.CreateObject("Main", "EventBanner");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_type = GetControllerAt(0);
        }
    }
}