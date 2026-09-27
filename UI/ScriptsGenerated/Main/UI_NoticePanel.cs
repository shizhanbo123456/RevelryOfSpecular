/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_NoticePanel : GComponent
    {
        public GTextField m_title;
        public const string URL = "ui://q68vr2bfjtpbi3";

        public static UI_NoticePanel CreateInstance()
        {
            return (UI_NoticePanel)UIPackage.CreateObject("Main", "NoticePanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_title = (GTextField)GetChildAt(1);
        }
    }
}