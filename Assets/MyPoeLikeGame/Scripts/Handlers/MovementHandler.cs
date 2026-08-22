using R3;
using System;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CharacterStateHandler))]
    public class MovementHandler : MonoBehaviour
    {
        public struct MovementData
        {
            public Vector2 input;
            public Vector3 speed;
        }

        private CharacterController characterController;

        private Vector3 speed;

        private IDisposable subscription;

        private bool canMove = true;

        private CharacterStateHandler stateHandler;

        [SerializeField]
        private float movementSpeedBasic = 1.75f;

        [SerializeField]
        private float movementSpeedIncrement = 0.0f;

        public readonly Subject<Vector2> movementInput = new();
        public Observable<MovementData> MovementEvent => movementInput.Select((input) =>
        {
            return new MovementData
            {
                input = input,
                speed = movementSpeedBasic * (1.0f + movementSpeedIncrement) * new Vector3(input.x, 0, input.y),
            };
        });

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            stateHandler = GetComponent<CharacterStateHandler>();
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            MovementEvent.Subscribe((evn) => speed = evn.speed).AddTo(ref builder);

            stateHandler.stateObservable.Subscribe(StateHandler).AddTo(ref builder);

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
            subscription?.Dispose();
            subscription = null;
        }

        private void Update()
        {
            if (!canMove)
                return;

            characterController.SimpleMove(speed);
        }
    }
}
