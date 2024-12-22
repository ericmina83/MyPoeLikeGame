using UnityEngine;

namespace MyPoeLikeGame
{

    [CreateAssetMenu(fileName = "Skill", menuName = "Scriptable Objects/Skill")]
    public class Skill : ScriptableObject
    {
        public string skillName = "New Skill";

        public Projectile projectilePrefab;

        public Vector3 offset = new(0, 1, 1);

        public Vector3 direction = new(0, 0, 1);


        public float attackFrequency = 1f;

        public float overdrawPeriod = 1f;

        public void Fire(Transform sender)
        {
            Reactive.events.OnNext(new Pool.PoolEvent
            {
                state = Pool.PoolEvent.PoolEventState.FIRE,
                poolObject = projectilePrefab,
                direction = direction,
                offset = offset,
                sender = sender,
            });
        }
    }
}
