using System;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{

    public class HpHandler : MonoBehaviour
    {
        [SerializeField]
        private int hp;

        private Observable<IEvent> observable;

        private IDisposable disposable;

        private string gameObjectId;

        public class DeathEvent : IEvent
        {

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
                .OfType<IEvent, DamageHandler.DamageEvent>()
                .Select(e => e.damageValue)
                .Subscribe(RenewHp)
                .AddTo(ref builder);

            disposable = builder.Build();
        }

        private void OnDisable()
        {
            disposable.Dispose();
        }

        private void RenewHp(int delta)
        {
            if (hp + delta <= 0)
            {
                hp = 0;
                Reactive.events.OnNext(new DeathEvent
                {
                    gameObjectId = gameObjectId
                });
            }
            else
            {
                hp += delta;
            }
        }
    }
}