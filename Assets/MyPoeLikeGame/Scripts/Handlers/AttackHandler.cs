using System;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
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
            public float percentage;
        }

        private AttackState prevState = AttackState.NONE;

        private AttackState currState = AttackState.IDLE;


        [SerializeField]
        private Skill skill;

        private float time = 0;

        private IDisposable disposable;

        public readonly BehaviorSubject<bool> attackInput = new(false);

        private bool attacking = false;

        private bool canAttack = false;

        [NonSerialized]
        public BehaviorSubject<AttackEvent> attackEvent = new(new AttackEvent
        {
            attackState = AttackState.IDLE,
            percentage = 0
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

        private void Update()
        {
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
                    attackEvent.OnNext(new AttackEvent
                    {
                        attackState = AttackState.IDLE,
                        percentage = 0,
                    });
                }
            }

            if (currState == AttackState.IDLE)
            {
                // if (nextInputEvent != null)
                // {
                //     currInputEvent = nextInputEvent;
                //     nextInputEvent = null;
                //     currState = AttackState.DRAW;
                // }
            }
            else if (currState == AttackState.DRAW)
            {
                var drawPercentage = time * skill.attackFrequency * 2f;

                attackEvent.OnNext(new AttackEvent
                {
                    attackState = AttackState.DRAW,
                    percentage = drawPercentage,
                });

                if (drawPercentage > 1.0f)
                {
                    if (attacking)
                    {
                        currState = AttackState.OVERDRAW;
                    }
                    else
                    {
                        currState = AttackState.FIRE;
                    }
                }
            }
            else if (currState == AttackState.OVERDRAW)
            {
                var drawPercentage = time / skill.overdrawPeriod;

                attackEvent.OnNext(new AttackEvent
                {
                    attackState = AttackState.OVERDRAW,
                    percentage = drawPercentage,
                });

                if (!attacking)
                {
                    currState = AttackState.FIRE;
                }
            }
            else if (currState == AttackState.FIRE)
            {
                attackEvent.OnNext(new AttackEvent
                {
                    attackState = AttackState.FIRE,
                    percentage = 0,
                });

                skill.Fire(transform);

                currState = AttackState.WITHDRAW;
            }
            else if (currState == AttackState.WITHDRAW)
            {
                var withdrawPercentage = time * skill.attackFrequency * 2f;

                // Reactive.events.OnNext(new AttackEvent
                // {
                //     gameObjectId = gameObjectId,
                //     attackState = AttackState.WITHDRAW,
                //     percentage = withdrawPercentage,
                //     sender = this
                // });

                if (withdrawPercentage >= 1.0f)
                {
                    currState = AttackState.IDLE;
                }
            }

            time += Time.deltaTime;
        }
    }
}
