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

        public class DodgeEvent : IEvent
        {
            public enum DodgeState { DODGING, END }
            public DodgeState dodgeState;
            public float percentage;
            public Vector3 dodgeDirection;
        }

        private enum MovementState
        {
            MOVEMENT,
            DODGE
        }

        private MovementState state = MovementState.MOVEMENT;

        private CharacterController characterController;

        private Vector3 speed;

        private IDisposable subscription;

        private string gameObjectId;

        [SerializeField]
        private float movementSpeedBasic = 1.0f;

        [SerializeField]
        private float movementSpeedIncrement = 0.0f;

        [SerializeField]
        private float dodgeDistanceBasic = 1.5f;

        [SerializeField]
        private float dodgeDistanceIncrement = 0.0f;

        [SerializeField]
        private float dodgePeriod = 0.2f;

        private float time = 0.0f;

        private Vector3 dodgeDirection = Vector3.zero;

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

            // Dodge
            observable.OfType<IEvent, PlayerInputHandler.DodgeEvent>()
                .Subscribe(_ =>
                {
                    if (state == MovementState.MOVEMENT)
                    {
                        state = MovementState.DODGE;
                        time = 0.0f;

                        if (!Mathf.Approximately(speed.magnitude, 0.0f))
                        {
                            dodgeDirection = speed.normalized;
                        }
                        else
                        {
                            dodgeDirection = -transform.forward;
                        }
                    }
                }).AddTo(ref builder);

            subscription = builder.Build();

            state = MovementState.MOVEMENT;
        }

        private void OnDisable()
        {
            subscription.Dispose();
        }

        private void Update()
        {
            if (state == MovementState.MOVEMENT)
            {
                characterController.SimpleMove(speed);
            }
            else if (state == MovementState.DODGE)
            {
                time += Time.deltaTime;
                var dodgeSpeed = dodgeDistanceBasic * (1.0f + dodgeDistanceIncrement) / dodgePeriod;
                characterController.SimpleMove(dodgeSpeed * dodgeDirection);

                var percentage = time / dodgePeriod;

                if (percentage > 1.0f) // end of dodge
                {
                    state = MovementState.MOVEMENT;

                    Reactive.events.OnNext(new DodgeEvent
                    {
                        dodgeState = DodgeEvent.DodgeState.END,
                        percentage = percentage,
                        dodgeDirection = dodgeDirection,
                        gameObjectId = gameObjectId
                    });
                }
                else // dodging
                {
                    Reactive.events.OnNext(new DodgeEvent
                    {
                        dodgeState = DodgeEvent.DodgeState.DODGING,
                        percentage = percentage,
                        dodgeDirection = dodgeDirection,
                        gameObjectId = gameObjectId
                    });
                }
            }
        }
    }
}
