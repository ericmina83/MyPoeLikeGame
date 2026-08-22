using System;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(MovementHandler))]
    public class DodgeHandler : MonoBehaviour
    {
        public enum DodgeState { DODGING, END }

        public class DodgeEvent
        {
            public bool dodging;
            public float percentage;
            public Vector3 dodgeDirection;
        }

        [SerializeField]
        private float dodgeDistanceBasic = 3f;

        [SerializeField]
        private float dodgeDistanceIncrement = 0.0f;

        [SerializeField]
        private float dodgePeriod = 0.1f;

        private float time = 0.0f;

        private Vector3? dodgeDirection = null;
        private IDisposable disposable;

        private CharacterController characterController;
        private MovementHandler movementHandler;

        public readonly Subject<DodgeEvent> dodgeEvent = new();
        public readonly Subject<bool> dodgeInput = new();

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            movementHandler = GetComponent<MovementHandler>();
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            // Subscribe to the movement input only when dodgeInput emits
            dodgeInput
                .WithLatestFrom(movementHandler.movementInput, (input, direction) => direction)
                .Subscribe(direction =>
                {
                    if (dodgeDirection != null)
                        return;

                    if (!Mathf.Approximately(direction.magnitude, 0.0f))
                    {
                        dodgeDirection = new Vector3(direction.x, 0.0f, direction.y);
                    }
                    else
                    {
                        dodgeDirection = -transform.forward;
                    }

                    time = 0.0f; // Reset time for the new dodge
                    dodgeDirection?.Normalize();
                }).AddTo(ref builder);


            disposable = builder.Build();
        }

        private void OnDisable()
        {
            disposable?.Dispose();
            disposable = null;
        }

        private void Update()
        {
            if (dodgeDirection == null)
                return;

            time += Time.deltaTime;
            var dodgeSpeed = dodgeDistanceBasic * (1.0f + dodgeDistanceIncrement) / dodgePeriod;
            characterController.SimpleMove(dodgeSpeed * dodgeDirection.Value);

            transform.rotation = Quaternion.LookRotation(dodgeDirection.Value);

            var percentage = time / dodgePeriod;

            if (percentage > 1.0f) // end of dodge
            {
                dodgeEvent.OnNext(new DodgeEvent
                {
                    dodging = false,
                    percentage = percentage,
                    dodgeDirection = dodgeDirection.Value,
                });

                dodgeDirection = null;
            }
            else // dodging
            {
                dodgeEvent.OnNext(new DodgeEvent
                {
                    dodging = true,
                    percentage = percentage,
                    dodgeDirection = dodgeDirection.Value,
                });
            }
        }
    }
}
