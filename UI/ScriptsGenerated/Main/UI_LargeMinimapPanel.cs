/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_LargeMinimapPanel : GComponent
    {
        public UI_panel_2 m_panel;
        public GLoader m_loader;
        public const string URL = "ui://q68vr2bfftrlnf";

        public static UI_LargeMinimapPanel CreateInstance()
        {
            return (UI_LargeMinimapPanel)UIPackage.CreateObject("Main", "LargeMinimapPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_panel = (UI_panel_2)GetChildAt(0);
            m_loader = (GLoader)GetChildAt(1);
        }
    }
}