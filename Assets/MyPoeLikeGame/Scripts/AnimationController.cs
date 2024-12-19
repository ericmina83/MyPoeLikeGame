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
        private int upperLayerIdx = -1;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            gameObjectId = gameObject.GetInstanceID().ToString();
            upperLayerIdx = animator.GetLayerIndex("Upper");
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

            observable.OfType<IEvent, AttackController.AttackEvent>()
                .Subscribe(Attack)
                .AddTo(ref builder);

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

            animator.SetFloat("Speed X", localSpeed.x, 0.1f, Time.deltaTime);
            animator.SetFloat("Speed Y", localSpeed.z, 0.1f, Time.deltaTime);
        }

        private void Attack(AttackController.AttackEvent e)
        {
            var percentage = e.percentage;
            var state = e.attackState;

            switch (state)
            {
                case AttackController.AttackState.DRAW:
                    animator.Play("Draw", upperLayerIdx, Mathf.Min(percentage, 1.0f));
                    break;
                case AttackController.AttackState.OVERDRAW:
                    animator.Play("Overdraw", upperLayerIdx, Mathf.Min(percentage, 1.0f));
                    break;
                case AttackController.AttackState.FIRE:
                    animator.Play("Withdraw", upperLayerIdx, Mathf.Min(percentage, 1.0f));
                    break;
                case AttackController.AttackState.WITHDRAW:
                    animator.Play("Withdraw", upperLayerIdx, Mathf.Min(percentage, 1.0f));
                    break;
                case AttackController.AttackState.IDLE:
                    animator.Play("Idle", upperLayerIdx);
                    break;
            }



            switch (state)
            {
                case AttackController.AttackState.DRAW:
                case AttackController.AttackState.OVERDRAW:
                    animator.SetBool("Aiming", true);
                    break;
                default:
                    animator.SetBool("Aiming", false);
                    break;
            }
        }
    }
}
