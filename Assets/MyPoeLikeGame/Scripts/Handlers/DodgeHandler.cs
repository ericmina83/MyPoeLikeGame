using System;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(MovementHandler))]
    public class DodgeHandler : MonoBehaviour
    {
        public enum DodgeState { DODGING, END }

        public class DodgeEvent
        {
            public bool dodging;
            public float percentage;
            public Vector3 dodgeDirection;
        }

        [SerializeField]
        private float dodgeDistanceBasic = 3f;

        [SerializeField]
        private float dodgeDistanceIncrement = 0.0f;

        [SerializeField]
        private float dodgePeriod = 0.1f;

        private float time = 0.0f;

        private Vector3? dodgeDirection = null;
        private IDisposable disposable;

        private CharacterController characterController;
        private MovementHandler movementHandler;

        /// <summary>
        /// 只用來「回報」，不訂閱它。相依是單向的：DodgeHandler → CharacterStateHandler。
        /// 中轉站不認識這個類別，所以不會有參照環。
        /// </summary>
        private CharacterStateHandler stateHandler;

        public readonly Subject<DodgeEvent> dodgeEvent = new();
        public readonly Subject<bool> dodgeInput = new();

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            movementHandler = GetComponent<MovementHandler>();
            stateHandler = GetComponent<CharacterStateHandler>();
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            // Subscribe to the movement input only when dodgeInput emits
            dodgeInput
                .WithLatestFrom(movementHandler.movementInput, (input, direction) => direction)
                .Subscribe(direction =>
                {
                    if (dodgeDirection != null)
                        return;

                    // 起手條件屬於這個 Handler 自己，不是跟中轉站要許可。
                    // 之後若要加「暈眩時不能翻滾」，形狀是在這裡讀狀況、自己組條件：
                    //     if (stateHandler.IsIncapacitated.CurrentValue) return;
                    // 「我已經在翻滾」用上面的 dodgeDirection 判斷就好——用自己的欄位，
                    // 就天然排除了自己回報進中轉站的那份貢獻，不會自我否定。

                    BeginDodge(!Mathf.Approximately(direction.magnitude, 0.0f)
                        ? new Vector3(direction.x, 0.0f, direction.y)
                        : -transform.forward);
                }).AddTo(ref builder);


            disposable = builder.Build();
        }

        private void OnDisable()
        {
            disposable?.Dispose();
            disposable = null;
        }

        // ── 狀態轉換集中在這兩個方法 ────────────────────────────────────
        // 把「改自己的狀態」和「回報給中轉站」綁在同一處，是這份副本不會漂移的原因：
        // 只有一個寫入者，而且寫入與轉換同地。散在各處呼叫 ReportDodging 才會出事。

        private void BeginDodge(Vector3 direction)
        {
            dodgeDirection = direction.normalized;
            time = 0.0f;
            stateHandler.ReportDodging(true);
        }

        private void EndDodge()
        {
            dodgeDirection = null;
            stateHandler.ReportDodging(false);
        }

        private void Update()
        {
            if (dodgeDirection == null)
                return;

            time += Time.deltaTime;
            var dodgeSpeed = dodgeDistanceBasic * (1.0f + dodgeDistanceIncrement) / dodgePeriod;
            characterController.SimpleMove(dodgeSpeed * dodgeDirection.Value);

            transform.rotation = Quaternion.LookRotation(dodgeDirection.Value);

            var percentage = time / dodgePeriod;

            if (percentage > 1.0f) // end of dodge
            {
                dodgeEvent.OnNext(new DodgeEvent
                {
                    dodging = false,
                    percentage = percentage,
                    dodgeDirection = dodgeDirection.Value,
                });

                EndDodge();
            }
            else // dodging
            {
                dodgeEvent.OnNext(new DodgeEvent
                {
                    dodging = true,
                    percentage = percentage,
                    dodgeDirection = dodgeDirection.Value,
                });
            }
        }
    }
}
