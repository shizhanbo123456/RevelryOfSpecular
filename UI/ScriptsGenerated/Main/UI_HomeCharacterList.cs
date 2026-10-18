/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Ros.UI.Main
{
    public partial class UI_HomeCharacterList : GComponent
    {
        public UI_panel_2 m_panel;
        public GList m_characterList;
        public UI_button3 m_btn_attacker;
        public UI_button3 m_btn_defenser;
        public GList m_attrList;
        public UI_button2 m_btn_confirm;
        public const string URL = "ui://q68vr2bftwqeh2";

        public static UI_HomeCharacterList CreateInstance()
        {
            return (UI_HomeCharacterList)UIPackage.CreateObject("Main", "HomeCharacterList");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_panel = (UI_panel_2)GetChildAt(0);
            m_characterList = (GList)GetChildAt(1);
            m_btn_attacker = (UI_button3)GetChildAt(3);
            m_btn_defenser = (UI_button3)GetChildAt(4);
            m_attrList = (GList)GetChildAt(5);
            m_btn_confirm = (UI_button2)GetChildAt(6);
        }
    }
}