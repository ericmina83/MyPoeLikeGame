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

        public readonly Subject<Vector2> movementInput = new();
        public Observable<MovementData> MovementEvent => movementInput.Select((input) =>
        {
            return new MovementData
            {
                input = input,
                speed = movementSpeedBasic * (1.0f + movementSpeedIncrement) * new Vector3(input.x, 0, input.y),
            };
        });

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
