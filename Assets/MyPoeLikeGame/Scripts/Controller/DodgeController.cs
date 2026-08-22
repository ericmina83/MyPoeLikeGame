using UnityEngine;
using MyPoeLikeGame.Handlers;
using System;
using R3;

namespace MyPoeLikeGame.Controllers
{
    [RequireComponent(typeof(DodgeHandler))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class DodgeController : MonoBehaviour
    {
        private PlayerInputHandler inputHandler;
        private DodgeHandler dodgeHandler;

        private IDisposable disposable;


        private void Awake()
        {
            inputHandler = GetComponent<PlayerInputHandler>();
            dodgeHandler = GetComponent<DodgeHandler>();
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            inputHandler.DodgeInput.Subscribe(Dodge).AddTo(ref builder);

            disposable = builder.Build();
        }

        private void OnDisable()
        {
            disposable?.Dispose();
            disposable = null;
        }

        private void Dodge(bool _)
        {
            dodgeHandler.dodgeInput.OnNext(true);
        }
    }
}