using R3;
using System;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CharacterStateHandler))]
    public class MovementHandler : MonoBehaviour
    {
        public struct MovementData
        {
            public Vector2 input;
            public Vector3 speed;
        }

        private CharacterController characterController;

        private Vector3 speed;

        private IDisposable subscription;

        private bool isBusy = false;

        private CharacterStateHandler stateHandler;

        [SerializeField]
        private float movementSpeedBasic = 1.75f;

        [SerializeField]
        private float movementSpeedIncrement = 0.0f;

        private readonly Subject<Vector2> movementInput = new();

        /// <summary>輸出埠：移動意圖與換算後的速度。誰想知道角色在動就訂閱這個。</summary>
        public Observable<MovementData> MovementEvent { get; }

        public MovementHandler()
        {
            // 建好一次存住，不要讓 MovementEvent 變成每次讀取都重建一條 Select 鏈
            // （原本是 expression-bodied property，DodgeHandler 和自己的 OnEnable
            // 各讀一次，就變成兩條互不相干的鏈，白白多算兩次）。
            // 放構子安全：movementSpeedBasic/Increment 是 [SerializeField]，
            // 這個 lambda 是閉包讀 this 的欄位，實際讀值在 movementInput 觸發當下，
            // 那時 Unity 早就把 Inspector 的值反序列化進來了。
            MovementEvent = movementInput.Select(input => new MovementData
            {
                input = input,
                speed = movementSpeedBasic * (1.0f + movementSpeedIncrement) * new Vector3(input.x, 0, input.y),
            });
        }

        /// <summary>輸入埠：Controller 把移動意圖推進來。</summary>
        public void SetMoveInput(Vector2 direction) => movementInput.OnNext(direction);

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            stateHandler = GetComponent<CharacterStateHandler>();
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            MovementEvent.Subscribe((evn) => speed = evn.speed).AddTo(ref builder);

            // 訂閱匯總值，不訂閱個別 flag：加新狀況（受擊硬直、死亡）時，
            // 改的是 CharacterStateHandler 的推導式，這裡一行都不用動。
            // 「busy 代表不能移動」是這個 Handler 自己的詮釋，中轉站沒有規定。
            stateHandler.IsBusy.Subscribe(v => isBusy = v).AddTo(ref builder);

            subscription = builder.Build();
        }

        private void OnDisable()
        {
            subscription?.Dispose();
            subscription = null;
        }

        private void Update()
        {
            if (isBusy)
                return;

            characterController.SimpleMove(speed);
        }
    }
}
