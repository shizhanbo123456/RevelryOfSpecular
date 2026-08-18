using System.Collections.Generic;

public class SettleRewardInfo
{
    public int timeCount;
    public int exp;
    public HashSet<int> characterUnlocks;
    public List<int> characterToken;
    public List<int> imprint;
    public List<int> rune;
    public List<int> note;
    public SettleRewardInfo()
    {
        characterUnlocks= new();
        characterToken = new();
        Tool.ExpandListTo(characterToken, Config.character_count);
        imprint = new();
        Tool.ExpandListTo(imprint, Config.imprint_count);
        rune = new();
        Tool.ExpandListTo(rune, Config.skill_count);
        note = new();
        Tool.ExpandListTo(note, Config.note_count);
    }
    public void GetExp(int value)
    {
        exp+= value;
    }
    public void GetCharacterUnlock(int type)
    {
        characterUnlocks.Add(type);
    }
    public void GetCharacterToken(int type,int count)
    {
        characterToken[type] += count;
    }
    public void GetImprint(int type, int count)
    {
        imprint[type] += count;
    }
    public void GetRune(int type,int count)
    {
        rune[type] += count;
    }
    public void GetNote(int type,int count)
    {
        note[type] += count;
    }
}
public class RuntimeRewardInfo
{
    public int fruit;
    public int infectedFruit;
    public int fragment;
    public int infectedFragment;

    public RuntimeRewardInfo()
    {

    }
    public void GetFruit(int value)
    {
        fruit += value;
    }
    public void GetInfectedFruit(int value)
    {
        infectedFruit += value;
    }
    public void GetFragment(int value)
    {
        fragment += value;
    }
    public void GetInfectedFragment(int value)
    {
        infectedFragment += value;
    }
}
