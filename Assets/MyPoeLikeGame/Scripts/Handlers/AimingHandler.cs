using R3;
using System;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
    public class AimingHandler : MonoBehaviour
    {
        public class AimingEvent
        {
            public bool aiming;
        }

        private IDisposable disposable;

        private readonly BehaviorSubject<Vector2> lookInput = new(Vector2.zero);

        private bool isBusy = false;

        private CharacterStateHandler stateHandler;

        /// <summary>輸入埠：Controller 把準心指向的世界座標推進來。</summary>
        public void SetLookTarget(Vector2 target) => lookInput.OnNext(target);

        private void Awake()
        {
            stateHandler = GetComponent<CharacterStateHandler>();
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            // 注意：瞄準之後很可能會和移動／攻擊分家——拉弓時不能移動但**必須能瞄準**。
            // 因為中轉站只陳述狀況、不下許可，那天到來時改的是這裡的詮釋
            // （例如 isDodging 擋瞄準、isAttacking 不擋），中轉站不必動。
            stateHandler.IsBusy.Subscribe(v => isBusy = v).AddTo(ref builder);

            disposable = builder.Build();
        }

        private void OnDisable()
        {
            disposable?.Dispose();
            disposable = null;
        }

        private void Update()
        {
            if (isBusy)
                return;

            var lookAt = new Vector3(lookInput.Value.x, transform.position.y, lookInput.Value.y);

            transform.rotation = Quaternion.LookRotation(lookAt - transform.position);
        }
    }
}
