using System;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
    public class DamageHandler : MonoBehaviour
    {

        private IDisposable disposable;

        public class DamageEvent
        {
            public int damageValue;
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            // observable
            //     .OfType<IEvent, Damage.DamageEvent>()
            //     .Subscribe(e => TakeDamage(e.damage))
            //     .AddTo(ref builder);

            disposable = builder.Build();
        }

        private void OnDisable()
        {
            disposable?.Dispose();
            disposable = null;
        }

        private void TakeDamage(Damage damage)
        {
            var damageValue = damage.DamageValue;

            // Reactive.events.OnNext(new DamageEvent
            // {
            //     gameObjectId = gameObjectId,
            //     damageValue = damageValue
            // });
        }
    }
}