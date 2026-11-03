/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_MinimapItemButton : GButton
    {
        public Controller m_type;
        public const string URL = "ui://q68vr2bfko53nj";

        public static UI_MinimapItemButton CreateInstance()
        {
            return (UI_MinimapItemButton)UIPackage.CreateObject("Main", "MinimapItemButton");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_type = GetControllerAt(1);
        }
    }
}