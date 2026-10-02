using UnityEngine;

[CreateAssetMenu(menuName = "Ros/SkillInfo", fileName = "SkillInfo")]
public class SkillInfo : ScriptableObject
{
    public int id;

    public string skillName = "";

    public Sprite icon;
}
