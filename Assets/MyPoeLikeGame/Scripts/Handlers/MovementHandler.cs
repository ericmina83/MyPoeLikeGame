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
            public Vector3 speed;
        }

        private CharacterController characterController;

        private Vector3 speed;

        private IDisposable subscription;

        private string gameObjectId;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            gameObjectId = gameObject.GetInstanceID().ToString();
        }

        private void OnEnable()
        {
            subscription = Reactive.events
                .Where(e => e.gameObjectId == gameObjectId)
                .OfType<IEvent, PlayerInputHandler.MovementEvent>()
                .Select(e => e.input).Subscribe((input) =>
                {
                    speed = new Vector3(input.x, 0, input.y);

                    Reactive.events.OnNext(new MovementEvent
                    {
                        speed = speed,
                        gameObjectId = gameObjectId
                    });
                });
        }

        private void OnDisable()
        {
            subscription.Dispose();
        }

        private void Update()
        {
            characterController.SimpleMove(speed);
        }
    }
}
