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

        private void Awake()
        {
            // 不能改成欄位初始化——生成的 PlayerInput 在建構子裡呼叫
            // ScriptableObject.CreateInstance，Unity 明確禁止在 MonoBehaviour 的
            // 建構子或欄位初始化中這麼做（UnityException:
            // CreateScriptableObjectInstanceFromType is not allowed ...）。
            inputActions = new();

            // 回呼註冊描述的是「這個物件是什麼」，屬於建立階段；
            // OnEnable / OnDisable 只管「現在活不活躍」。
            // 停用期間註冊仍掛著，但 Disable() 之後 action 不會觸發，行為等價。
            inputActions.Player.Attack.performed += Attack;
            inputActions.Player.Attack.canceled += Attack;

            inputActions.Player.Move.performed += Move;
            inputActions.Player.Move.canceled += Move;

            inputActions.Player.Look.performed += Look;

            inputActions.Player.Dodge.performed += Dodge;
        }

        private void OnEnable()
        {
            inputActions.Enable();
        }

        private void OnDisable()
        {
            // 防呆是為了讓 Awake 自己拋例外時，原始錯誤不會被這裡的 NRE 蓋掉。
            inputActions?.Disable();
        }

        private void OnDestroy()
        {
            // 生成的 PlayerInput 持有一個 InputActionAsset，不釋放會留下孤兒。
            // 註冊的 delegate 隨它一起消失，不需要逐一 -=。
            inputActions?.Dispose();
            inputActions = null;
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
