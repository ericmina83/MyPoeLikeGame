using R3;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyPoeLikeGame.Handlers
{
    [RequireComponent(typeof(CharacterController))]
    public class MovementHandler : MonoBehaviour
    {
        public class MovementEvent : IEvent
        {
            public Vector3 speed;
        }

        PlayerInput inputAction;

        private CharacterController characterController;

        private Vector3 speed;

        private IDisposable subscription;

        private string GameObjectId => gameObject.GetInstanceID().ToString();

        private void Awake()
        {
            inputAction = new();
            characterController = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            inputAction.Player.Move.performed += Move;
            inputAction.Player.Move.canceled += Move;
            inputAction.Enable();

            subscription = Reactive.events
                .Where(e => e.gameObjectId == GameObjectId)
                .OfType<IEvent, MovementEvent>()
                .Select(e => e.speed).Subscribe((speed) =>
                {
                    this.speed = speed;
                });
        }

        private void OnDisable()
        {
            inputAction.Disable();
            inputAction.Player.Move.performed -= Move;
            inputAction.Player.Move.canceled -= Move;

            subscription.Dispose();
        }

        private void Update()
        {
            characterController.SimpleMove(speed);
        }

        private void Move(InputAction.CallbackContext ctx)
        {
            var vec2 = ctx.ReadValue<Vector2>();
            var speed = new Vector3(vec2.x, 0, vec2.y);

            if (ctx.canceled)
            {
                speed = Vector3.zero;
            }

            Reactive.events.OnNext(new MovementEvent
            {
                gameObjectId = GameObjectId,
                speed = speed
            });
        }
    }
}
