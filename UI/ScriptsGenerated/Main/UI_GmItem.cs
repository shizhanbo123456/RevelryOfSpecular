/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_GmItem : GComponent
    {
        public Controller m_content;
        public UI_Button1 m_content_btn;
        public GSlider m_content_slider;
        public GTextInput m_content_inputField;
        public GComboBox m_content_dropdown;
        public GTextField m_content_slider_value;
        public GTextField m_content_text;
        public GTextField m_title;
        public const string URL = "ui://q68vr2bftjktiq";

        public static UI_GmItem CreateInstance()
        {
            return (UI_GmItem)UIPackage.CreateObject("Main", "GmItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_content = GetControllerAt(0);
            m_content_btn = (UI_Button1)GetChildAt(1);
            m_content_slider = (GSlider)GetChildAt(2);
            m_content_inputField = (GTextInput)GetChildAt(4);
            m_content_dropdown = (GComboBox)GetChildAt(5);
            m_content_slider_value = (GTextField)GetChildAt(6);
            m_content_text = (GTextField)GetChildAt(10);
            m_title = (GTextField)GetChildAt(11);
        }
    }
}