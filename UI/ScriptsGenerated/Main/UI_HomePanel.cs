/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_HomePanel : GComponent
    {
        public UI_HomeConnect m_connectPanel;
        public UI_PlayerInfo m_playerInfo;
        public UI_HomeCharacterList m_characterList;
        public const string URL = "ui://q68vr2bfvjeqgv";

        public static UI_HomePanel CreateInstance()
        {
            return (UI_HomePanel)UIPackage.CreateObject("Main", "HomePanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_connectPanel = (UI_HomeConnect)GetChildAt(0);
            m_playerInfo = (UI_PlayerInfo)GetChildAt(1);
            m_characterList = (UI_HomeCharacterList)GetChildAt(2);
        }
    }
}