/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_ConfirmPanel : GComponent
    {
        public GTextField m_content;
        public UI_button2 m_btn_confirm;
        public UI_button2 m_btn_cancel;
        public const string URL = "ui://q68vr2bftjktim";

        public static UI_ConfirmPanel CreateInstance()
        {
            return (UI_ConfirmPanel)UIPackage.CreateObject("Main", "ConfirmPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_content = (GTextField)GetChildAt(1);
            m_btn_confirm = (UI_button2)GetChildAt(2);
            m_btn_cancel = (UI_button2)GetChildAt(3);
        }
    }
}