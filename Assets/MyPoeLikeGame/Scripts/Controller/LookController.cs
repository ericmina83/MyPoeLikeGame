using System;
using MyPoeLikeGame.Handlers;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Controllers
{
    [RequireComponent(typeof(AimingHandler))]
    [RequireComponent(typeof(PlayerInputHandler))]
    class LookController : MonoBehaviour
    {
        private PlayerInputHandler inputHandler;
        private AimingHandler aimingHandler;

        private IDisposable disposable;

        private void Awake()
        {
            aimingHandler = GetComponent<AimingHandler>();
            inputHandler = GetComponent<PlayerInputHandler>();
        }

        private void OnEnable()
        {
            var builder = Disposable.CreateBuilder();

            inputHandler.LookInput.Subscribe(OnMouseMove).AddTo(ref builder);

            disposable = builder.Build();
        }

        private void OnDisable()
        {
            disposable?.Dispose();
            disposable = null;
        }

        private void OnMouseMove(Vector2 mousePosition)
        {
            var ray = Camera.main.ScreenPointToRay(mousePosition);

            var height = ray.origin.y - transform.position.y;
            var distance = Mathf.Abs(height / ray.direction.y);

            var point = ray.GetPoint(distance);

            // If ray hit something, make the object look at the hit point
            if (Physics.Raycast(ray, out var hit, distance))
            {
                point = hit.point;
            }

            aimingHandler.lookInput.OnNext(new Vector2(point.x, point.z));
        }
    }
}
