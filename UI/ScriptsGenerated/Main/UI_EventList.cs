/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_EventList : GComponent
    {
        public GList m_EventItemContainer;
        public const string URL = "ui://q68vr2bfjtpbi5";

        public static UI_EventList CreateInstance()
        {
            return (UI_EventList)UIPackage.CreateObject("Main", "EventList");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_EventItemContainer = (GList)GetChildAt(0);
        }
    }
}