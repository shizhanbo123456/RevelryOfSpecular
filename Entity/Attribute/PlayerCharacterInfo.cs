using UnityEngine;

namespace Ros.Info
{
    /// <summary>
    /// 玩家角色信息（继承实体属性 SO，以 Info 结尾）。
    /// 在基础属性/升级路线之上增加局外解锁配置；进攻/防守双方角色均使用本类型（InfoManager 双列表）。
    /// </summary>
    [CreateAssetMenu(menuName = "Ros/PlayerCharacterInfo", fileName = "PlayerCharacterInfo")]
    public class PlayerCharacterInfo : EntityAttributeInfo
    {
        [Header("局外解锁（双等级制，策划案 10.2：玩家等级达标后自动解锁）")]
        /// <summary>解锁所需玩家等级（账号级，达标即解锁该角色）。</summary>
        public int unlockPlayerLevel = 1;
    }
}
