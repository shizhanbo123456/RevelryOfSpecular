/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace Main
{
    public partial class UI_BattleResultDetail : GComponent
    {
        public GList m_resultList;
        public const string URL = "ui://q68vr2bfjtpbif";

        public static UI_BattleResultDetail CreateInstance()
        {
            return (UI_BattleResultDetail)UIPackage.CreateObject("Main", "BattleResultDetail");
        }

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_resultList = (GList)GetChildAt(0);
        }
    }
}