using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyPoeLikeGame.Handlers
{
    public class PlayerInputHandler : MonoBehaviour
    {
        private PlayerInput inputActions;

        private readonly Subject<Vector2> lookInput = new();
        private readonly Subject<bool> dodgeInput = new();
        private readonly Subject<Vector2> movementInput = new();
        private readonly Subject<bool> attackInput = new();

        public Observable<Vector2> LookInput => lookInput.AsObservable();
        public Observable<bool> DodgeInput => dodgeInput.AsObservable();
        public Observable<Vector2> MovementInput => movementInput.AsObservable();
        public Observable<bool> AttackInput => attackInput.AsObservable();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            inputActions = new();
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
                attackInput.OnNext(true);
            }

            if (ctx.canceled)
            {
                attackInput.OnNext(false);
            }
        }

        private void Move(InputAction.CallbackContext ctx)
        {
            var input = Vector2.zero;

            if (ctx.performed)
            {
                input = ctx.ReadValue<Vector2>();
            }

            movementInput.OnNext(input);
        }

        private void Look(InputAction.CallbackContext ctx)
        {
            lookInput.OnNext(ctx.ReadValue<Vector2>());
        }

        private void Dodge(InputAction.CallbackContext ctx)
        {
            dodgeInput.OnNext(true);
        }
    }
}
