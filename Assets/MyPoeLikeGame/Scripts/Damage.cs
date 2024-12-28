using UnityEngine;

namespace MyPoeLikeGame
{
    public abstract class Damage : PoolObject
    {
        [SerializeField]
        private int damageValue;

        public int DamageValue => damageValue;

        public class DamageEvent : IEvent
        {
            public Damage damage;
        }

        private void Update()
        {
            OnAnimate(Time.deltaTime);

            if (Detect(out var gameObjectIds))
            {
                foreach (var gameObjectId in gameObjectIds)
                {
                    Reactive.events.OnNext(new DamageEvent()
                    {
                        gameObjectId = gameObjectId,
                        damage = this,
                    });
                }
                Die();
            }
        }

        protected abstract void OnAnimate(float deltaTime);

        protected abstract bool Detect(out string[] gameObjectIds);
    }
}
