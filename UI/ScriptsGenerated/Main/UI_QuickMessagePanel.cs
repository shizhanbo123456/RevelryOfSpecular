/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_QuickMessagePanel : GComponent
    {
        public GTextField m_label1;
        public GTextField m_label2;
        public GTextField m_label3;
        public GTextField m_label4;
        public GTextField m_label5;
        public GTextField m_label6;
        public GTextField m_label7;
        public GTextField m_label8;
        public const string URL = "ui://q68vr2bfe249ne";

        public static UI_QuickMessagePanel CreateInstance()
        {
            return (UI_QuickMessagePanel)UIPackage.CreateObject("Main", "QuickMessagePanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_label1 = (GTextField)GetChildAt(10);
            m_label2 = (GTextField)GetChildAt(11);
            m_label3 = (GTextField)GetChildAt(12);
            m_label4 = (GTextField)GetChildAt(13);
            m_label5 = (GTextField)GetChildAt(14);
            m_label6 = (GTextField)GetChildAt(15);
            m_label7 = (GTextField)GetChildAt(16);
            m_label8 = (GTextField)GetChildAt(17);
        }
    }
}