using UnityEngine;

[CreateAssetMenu]
public class SkillInfo : ScriptableObject
{
    public enum Quality
    {
        C,B,A,S
    }
    public Quality quality;
    public bool infectionSkill;
    public int weight = 1;
    public Sprite sprite;
}