/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_SettingsRuleLabel : GComponent
    {
        public GTextField m_content;
        public const string URL = "ui://q68vr2bfj95pms";

        public static UI_SettingsRuleLabel CreateInstance()
        {
            return (UI_SettingsRuleLabel)UIPackage.CreateObject("Main", "SettingsRuleLabel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_content = (GTextField)GetChildAt(0);
        }
    }
}