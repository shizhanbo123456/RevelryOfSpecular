using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EntityAttribute
{
    public int health = 1200;
    public int attack = 200;
    public int strikeRate = 20;
    public int strikeDamage = 200;
    public int defense = 200;
    public int strikeRateResistance = 0;
    public int strikeDamageResistance = 0;
    public int endurance = 100;
    public int speed = 40;
    public int pickAbility = 20;
    public int digAbility = 100;

    public EntityAttribute Clone()
    {
        return new EntityAttribute()
        {
            health = health,
            attack = attack,
            strikeRate = strikeRate,
            strikeDamage = strikeDamage,
            defense = defense,
            strikeRateResistance = strikeRateResistance,
            strikeDamageResistance = strikeDamageResistance,
            endurance = endurance,
            speed = speed,
            pickAbility = pickAbility,
            digAbility = digAbility
        };
    }
    public List<int> GetValueList()
    {
        return new List<int>()
        {
            health,attack,strikeRate,strikeDamage,defense,
            strikeRateResistance,strikeDamageResistance,
            endurance,speed,pickAbility, digAbility
        };
    }
}