/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_HomeCharacterList : GComponent
    {
        public UI_Panel_1 m_panel;
        public GList m_characterList;
        public UI_Button1 m_btn_attacker;
        public UI_Button1 m_btn_defenser;
        public UI_Button1 m_btn_finish;
        public const string URL = "ui://q68vr2bftwqeh2";

        public static UI_HomeCharacterList CreateInstance()
        {
            return (UI_HomeCharacterList)UIPackage.CreateObject("Main", "HomeCharacterList");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_panel = (UI_Panel_1)GetChildAt(0);
            m_characterList = (GList)GetChildAt(1);
            m_btn_attacker = (UI_Button1)GetChildAt(2);
            m_btn_defenser = (UI_Button1)GetChildAt(3);
            m_btn_finish = (UI_Button1)GetChildAt(4);
        }
    }
}