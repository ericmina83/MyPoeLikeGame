using UnityEngine;

namespace MyPoeLikeGame.Combat
{
    /// <summary>
    /// 生成投射物的能力。<see cref="AttackBehaviour"/> 透過 <see cref="AttackContext"/>
    /// 拿到它，而不是自己去找場上的池子。
    /// </summary>
    /// <remarks>
    /// 尚未有實作。物件池重整完成後（計畫 Phase 15、16）才會接上，
    /// 在那之前 <see cref="AttackContext.Spawner"/> 會是 null。
    /// </remarks>
    public interface IProjectileSpawner
    {
        /// <param name="chargeRatio">離開 OVERDRAW 時的蓄力強度，未蓄力為 0。</param>
        void Spawn(Projectile prefab, Transform sender, Vector3 offset, Vector3 direction, float chargeRatio);
    }
}
