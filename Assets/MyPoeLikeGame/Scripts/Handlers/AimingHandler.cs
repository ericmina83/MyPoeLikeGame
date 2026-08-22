using R3;
using System;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
    public class AimingHandler : MonoBehaviour
    {
        public class AimingEvent
        {
            public bool aiming;
        }

        private IDisposable disposable;

        public readonly BehaviorSubject<Vector2> lookInput = new(Vector2.zero);

        private bool canAim = true;

        private CharacterStateHandler stateHandler;

        private void Awake()
        {
            stateHandler = GetComponent<CharacterStateHandler>();
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            stateHandler.stateObservable.Subscribe(StateHandler).AddTo(ref builder);

            disposable = builder.Build();

            canAim = true;
        }

        private void OnDisable()
        {
            disposable?.Dispose();
            disposable = null;
        }

        private void StateHandler(CharacterStateHandler.CharacterState state)
        {
            if (state == CharacterStateHandler.CharacterState.DODGE)
            {
                canAim = false;
            }
            else
            {
                canAim = true;
            }
        }

        private void Update()
        {
            if (!canAim)
                return;

            var lookAt = new Vector3(lookInput.Value.x, transform.position.y, lookInput.Value.y);

            transform.rotation = Quaternion.LookRotation(lookAt - transform.position);
        }
    }
}
