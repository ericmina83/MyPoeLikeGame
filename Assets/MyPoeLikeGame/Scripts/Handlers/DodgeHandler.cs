using System;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
    [RequireComponent(typeof(CharacterController))]
    public class DodgeHandler : MonoBehaviour
    {
        public class DodgeEvent : IEvent
        {
            public enum DodgeState { DODGING, END }
            public bool dodging;
            public float percentage;
            public Vector3 dodgeDirection;
        }

        [SerializeField]
        private float dodgeDistanceBasic = 3f;

        [SerializeField]
        private float dodgeDistanceIncrement = 0.0f;

        [SerializeField]
        private float dodgePeriod = 0.75f;

        private float time = 0.0f;

        private string gameObjectId = null;

        private Vector2 inputDirection = Vector2.zero;
        private Vector3 dodgeDirection;
        private IDisposable disposable;
        private bool dodging = false;

        private CharacterController characterController;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            gameObjectId = gameObject.GetInstanceID().ToString();

            var observable = Reactive.events
                .Where(e => e.gameObjectId == gameObjectId);

            var builder = Disposable.CreateBuilder();

            observable
                .OfType<IEvent, PlayerInputHandler.MovementEvent>()
                .Subscribe(e =>
                {
                    inputDirection = e.input;
                }).AddTo(ref builder);

            // Dodge
            observable
                .OfType<IEvent, PlayerInputHandler.DodgeEvent>()
                .Subscribe(_ =>
                {
                    if (dodging)
                        return;

                    if (!Mathf.Approximately(inputDirection.magnitude, 0.0f))
                    {
                        dodgeDirection = new Vector3(inputDirection.x, 0.0f, inputDirection.y);
                    }
                    else
                    {
                        dodgeDirection = -transform.forward;
                    }

                    time = 0.0f;
                    dodging = true;
                }).AddTo(ref builder);

            disposable = builder.Build();

            dodging = false;
        }

        private void OnDisable()
        {
            disposable.Dispose();
        }

        private void Update()
        {
            if (!dodging)
                return;

            time += Time.deltaTime;
            var dodgeSpeed = dodgeDistanceBasic * (1.0f + dodgeDistanceIncrement) / dodgePeriod;
            characterController.SimpleMove(dodgeSpeed * dodgeDirection);

            transform.rotation = Quaternion.LookRotation(dodgeDirection);

            var percentage = time / dodgePeriod;

            if (percentage > 1.0f) // end of dodge
            {
                dodging = false;

                Reactive.events.OnNext(new DodgeEvent
                {
                    dodging = false,
                    percentage = percentage,
                    dodgeDirection = dodgeDirection,
                    gameObjectId = gameObjectId
                });
            }
            else // dodging
            {
                Reactive.events.OnNext(new DodgeEvent
                {
                    dodging = true,
                    percentage = percentage,
                    dodgeDirection = dodgeDirection,
                    gameObjectId = gameObjectId
                });
            }
        }
    }
}
