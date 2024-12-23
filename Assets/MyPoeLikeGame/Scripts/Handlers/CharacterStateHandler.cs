using R3;
using System;
using UnityEngine;


namespace MyPoeLikeGame.Handlers
{
    public class CharacterStateHandler : MonoBehaviour
    {
        public enum CharacterState
        {
            NONE,
            IDLE,
            DODGE,
        }

        public class StateEvent : IEvent
        {
            public CharacterState state;
        }

        private CharacterState state = CharacterState.IDLE;

        private string gameObjectId;

        private IDisposable disposable;

        private void Awake()
        {
            gameObjectId = gameObject.GetInstanceID().ToString();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void OnEnable()
        {
            var observable = Reactive.events.Where(e => e.gameObjectId == gameObjectId);

            var builder = Disposable.CreateBuilder();

            observable
                .OfType<IEvent, DodgeHandler.DodgeEvent>()
                .Subscribe(e =>
                {
                    var dodging = e.dodging;
                    if (state == CharacterState.IDLE) // The state can dodge
                    {
                        if (dodging)
                        {
                            SetState(CharacterState.DODGE);
                        }
                    }
                    else if (state == CharacterState.DODGE)
                    {
                        if (!dodging)
                        {
                            SetState(CharacterState.IDLE);
                        }
                    }
                }).AddTo(ref builder);

            disposable = builder.Build();
        }

        private void OnDisable()
        {
            disposable.Dispose();
        }

        private void SetState(CharacterState nextState)
        {
            if (state != nextState)
            {
                state = nextState;

                Reactive.events.OnNext(new StateEvent
                {
                    gameObjectId = gameObjectId,
                    state = state
                });
            }
        }
    }
}
