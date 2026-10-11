/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_GmComboBox_popup : GComponent
    {
        public GList m_list;
        public const string URL = "ui://q68vr2bftjktix";

        public static UI_GmComboBox_popup CreateInstance()
        {
            return (UI_GmComboBox_popup)UIPackage.CreateObject("Main", "GmComboBox_popup");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_list = (GList)GetChildAt(1);
        }
    }
}