using UnityEngine;

namespace Ros.Info
{
    [CreateAssetMenu]
    public class EntityAttributeInfo : ScriptableObject
    {
        public string Name;
        [Header("BaseAttributes")]
        public int health=1200;
        [Space]
        [Range(0,400)]public int attack=200;
        [Range(0, 100)] public int strikeRate=20;
        [Range(0, 400)] public int strikeDamage=200;
        [Space]
        [Range(0, 400)] public int defense=200;
        [Range(0, 100)] public int strikeRateResistance=0;
        [Range(0, 400)] public int strikeDamageResistance=0;
        [Space]
        [Range(0, 200)] public int endurance=100;
        [Range(0, 80)] public int speed=40;
        [Range(0, 100)] public int pickAbility=20;
        [Range(0, 200)] public int digAbility=100;
        [Space]
        [Header("Growth")]
        public int healthGrowth;
        [Space]
        public int attackGrowth;
        public int strikeRateGrowth;
        public int strikeDamageGrowth;
        [Space]
        public int defenseGrowth;
        public int strikeRateResistanceGrowth;
        public int strikeDamageResistanceGrowth;
        [Space]
        public int enduranceGrowth;
        public int speedGrowth;
        public int pickAbilityGrowth;
        public int digAbilityGrowth;
        public EntityAttribute GetAttribute(int level=0)
        {
            return new EntityAttribute()
            {
                health=health+healthGrowth*level,
                attack=attack+attackGrowth*level,
                strikeRate=strikeRate+strikeRateGrowth*level,
                strikeDamage=strikeDamage+strikeDamageGrowth*level,
                defense=defense+defenseGrowth*level,
                strikeRateResistance=strikeRateResistance+strikeRateResistanceGrowth*level,
                strikeDamageResistance=strikeDamageResistance+strikeDamageResistanceGrowth*level,
                endurance=endurance+enduranceGrowth*level,
                speed=speed+speedGrowth*level,
                pickAbility=pickAbility+pickAbilityGrowth*level,
                digAbility=digAbility+digAbilityGrowth*level,
            };
        }
    }
}