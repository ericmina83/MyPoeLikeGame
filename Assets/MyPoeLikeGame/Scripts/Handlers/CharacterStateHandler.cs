using R3;
using System;
using UnityEngine;


namespace MyPoeLikeGame.Handlers
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(DodgeHandler))]
    public class CharacterStateHandler : MonoBehaviour
    {
        public enum CharacterState
        {
            NONE,
            IDLE,
            DODGE,
        }

        public readonly BehaviorSubject<CharacterState> stateObservable = new(CharacterState.IDLE); // The current state of the character

        private CharacterState state = CharacterState.IDLE;

        private IDisposable disposable;

        private DodgeHandler dodgeHandler;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Awake()
        {
            dodgeHandler = GetComponent<DodgeHandler>();
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            dodgeHandler.dodgeEvent
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
            disposable?.Dispose();
            disposable = null;
        }

        private void SetState(CharacterState nextState)
        {
            if (state != nextState)
            {
                state = nextState;

                stateObservable.OnNext(nextState);
            }
        }
    }
}
