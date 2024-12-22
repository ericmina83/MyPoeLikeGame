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
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
            inputActions.Player.Attack.performed -= Attack;
            inputActions.Player.Attack.canceled -= Attack;
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
    }
}
