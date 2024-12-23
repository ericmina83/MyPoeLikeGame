using System;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
    [RequireComponent(typeof(Animator))]
    public class AnimationHandler : MonoBehaviour
    {
        private IDisposable disposable = null;

        private string gameObjectId;

        private Animator animator = null;

        private Vector3 speed = Vector3.zero;
        private int upperLayerIdx = -1;
        private int dodgeLayerIdx = -1;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            gameObjectId = gameObject.GetInstanceID().ToString();
            upperLayerIdx = animator.GetLayerIndex("Upper");
            dodgeLayerIdx = animator.GetLayerIndex("Dodge");
        }

        private void OnEnable()
        {
            var observable = Reactive.events.Where(e => e.gameObjectId == gameObjectId);

            var builder = Disposable.CreateBuilder();

            observable.OfType<IEvent, MovementHandler.MovementEvent>()
                .Select(e => e.speed)
                .Subscribe(speed => this.speed = speed)
                .AddTo(ref builder);

            observable.OfType<IEvent, MovementHandler.DodgeEvent>()
                .Subscribe(Dodge)
                .AddTo(ref builder);

            observable.OfType<IEvent, AttackHandler.AttackEvent>()
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

        private void Attack(AttackHandler.AttackEvent e)
        {
            var percentage = e.percentage;
            var state = e.attackState;

            switch (state)
            {
                case AttackHandler.AttackState.DRAW:
                    animator.Play("Draw", upperLayerIdx, Mathf.Min(percentage, 1.0f));
                    break;
                case AttackHandler.AttackState.OVERDRAW:
                    animator.Play("Overdraw", upperLayerIdx, Mathf.Min(percentage, 1.0f));
                    break;
                case AttackHandler.AttackState.FIRE:
                    animator.Play("Withdraw", upperLayerIdx, Mathf.Min(percentage, 1.0f));
                    break;
                case AttackHandler.AttackState.WITHDRAW:
                    animator.Play("Withdraw", upperLayerIdx, Mathf.Min(percentage, 1.0f));
                    break;
                case AttackHandler.AttackState.IDLE:
                    animator.Play("Idle", upperLayerIdx);
                    break;
            }



            switch (state)
            {
                case AttackHandler.AttackState.DRAW:
                case AttackHandler.AttackState.OVERDRAW:
                    animator.SetBool("Aiming", true);
                    break;
                default:
                    animator.SetBool("Aiming", false);
                    break;
            }
        }

        private void Dodge(MovementHandler.DodgeEvent e)
        {
            var dodgeState = e.dodgeState;

            if (dodgeState == MovementHandler.DodgeEvent.DodgeState.DODGING)
            {
                var dodgeDirection = transform.worldToLocalMatrix.MultiplyVector(e.dodgeDirection);
                animator.SetLayerWeight(dodgeLayerIdx, Mathf.Clamp01(e.percentage / 0.2f));
                animator.Play("Dodge", dodgeLayerIdx, e.percentage);
                animator.SetFloat("Dodge X", dodgeDirection.x);
                animator.SetFloat("Dodge Y", dodgeDirection.z);
            }
            else
            {
                animator.SetLayerWeight(dodgeLayerIdx, 0.0f);
                animator.Play("Idle", dodgeLayerIdx);
            }
        }
    }
}
