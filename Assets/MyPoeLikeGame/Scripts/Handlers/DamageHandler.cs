using System;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
    public class DamageHandler : MonoBehaviour
    {
        private string gameObjectId;

        private Observable<IEvent> observable;

        private IDisposable disposable;

        public class DamageEvent : IEvent
        {
            public int damageValue;
        }

        private void Awake()
        {
            gameObjectId = gameObject.GetInstanceID().ToString();

            observable = Reactive.events.Where(e => e.gameObjectId == gameObjectId);
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            observable
                .OfType<IEvent, Damage.DamageEvent>()
                .Subscribe(e => TakeDamage(e.damage))
                .AddTo(ref builder);

            disposable = builder.Build();
        }

        private void OnDisable()
        {
            disposable.Dispose();
        }

        private void TakeDamage(Damage damage)
        {
            var damageValue = damage.DamageValue;


            Reactive.events.OnNext(new DamageEvent
            {
                gameObjectId = gameObjectId,
                damageValue = damageValue
            });
        }
    }
}