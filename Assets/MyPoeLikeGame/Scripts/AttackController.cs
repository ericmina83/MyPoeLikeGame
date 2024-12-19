using UnityEngine;
using UnityEngine.InputSystem;

namespace MyPoeLikeGame
{
    public class AttackController : MonoBehaviour
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
            public AttackController sender;
        }

        private AttackState prevState = AttackState.NONE;

        private AttackState currState = AttackState.IDLE;

        private PlayerInput inputActions;

        private bool attacking = false;

        private float attackFrequency = 1;

        private float overdrawPeriod = 1;

        private float time = 0;

        private string GameObjectId => gameObject.GetInstanceID().ToString();

        private void Awake()
        {
            inputActions = new();
        }

        private void OnEnable()
        {
            inputActions.Player.Attack.performed += Attack;
            inputActions.Player.Attack.canceled += Attack;
            inputActions.Enable();

            prevState = AttackState.NONE;
            currState = AttackState.IDLE;
        }

        private void OnDisable()
        {
            inputActions.Disable();
            inputActions.Player.Attack.performed -= Attack;
            inputActions.Player.Attack.canceled -= Attack;
        }

        private void Attack(InputAction.CallbackContext ctx)
        {
            attacking = ctx.performed || (!ctx.canceled && attacking);
        }

        private void Update()
        {
            if (prevState != currState)
            {
                prevState = currState;
                time = 0;

                if (currState == AttackState.IDLE)
                {
                    Reactive.events.OnNext(new AttackEvent
                    {
                        gameObjectId = GameObjectId,
                        attackState = AttackState.IDLE,
                        percentage = 0,
                        sender = this
                    });
                }
            }

            if (currState == AttackState.IDLE)
            {
                if (attacking)
                {
                    currState = AttackState.DRAW;
                }
            }
            else if (currState == AttackState.DRAW)
            {
                var drawPercentage = time * attackFrequency * 2f;

                Reactive.events.OnNext(new AttackEvent
                {
                    gameObjectId = GameObjectId,
                    attackState = AttackState.DRAW,
                    percentage = drawPercentage,
                    sender = this
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
                var drawPercentage = time / overdrawPeriod;

                Reactive.events.OnNext(new AttackEvent
                {
                    gameObjectId = GameObjectId,
                    attackState = AttackState.OVERDRAW,
                    percentage = drawPercentage,
                    sender = this
                });

                if (!attacking)
                {
                    currState = AttackState.FIRE;
                }
            }
            else if (currState == AttackState.FIRE)
            {
                Reactive.events.OnNext(new AttackEvent
                {
                    gameObjectId = GameObjectId,
                    attackState = AttackState.FIRE,
                    percentage = 0,
                    sender = this
                });

                currState = AttackState.WITHDRAW;
            }
            else if (currState == AttackState.WITHDRAW)
            {
                var withdrawPercentage = time * attackFrequency * 2f;

                Reactive.events.OnNext(new AttackEvent
                {
                    gameObjectId = GameObjectId,
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
