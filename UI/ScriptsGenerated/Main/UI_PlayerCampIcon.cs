/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_PlayerCampIcon : GComponent
    {
        public Controller m_player_camp;
        public const string URL = "ui://q68vr2bfgmlklz";

        public static UI_PlayerCampIcon CreateInstance()
        {
            return (UI_PlayerCampIcon)UIPackage.CreateObject("Main", "PlayerCampIcon");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_player_camp = GetControllerAt(0);
        }
    }
}