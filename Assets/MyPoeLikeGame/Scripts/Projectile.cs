using System.Collections.Generic;
using UnityEngine;

namespace MyPoeLikeGame
{
    public class Projectile : Damage
    {
        public float projectileSpeed = 1f;

        public float maxDistance = 10f;

        private float distance = 0.0f;

        [SerializeField]
        private float raycastRadius = 0.5f;

        public enum ProjectileType { BOX, SPHERE, RAY }

        [SerializeField]
        private ProjectileType projectileType = ProjectileType.RAY;

        private LayerMask collisionMask;

        private void Awake()
        {
            collisionMask = LayerMask.GetMask("Character");
        }

        private void OnEnable()
        {
            distance = 0.0f;
        }

        protected override void OnAnimate(float deltaTime)
        {
            var deltaDistance = projectileSpeed * deltaTime;
            transform.position += deltaDistance * transform.forward;

            distance += deltaDistance;

            if (distance >= maxDistance)
            {
                Die();
            }
        }

        private readonly RaycastHit[] results = new RaycastHit[8];

        protected override bool Detect(out string[] gameObjectIds)
        {
            var delta = Time.deltaTime;

            List<string> candidatesList = new();

            gameObjectIds = null;

            var ray = new Ray(transform.position, transform.forward);
            var maxDistance = projectileSpeed * delta;
            var count = projectileType switch
            {
                ProjectileType.BOX => Physics.BoxCastNonAlloc(
                    transform.position,
                    Vector3.one * raycastRadius,
                    transform.forward,
                    results,
                    Quaternion.identity,
                    maxDistance),
                ProjectileType.SPHERE => Physics.SphereCastNonAlloc(
                    ray,
                    raycastRadius,
                    results,
                    maxDistance,
                    collisionMask),
                ProjectileType.RAY => Physics.RaycastNonAlloc(
                    ray, results,
                    maxDistance,
                    collisionMask),
                _ => 0,
            };

            if (count > 0)
            {
                for (var i = 0; i < count; i++)
                {
                    var hitInfo = results[i];
                    candidatesList.Add(hitInfo.collider.gameObject.GetInstanceID().ToString());
                }
            }
            else
            {
                return false;
            }

            gameObjectIds = candidatesList.ToArray();

            return gameObjectIds.Length > 0;
        }
    }
}
