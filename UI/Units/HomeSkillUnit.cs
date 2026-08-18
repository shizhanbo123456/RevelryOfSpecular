using UnityEngine;
using UnityEngine.UI;

public class HomeSkillUnit : MonoBehaviour
{
    public Image Icon;
    public Text Name;
    public Text Quality;
    public Text OwnCount;//总共拥有的数量
    public Text TakeCount;//带入的数量
    public Button Increase;//增加带入的数量
    public Button Decrease;//减少带入的数量
    public GameObject haveRune;//拥有（符文数量>0）
    public GameObject selected;//选中状态（带入数量>0）
}
