/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_HomePanel : GComponent
    {
        public Controller m_page;
        public UI_HomeCharacterList m_characterPanel;
        public UI_HomeAttrList m_attributePanel;
        public UI_HomeConnect m_connectPanel;
        public UI_PlayerInfo m_playerInfo;
        public const string URL = "ui://q68vr2bfvjeqgv";

        public static UI_HomePanel CreateInstance()
        {
            return (UI_HomePanel)UIPackage.CreateObject("Main", "HomePanel");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_page = GetControllerAt(0);
            m_characterPanel = (UI_HomeCharacterList)GetChildAt(0);
            m_attributePanel = (UI_HomeAttrList)GetChildAt(1);
            m_connectPanel = (UI_HomeConnect)GetChildAt(2);
            m_playerInfo = (UI_PlayerInfo)GetChildAt(3);
        }
    }
}