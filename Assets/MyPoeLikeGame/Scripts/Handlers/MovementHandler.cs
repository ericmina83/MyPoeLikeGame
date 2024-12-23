using R3;
using System;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
    [RequireComponent(typeof(CharacterController))]
    public class MovementHandler : MonoBehaviour
    {
        public class MovementEvent : IEvent
        {
            public Vector2 inputSpeed;
            public Vector3 speed;
        }

        private enum MovementState
        {
            MOVEMENT,
            DODGE
        }

        private CharacterController characterController;

        private Vector3 speed;

        private IDisposable subscription;

        private string gameObjectId;

        private bool canMove = true;

        [SerializeField]
        private float movementSpeedBasic = 1.0f;

        [SerializeField]
        private float movementSpeedIncrement = 0.0f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            gameObjectId = gameObject.GetInstanceID().ToString();
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            var observable = Reactive.events
                .Where(e => e.gameObjectId == gameObjectId);

            observable.OfType<IEvent, PlayerInputHandler.MovementEvent>()
                .Select(e => e.input).Subscribe((input) =>
                {
                    var inputSpeed = new Vector3(input.x, 0, input.y);
                    speed = movementSpeedBasic * (1.0f + movementSpeedIncrement) * inputSpeed;

                    Reactive.events.OnNext(new MovementEvent
                    {
                        inputSpeed = inputSpeed,
                        speed = speed,
                        gameObjectId = gameObjectId
                    });
                }).AddTo(ref builder);

            observable.OfType<IEvent, CharacterStateHandler.StateEvent>()
                .Select(e => e.state)
                .Subscribe(StateHandler).AddTo(ref builder);

            subscription = builder.Build();

            canMove = true;
        }

        private void StateHandler(CharacterStateHandler.CharacterState state)
        {
            if (state == CharacterStateHandler.CharacterState.DODGE)
            {
                canMove = false;
            }
            else
            {
                canMove = true;
            }
        }

        private void OnDisable()
        {
            subscription.Dispose();
        }

        private void Update()
        {
            if (!canMove)
                return;

            characterController.SimpleMove(speed);
        }
    }
}
