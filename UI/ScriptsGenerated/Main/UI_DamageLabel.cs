/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_DamageLabel : GComponent
    {
        public GTextField m_num;
        public const string URL = "ui://q68vr2bfjtpbi8";

        public static UI_DamageLabel CreateInstance()
        {
            return (UI_DamageLabel)UIPackage.CreateObject("Main", "DamageLabel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_num = (GTextField)GetChildAt(0);
        }
    }
}