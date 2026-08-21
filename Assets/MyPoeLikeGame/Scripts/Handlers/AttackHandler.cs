using System;
using MyPoeLikeGame.Combat;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
    /// <summary>
    /// 攻擊的四階段狀態機。只負責推進進度與決定轉換；
    /// 「這種攻擊實際上會發生什麼事」屬於 <see cref="AttackBehaviour"/>。
    /// 設計說明見 AttackStateMachine.md。
    /// </summary>
    [RequireComponent(typeof(CharacterStateHandler))]
    public class AttackHandler : MonoBehaviour
    {
        public enum AttackState
        {
            NONE,
            IDLE,
            DRAW,
            OVERDRAW,
            FIRE,
            WITHDRAW
        }

        public class AttackEvent
        {
            public AttackState attackState;

            /// <summary>該狀態自己的 0→1 進度，滿了就換狀態。</summary>
            public float progress;

            /// <summary>蓄力強度。只在進過 OVERDRAW 之後有意義，可以超過 1（過蓄）。</summary>
            public float chargeRatio;
        }

        private const float MinAttackFrequency = 0.0001f;
        private const float MinFullChargeTime = 0.0001f;

        [SerializeField]
        private Skill skill;

        private AttackState prevState = AttackState.NONE;

        private AttackState currState = AttackState.IDLE;

        /// <summary>目前狀態已經停留的秒數。</summary>
        private float time = 0;

        /// <summary>離開 OVERDRAW 時保留下來，供 FIRE 使用；回到 IDLE 時歸零。</summary>
        private float chargeRatio = 0f;

        private IDisposable disposable;

        public readonly BehaviorSubject<bool> attackInput = new(false);

        private bool attacking = false;

        private bool canAttack = false;

        [NonSerialized]
        public BehaviorSubject<AttackEvent> attackEvent = new(new AttackEvent
        {
            attackState = AttackState.IDLE,
            progress = 0,
            chargeRatio = 0
        });

        private CharacterStateHandler characterStateHandler;

        private void Awake()
        {
            characterStateHandler = GetComponent<CharacterStateHandler>();
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            characterStateHandler.stateObservable.Subscribe(StateHandler).AddTo(ref builder);

            attackInput.Subscribe((input) =>
            {
                attacking = input;
            }).AddTo(ref builder);

            disposable = builder.Build();
        }

        private void OnDisable()
        {
            disposable.Dispose();
        }

        private void StateHandler(CharacterStateHandler.CharacterState state)
        {
            if (state == CharacterStateHandler.CharacterState.DODGE)
            {
                canAttack = false;
            }
            else
            {
                canAttack = true;
            }
        }

        /// <summary>
        /// DRAW / FIRE / WITHDRAW 依權重瓜分一個 1 / attackFrequency 的循環。
        /// OVERDRAW 不屬於循環，不走這裡。
        /// </summary>
        private float PeriodOf(AttackState state)
        {
            var total = skill.drawWeight + skill.fireWeight + skill.withdrawWeight;
            if (total <= 0f)
                return 0f;

            var weight = state switch
            {
                AttackState.DRAW => skill.drawWeight,
                AttackState.FIRE => skill.fireWeight,
                AttackState.WITHDRAW => skill.withdrawWeight,
                _ => 0f,
            };
            if (weight <= 0f)
                return 0f;

            var cycle = 1f / Mathf.Max(skill.attackFrequency, MinAttackFrequency);
            return cycle * weight / total;
        }

        /// <summary>
        /// 回傳目前狀態的進度與是否已完成。權重為 0 的狀態視為立即完成，
        /// 但進度回報 0 —— 動畫應該停在起點而不是終點。
        /// </summary>
        private (float progress, bool done) Advance(AttackState state)
        {
            var period = PeriodOf(state);
            if (period <= 0f)
                return (0f, true);

            var progress = time / period;
            return (progress, progress >= 1f);
        }

        private void Emit(AttackState state, float progress)
        {
            attackEvent.OnNext(new AttackEvent
            {
                attackState = state,
                progress = progress,
                chargeRatio = chargeRatio,
            });
        }

        // 投射物生成器尚未存在（計畫 Phase 15、16）。接上之前 FIRE 會正常運轉，
        // 只是不會有投射物飛出來。
        private AttackContext Context() => new(transform, skill, chargeRatio, null);

        private void Update()
        {
            if (skill == null)
                return;

            var behaviour = AttackBehaviours.Resolve(skill.type);
            if (behaviour == null)
                return;

            if (!canAttack)
            {
                if (currState == AttackState.OVERDRAW)
                {
                    currState = AttackState.FIRE;
                }
                else if (currState != AttackState.FIRE)
                {
                    currState = AttackState.IDLE;
                }
            }

            if (prevState != currState)
            {
                prevState = currState;
                time = 0;

                if (currState == AttackState.IDLE)
                {
                    chargeRatio = 0f;
                    Emit(AttackState.IDLE, 0f);
                }
                else if (currState == AttackState.FIRE)
                {
                    behaviour.OnEnterFire(Context());
                }
            }

            switch (currState)
            {
                case AttackState.IDLE:
                {
                    if (attacking)
                    {
                        currState = AttackState.DRAW;
                    }
                    break;
                }

                case AttackState.DRAW:
                {
                    var (progress, done) = Advance(AttackState.DRAW);
                    Emit(AttackState.DRAW, progress);
                    behaviour.OnDrawTick(Context(), progress);

                    if (done)
                    {
                        currState = behaviour.CanOverdraw && attacking
                            ? AttackState.OVERDRAW
                            : AttackState.FIRE;
                    }
                    break;
                }

                case AttackState.OVERDRAW:
                {
                    // 強度：共用邏輯，餵動畫與傷害，可以超過 1。
                    chargeRatio = time / Mathf.Max(skill.fullChargeTime, MinFullChargeTime);

                    // 完成度：由行為決定，滿了就結束滿蓄。預設恆為 0 = 無限 hold。
                    var completion = behaviour.OverdrawProgress(Context(), time);
                    Emit(AttackState.OVERDRAW, completion);
                    behaviour.OnOverdrawTick(Context(), completion);

                    // 放手一定出得來，這是共用邏輯保證的底線；
                    // completion 是型態可以加碼的提前結束條件（耐力耗盡、過蓄上限）。
                    if (completion >= 1f || !attacking)
                    {
                        currState = AttackState.FIRE;
                    }
                    break;
                }

                case AttackState.FIRE:
                {
                    var (progress, done) = Advance(AttackState.FIRE);
                    Emit(AttackState.FIRE, progress);
                    behaviour.OnFireTick(Context(), progress);

                    if (done)
                    {
                        currState = AttackState.WITHDRAW;
                    }
                    break;
                }

                case AttackState.WITHDRAW:
                {
                    var (progress, done) = Advance(AttackState.WITHDRAW);
                    Emit(AttackState.WITHDRAW, progress);
                    behaviour.OnWithdrawTick(Context(), progress);

                    if (done)
                    {
                        currState = AttackState.IDLE;
                    }
                    break;
                }
            }

            time += Time.deltaTime;
        }
    }
}
