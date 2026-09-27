/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_EventIcon : GComponent
    {
        public Controller m_type;
        public const string URL = "ui://q68vr2bfjtpbic";

        public static UI_EventIcon CreateInstance()
        {
            return (UI_EventIcon)UIPackage.CreateObject("Main", "EventIcon");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_type = GetControllerAt(0);
        }
    }
}