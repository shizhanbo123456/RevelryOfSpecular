/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_GmButtonList : GComponent
    {
        public GList m_button_list;
        public const string URL = "ui://q68vr2bftjktip";

        public static UI_GmButtonList CreateInstance()
        {
            return (UI_GmButtonList)UIPackage.CreateObject("Main", "GmButtonList");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_button_list = (GList)GetChildAt(0);
        }
    }
}