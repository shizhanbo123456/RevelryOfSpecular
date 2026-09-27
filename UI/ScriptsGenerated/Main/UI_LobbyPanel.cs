/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_LobbyPanel : GComponent
    {
        public UI_LobbyMemberList m_mainView;
        public const string URL = "ui://q68vr2bftwqeh8";

        public static UI_LobbyPanel CreateInstance()
        {
            return (UI_LobbyPanel)UIPackage.CreateObject("Main", "LobbyPanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_mainView = (UI_LobbyMemberList)GetChildAt(0);
        }
    }
}