using UnityEngine;

namespace MyPoeLikeGame.Combat
{
    /// <summary>
    /// 交給 <see cref="AttackBehaviour"/> 的一次攻擊快照。
    /// 行為需要的東西一律由這裡傳入，行為不自己去抓全域狀態。
    /// </summary>
    public readonly struct AttackContext
    {
        /// <summary>發動攻擊的角色。</summary>
        public readonly Transform Sender;

        /// <summary>這次攻擊的資料。</summary>
        public readonly Skill Skill;

        /// <summary>
        /// 蓄力強度，取離開 OVERDRAW 當下的值；沒進過 OVERDRAW 就是 0。
        /// 可以超過 1（過蓄）。
        /// </summary>
        public readonly float ChargeRatio;

        /// <summary>投射物生成器。尚未接上前為 null。</summary>
        public readonly IProjectileSpawner Spawner;

        public AttackContext(Transform sender, Skill skill, float chargeRatio, IProjectileSpawner spawner)
        {
            Sender = sender;
            Skill = skill;
            ChargeRatio = chargeRatio;
            Spawner = spawner;
        }
    }
}
