/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_InputField : GComponent
    {
        public GTextInput m_content;
        public const string URL = "ui://q68vr2bfjtpbhc";

        public static UI_InputField CreateInstance()
        {
            return (UI_InputField)UIPackage.CreateObject("Main", "InputField");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_content = (GTextInput)GetChildAt(1);
        }
    }
}