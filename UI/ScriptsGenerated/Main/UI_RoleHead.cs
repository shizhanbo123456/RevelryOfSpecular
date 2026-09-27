/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_RoleHead : GButton
    {
        public Controller m_level;
        public Controller m_lock;
        public GLoader m_headIcon;
        public GTextField m_roleName;
        public const string URL = "ui://q68vr2bfvjeqgx";

        public static UI_RoleHead CreateInstance()
        {
            return (UI_RoleHead)UIPackage.CreateObject("Main", "RoleHead");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_level = GetControllerAt(1);
            m_lock = GetControllerAt(2);
            m_headIcon = (GLoader)GetChildAt(1);
            m_roleName = (GTextField)GetChildAt(15);
        }
    }
}