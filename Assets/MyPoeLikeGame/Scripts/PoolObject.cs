using UnityEngine;

namespace MyPoeLikeGame
{
    public class PoolObject : MonoBehaviour
    {
        private Transform sender;

        public Transform Sender
        {
            set { sender = value; }
            get => sender;
        }

        protected void Die()
        {
            gameObject.SetActive(false);

            Reactive.events.OnNext(
                new Pool.PoolEvent
                {
                    state = Pool.PoolEvent.PoolEventState.RECYCLE,
                    poolObject = this,
                    sender = sender,
                }
            );
        }
    }
}
