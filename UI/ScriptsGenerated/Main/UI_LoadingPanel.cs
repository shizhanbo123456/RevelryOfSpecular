/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_LoadingPanel : GComponent
    {
        public Controller m_page;
        public Controller m_show_bar;
        public GProgressBar m_progressbar;
        public GTextField m_progress;
        public GTextField m_label;
        public const string URL = "ui://q68vr2bfpdekji";

        public static UI_LoadingPanel CreateInstance()
        {
            return (UI_LoadingPanel)UIPackage.CreateObject("Main", "LoadingPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_page = GetControllerAt(0);
            m_show_bar = GetControllerAt(1);
            m_progressbar = (GProgressBar)GetChildAt(4);
            m_progress = (GTextField)GetChildAt(5);
            m_label = (GTextField)GetChildAt(6);
        }
    }
}