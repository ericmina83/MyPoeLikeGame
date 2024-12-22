using UnityEngine;
using UnityEngine.InputSystem;

namespace MyPoeLikeGame.Handlers
{
    public class PlayerInputHandler : MonoBehaviour
    {
        private PlayerInput inputActions;

        private string gameObjectId = null;

        private AttackEvent currentAttackEvent;

        public class AttackEvent : IEvent
        {
            public bool attacking;
        }

        public class MovementEvent : IEvent
        {
            public Vector2 input;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            inputActions = new();
            gameObjectId = gameObject.GetInstanceID().ToString();
        }


        private void OnEnable()
        {
            inputActions.Player.Attack.performed += Attack;
            inputActions.Player.Attack.canceled += Attack;

            inputActions.Player.Move.performed += Move;
            inputActions.Player.Move.canceled += Move;
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
            inputActions.Player.Attack.performed -= Attack;
            inputActions.Player.Attack.canceled -= Attack;

            inputActions.Player.Move.performed -= Move;
            inputActions.Player.Move.canceled -= Move;
        }

        private void Attack(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                if (currentAttackEvent != null)
                {
                    currentAttackEvent.attacking = false;
                }

                currentAttackEvent = new AttackEvent
                {
                    attacking = true,
                    gameObjectId = gameObjectId,
                };

                Reactive.events.OnNext(currentAttackEvent);
            }

            if (ctx.canceled)
            {
                currentAttackEvent.attacking = false;
            }
        }

        private void Move(InputAction.CallbackContext ctx)
        {
            var input = Vector2.zero;

            if (ctx.performed)
            {
                input = ctx.ReadValue<Vector2>();
            }

            Reactive.events.OnNext(new MovementEvent
            {
                gameObjectId = gameObjectId,
                input = input
            });
        }
    }
}
