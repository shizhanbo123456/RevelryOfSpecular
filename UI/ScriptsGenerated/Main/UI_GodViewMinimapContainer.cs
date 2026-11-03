/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_GodViewMinimapContainer : GComponent
    {
        public GLoader m_loader;
        public const string URL = "ui://q68vr2bflp1tni";

        public static UI_GodViewMinimapContainer CreateInstance()
        {
            return (UI_GodViewMinimapContainer)UIPackage.CreateObject("Main", "GodViewMinimapContainer");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_loader = (GLoader)GetChildAt(1);
        }
    }
}