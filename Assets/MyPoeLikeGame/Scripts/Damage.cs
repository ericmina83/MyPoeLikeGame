using UnityEngine;

namespace MyPoeLikeGame
{
    public class Damage : PoolObject
    {
        private void Update()
        {
            DoDamage();
            OnAnimate(Time.deltaTime);
        }

        protected virtual void OnAnimate(float deltaTime)
        {

        }

        protected virtual void DoDamage()
        {

        }
    }
}
