/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_HomeCharacterList : GComponent
    {
        public UI_panel_2 m_panel;
        public GList m_characterList;
        public GList m_attrList;
        public GButton m_btn_defense;
        public GButton m_btn_attack;
        public const string URL = "ui://q68vr2bftwqeh2";

        public static UI_HomeCharacterList CreateInstance()
        {
            return (UI_HomeCharacterList)UIPackage.CreateObject("Main", "HomeCharacterList");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_panel = (UI_panel_2)GetChildAt(0);
            m_characterList = (GList)GetChildAt(2);
            m_attrList = (GList)GetChildAt(3);
            m_btn_defense = (GButton)GetChildAt(4);
            m_btn_attack = (GButton)GetChildAt(5);
        }
    }
}