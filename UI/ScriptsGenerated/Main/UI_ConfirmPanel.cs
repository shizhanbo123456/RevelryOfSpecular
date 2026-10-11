/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_ConfirmPanel : GComponent
    {
        public GButton m_btn_confirm;
        public GButton m_btn_cancel;
        public const string URL = "ui://q68vr2bftjktim";

        public static UI_ConfirmPanel CreateInstance()
        {
            return (UI_ConfirmPanel)UIPackage.CreateObject("Main", "ConfirmPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_btn_confirm = (GButton)GetChildAt(3);
            m_btn_cancel = (GButton)GetChildAt(4);
        }
    }
}