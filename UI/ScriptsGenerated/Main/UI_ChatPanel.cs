/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_ChatPanel : GComponent
    {
        public Controller m_show_channel_menu;
        public Controller m_fold;
        public GTextInput m_input;
        public GButton m_btn_send;
        public GList m_content;
        public GButton m_btn_allChannel;
        public GButton m_btn_campChannel;
        public UI_btn_chatShowHide m_btn_hide;
        public UI_btn_chatShowHide m_btn_show;
        public const string URL = "ui://q68vr2bfobkwj0";

        public static UI_ChatPanel CreateInstance()
        {
            return (UI_ChatPanel)UIPackage.CreateObject("Main", "ChatPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_show_channel_menu = GetControllerAt(0);
            m_fold = GetControllerAt(1);
            m_input = (GTextInput)GetChildAt(2);
            m_btn_send = (GButton)GetChildAt(4);
            m_content = (GList)GetChildAt(6);
            m_btn_allChannel = (GButton)GetChildAt(7);
            m_btn_campChannel = (GButton)GetChildAt(8);
            m_btn_hide = (UI_btn_chatShowHide)GetChildAt(10);
            m_btn_show = (UI_btn_chatShowHide)GetChildAt(12);
        }
    }
}