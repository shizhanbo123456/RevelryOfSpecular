/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_PlayerName : GComponent
    {
        public GTextField m_num;
        public const string URL = "ui://q68vr2bfjtpbi9";

        public static UI_PlayerName CreateInstance()
        {
            return (UI_PlayerName)UIPackage.CreateObject("Main", "PlayerName");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_num = (GTextField)GetChildAt(1);
        }
    }
}