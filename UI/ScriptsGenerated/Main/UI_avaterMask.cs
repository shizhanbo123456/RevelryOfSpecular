/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_avaterMask : GComponent
    {
        public GLoader m_content;
        public const string URL = "ui://q68vr2bfncwmlp";

        public static UI_avaterMask CreateInstance()
        {
            return (UI_avaterMask)UIPackage.CreateObject("Main", "avaterMask");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_content = (GLoader)GetChildAt(1);
        }
    }
}