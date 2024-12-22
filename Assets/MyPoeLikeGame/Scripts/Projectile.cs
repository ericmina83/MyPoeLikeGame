namespace MyPoeLikeGame
{
    public class Projectile : Damage
    {
        public float projectileSpeed = 1f;

        public float maxDistance = 10f;

        private float distance = 0.0f;

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
    }
}
