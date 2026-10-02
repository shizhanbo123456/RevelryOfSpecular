using UnityEngine;

namespace Ros.Info
{
    [CreateAssetMenu(menuName = "Ros/PlayerCharacterInfo", fileName = "PlayerCharacterInfo")]
    public class PlayerCharacterInfo : EntityAttributeInfo
    {
        [Header("局外解锁")]
        public int unlockPlayerLevel = 1;
    }
}
