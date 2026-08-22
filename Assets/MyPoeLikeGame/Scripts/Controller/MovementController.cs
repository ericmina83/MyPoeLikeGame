using System;
using MyPoeLikeGame.Handlers;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Controllers
{
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(MovementHandler))]
    public class MovementController : MonoBehaviour
    {
        private PlayerInputHandler inputHandler;
        private MovementHandler movementHandler;

        private IDisposable disposable;

        private void Awake()
        {
            inputHandler = GetComponent<PlayerInputHandler>();
            movementHandler = GetComponent<MovementHandler>();
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            inputHandler.MovementInput.Subscribe(Move).AddTo(ref builder);

            disposable = builder.Build();
        }

        private void OnDisable()
        {
            disposable?.Dispose();
            disposable = null;
        }

        private void Move(Vector2 direction)
        {
            movementHandler.movementInput.OnNext(direction);
        }
    }
}
