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

        public class DodgeEvent : IEvent
        {

        }

        public class LookEvent : IEvent
        {
            public Vector2 mousePosition;
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

            inputActions.Player.Look.performed += Look;

            inputActions.Player.Dodge.performed += Dodge;
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
            inputActions.Player.Attack.performed -= Attack;
            inputActions.Player.Attack.canceled -= Attack;

            inputActions.Player.Move.performed -= Move;
            inputActions.Player.Move.canceled -= Move;

            inputActions.Player.Look.performed -= Look;

            inputActions.Player.Dodge.performed -= Dodge;
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

        private void Look(InputAction.CallbackContext ctx)
        {
            Reactive.events.OnNext(new LookEvent
            {
                gameObjectId = gameObjectId,
                mousePosition = ctx.ReadValue<Vector2>()
            });
        }

        private void Dodge(InputAction.CallbackContext ctx)
        {
            Reactive.events.OnNext(new DodgeEvent
            {
                gameObjectId = gameObjectId,
            });
        }
    }
}
