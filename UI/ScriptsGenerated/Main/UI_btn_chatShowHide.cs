/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_btn_chatShowHide : GButton
    {
        public Controller m_side;
        public const string URL = "ui://q68vr2bfqh53k1";

        public static UI_btn_chatShowHide CreateInstance()
        {
            return (UI_btn_chatShowHide)UIPackage.CreateObject("Main", "btn_chatShowHide");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_side = GetControllerAt(1);
        }
    }
}