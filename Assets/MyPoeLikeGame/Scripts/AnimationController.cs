using System;
using R3;
using UnityEngine;

namespace MyPoeLikeGame
{
    [RequireComponent(typeof(Animator))]
    public class AnimationController : MonoBehaviour
    {
        private IDisposable disposable = null;

        private string gameObjectId;

        private Animator animator = null;

        private Vector3 speed = Vector3.zero;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            gameObjectId = gameObject.GetInstanceID().ToString();
        }

        private void OnEnable()
        {
            var observable = Reactive.events.Where(e => e.gameObjectId == gameObjectId);

            var builder = new DisposableBuilder();

            observable.OfType<IEvent, MovementController.MovementEvent>()
                .Select(e => e.speed)
                .Subscribe(speed =>
                {
                    this.speed = speed;
                })
                .AddTo(ref builder);

            observable.OfType<IEvent, AimingController.AimingEvent>()
                .Select(e => e.aiming)
                .Subscribe(aiming =>
                {
                    animator.SetBool("Aiming", aiming);
                }).AddTo(ref builder);

            disposable = builder.Build();
        }

        private void OnDisable()
        {
            if (disposable == null)
                return;

            disposable.Dispose();
        }

        private void Update()
        {
            SetAnimatorSpeed(speed);
        }

        private void SetAnimatorSpeed(Vector3 speed)
        {
            var localSpeed = transform.worldToLocalMatrix.MultiplyVector(speed);

            animator.SetFloat("Speed X", localSpeed.x);
            animator.SetFloat("Speed Y", localSpeed.z);
        }
    }
}
