using System;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
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

        public class AttackEvent : IEvent
        {
            public AttackState attackState;
            public float percentage;
            public AttackHandler sender;
        }

        private AttackState prevState = AttackState.NONE;

        private AttackState currState = AttackState.IDLE;

        private PlayerInputHandler.AttackEvent nextInputEvent = null;
        private PlayerInputHandler.AttackEvent currInputEvent = null;

        [SerializeField]
        private Skill skill;

        private float time = 0;

        private string gameObjectId;

        private IDisposable disposable;

        private bool canAttack = true;

        private void OnEnable()
        {
            prevState = AttackState.NONE;
            currState = AttackState.IDLE;

            gameObjectId = gameObject.GetInstanceID().ToString();

            var observable = Reactive.events
                .Where(e => e.gameObjectId == gameObjectId);

            var builder = Disposable.CreateBuilder();

            observable
                .OfType<IEvent, PlayerInputHandler.AttackEvent>()
                .Subscribe(e => nextInputEvent = e)
                .AddTo(ref builder);

            observable
                .OfType<IEvent, CharacterStateHandler.StateEvent>()
                .Select(e => e.state)
                .Subscribe(StateHandler)
                .AddTo(ref builder);

            disposable = builder.Build();

            canAttack = true;
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
                    Reactive.events.OnNext(new AttackEvent
                    {
                        gameObjectId = gameObjectId,
                        attackState = AttackState.IDLE,
                        percentage = 0,
                        sender = this
                    });
                }
            }

            if (currState == AttackState.IDLE)
            {
                if (nextInputEvent != null)
                {
                    currInputEvent = nextInputEvent;
                    nextInputEvent = null;
                    currState = AttackState.DRAW;
                }
            }
            else if (currState == AttackState.DRAW)
            {
                var drawPercentage = time * skill.attackFrequency * 2f;

                Reactive.events.OnNext(new AttackEvent
                {
                    gameObjectId = gameObjectId,
                    attackState = AttackState.DRAW,
                    percentage = drawPercentage,
                    sender = this
                });

                if (drawPercentage > 1.0f)
                {
                    if (currInputEvent.attacking)
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

                Reactive.events.OnNext(new AttackEvent
                {
                    gameObjectId = gameObjectId,
                    attackState = AttackState.OVERDRAW,
                    percentage = drawPercentage,
                    sender = this
                });

                if (!currInputEvent.attacking)
                {
                    currState = AttackState.FIRE;
                }
            }
            else if (currState == AttackState.FIRE)
            {
                Reactive.events.OnNext(new AttackEvent
                {
                    gameObjectId = gameObjectId,
                    attackState = AttackState.FIRE,
                    percentage = 0,
                    sender = this
                });

                skill.Fire(transform);

                currState = AttackState.WITHDRAW;
            }
            else if (currState == AttackState.WITHDRAW)
            {
                var withdrawPercentage = time * skill.attackFrequency * 2f;

                Reactive.events.OnNext(new AttackEvent
                {
                    gameObjectId = gameObjectId,
                    attackState = AttackState.WITHDRAW,
                    percentage = withdrawPercentage,
                    sender = this
                });

                if (withdrawPercentage >= 1.0f)
                {
                    currState = AttackState.IDLE;
                }
            }

            time += Time.deltaTime;
        }
    }
}
