/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_iconEmerald : GComponent
    {
        public Controller m_empty;
        public const string URL = "ui://q68vr2bflu0blr";

        public static UI_iconEmerald CreateInstance()
        {
            return (UI_iconEmerald)UIPackage.CreateObject("Main", "iconEmerald");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_empty = GetControllerAt(0);
        }
    }
}