/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_buffList : GComponent
    {
        public GList m_list;
        public const string URL = "ui://q68vr2bfncwmlo";

        public static UI_buffList CreateInstance()
        {
            return (UI_buffList)UIPackage.CreateObject("Main", "buffList");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_list = (GList)GetChildAt(0);
        }
    }
}