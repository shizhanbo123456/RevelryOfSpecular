/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_TipLevel : GComponent
    {
        public Controller m_level;
        public GTextField m_title;
        public const string URL = "ui://q68vr2bfgmlkm1";

        public static UI_TipLevel CreateInstance()
        {
            return (UI_TipLevel)UIPackage.CreateObject("Main", "TipLevel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_level = GetControllerAt(0);
            m_title = (GTextField)GetChildAt(0);
        }
    }
}