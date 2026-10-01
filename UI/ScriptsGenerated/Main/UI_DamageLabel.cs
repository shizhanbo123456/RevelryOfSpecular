/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_DamageLabel : GComponent
    {
        public Controller m_type;
        public GTextField m_num_common;
        public GTextField m_num_strike;
        public const string URL = "ui://q68vr2bfjtpbi8";

        public static UI_DamageLabel CreateInstance()
        {
            return (UI_DamageLabel)UIPackage.CreateObject("Main", "DamageLabel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_type = GetControllerAt(0);
            m_num_common = (GTextField)GetChildAt(0);
            m_num_strike = (GTextField)GetChildAt(2);
        }
    }
}