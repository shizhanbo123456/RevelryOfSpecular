using UnityEngine;
using UnityEngine.UI;

public partial class HomePage : RosPage
{
    [SerializeField] private Text playerLevelLabel;

    [SerializeField] private RosList attackerInfoList;
    [SerializeField] private RosList defenserInfoList;
    private RosListWrapper<HomeCharacterItem> attackerInfoListWrapper;
    private RosListWrapper<HomeCharacterItem> defenserInfoListWrapper;

    [SerializeField] private RosButton connectButton;
    [SerializeField] private InputField IpAddressInputField;

    public override void Construct()
    {
        attackerInfoListWrapper = new(attackerInfoList);
        defenserInfoListWrapper = new(defenserInfoList);
    }
}