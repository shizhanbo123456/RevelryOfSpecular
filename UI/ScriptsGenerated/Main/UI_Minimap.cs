/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_Minimap : GComponent
    {
        public GGraph m_mapBase;
        public const string URL = "ui://q68vr2bfjtpbi6";

        public static UI_Minimap CreateInstance()
        {
            return (UI_Minimap)UIPackage.CreateObject("Main", "Minimap");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_mapBase = (GGraph)GetChildAt(0);
        }
    }
}