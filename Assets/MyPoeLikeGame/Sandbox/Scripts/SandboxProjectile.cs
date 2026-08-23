using UnityEngine;

namespace MyPoeLikeGame.Sandbox
{
    /// <summary>
    /// Sandbox 用的投射物視覺。往前直線飛行，時間到自行銷毀。
    /// </summary>
    /// <remarks>
    /// 這**不是**正式的投射物。正式的生成路徑是
    /// <c>RangeChargeAttackBehaviour.OnEnterFire</c> → <c>IProjectileSpawner</c>，
    /// 但那個介面還沒有實作（計畫 Phase 15、16），所以 sandbox 先由
    /// <see cref="AttackDummyViz"/> 在偵測到進入 FIRE 時自己生一顆，
    /// 純粹是為了讓「瞬發」這件事看得見。
    /// </remarks>
    public class SandboxProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 9f;
        [SerializeField] private float lifeSeconds = 2.5f;

        private Vector3 direction = Vector3.forward;
        private float remaining;

        public void Launch(Vector3 worldDirection)
        {
            direction = worldDirection.normalized;
            remaining = lifeSeconds;
        }

        private void OnEnable() => remaining = lifeSeconds;

        private void Update()
        {
            transform.position += speed * Time.deltaTime * direction;

            remaining -= Time.deltaTime;
            if (remaining <= 0f)
                Destroy(gameObject);
        }
    }
}
