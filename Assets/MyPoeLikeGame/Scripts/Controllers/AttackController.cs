

using System;
using MyPoeLikeGame.Handlers;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Controllers
{
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(AttackHandler))]
    public class AttackController : MonoBehaviour
    {
        private PlayerInputHandler inputHandler;
        private AttackHandler attackHandler;

        private IDisposable disposable;


        private void Awake()
        {
            inputHandler = GetComponent<PlayerInputHandler>();
            attackHandler = GetComponent<AttackHandler>();
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            inputHandler.AttackInput.Subscribe(Attack).AddTo(ref builder);

            disposable = builder.Build();
        }

        private void OnDisable()
        {
            disposable?.Dispose();
            disposable = null;
        }

        private void Attack(bool pressed)
        {
            attackHandler.SetAttackInput(pressed);
        }
    }
}
