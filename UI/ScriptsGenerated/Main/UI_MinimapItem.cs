/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_MinimapItem : GComponent
    {
        public Controller m_type;
        public const string URL = "ui://q68vr2bfjtpbi7";

        public static UI_MinimapItem CreateInstance()
        {
            return (UI_MinimapItem)UIPackage.CreateObject("Main", "MinimapItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_type = GetControllerAt(0);
        }
    }
}