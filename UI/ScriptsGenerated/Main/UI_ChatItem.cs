/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_ChatItem : GComponent
    {
        public Controller m_camp;
        public Controller m_side;
        public GTextField m_content;
        public GLoader m_headIcon;
        public GTextField m_player_name;
        public const string URL = "ui://q68vr2bfobkwj5";

        public static UI_ChatItem CreateInstance()
        {
            return (UI_ChatItem)UIPackage.CreateObject("Main", "ChatItem");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_camp = GetControllerAt(0);
            m_side = GetControllerAt(1);
            m_content = (GTextField)GetChildAt(1);
            m_headIcon = (GLoader)GetChildAt(3);
            m_player_name = (GTextField)GetChildAt(10);
        }
    }
}