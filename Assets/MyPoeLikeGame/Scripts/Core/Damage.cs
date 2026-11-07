using UnityEngine;

namespace MyPoeLikeGame
{
    public abstract class Damage : PoolObject
    {
        [SerializeField]
        private int damageValue;

        public int DamageValue => damageValue;

        public class DamageEvent
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
                    // TODO: Reactive was removed; wire this up via the new R3-based event pattern
                    // (see DamageHandler.cs, HpHandler.cs for other pending pieces of this chain).
                    // damageEvent.OnNext(new DamageEvent()
                    // {
                    //     gameObjectId = gameObjectId,
                    //     damage = this,
                    // });
                }
                Die();
            }
        }

        protected abstract void OnAnimate(float deltaTime);

        protected abstract bool Detect(out string[] gameObjectIds);
    }
}
