/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_onlookComboBox_popup : GComponent
    {
        public GList m_list;
        public const string URL = "ui://q68vr2bfncwbj7";

        public static UI_onlookComboBox_popup CreateInstance()
        {
            return (UI_onlookComboBox_popup)UIPackage.CreateObject("Main", "onlookComboBox_popup");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_list = (GList)GetChildAt(1);
        }
    }
}