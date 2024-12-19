using System;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyPoeLikeGame
{
    public class AimingController : MonoBehaviour
    {
        public class LookAtPointEvent : IEvent
        {
            public Vector2 lookAtPoint;
        }

        public class AimingEvent : IEvent
        {
            public bool aiming;
        }

        PlayerInput inputAction;

        IDisposable disposable;

        private string gameObjectId => gameObject.GetInstanceID().ToString();

        private void Awake()
        {
            inputAction = new();
        }

        private void OnEnable()
        {
            inputAction.Player.Look.performed += Look;
            inputAction.Player.Aim.performed += Aim;
            inputAction.Player.Aim.canceled += Aim;
            inputAction.Enable();

            var builder = Disposable.CreateBuilder();

            var observable = Reactive.events
                .Where(e => e.gameObjectId == gameObjectId);

            observable.OfType<IEvent, LookAtPointEvent>()
                .Select(e => e.lookAtPoint)
                .Subscribe(lookAtPoint =>
                {
                    transform.LookAt(new Vector3(lookAtPoint.x, transform.position.y, lookAtPoint.y));
                }).AddTo(ref builder);


            disposable = builder.Build();
        }

        private void OnDisable()
        {
            inputAction.Disable();
            inputAction.Player.Look.performed -= Look;
            inputAction.Player.Aim.performed -= Aim;
            inputAction.Player.Aim.canceled -= Aim;

            disposable.Dispose();
        }

        void Look(InputAction.CallbackContext ctx)
        {
            var screenPos = ctx.ReadValue<Vector2>();
            var ray = Camera.main.ScreenPointToRay(screenPos);

            var height = ray.origin.y;
            var distance = Mathf.Abs(height / ray.direction.y);

            var point = ray.GetPoint(distance);

            // If ray hit something, make the object look at the hit point
            if (Physics.Raycast(ray, out var hit, distance))
            {
                point = hit.point;
            }

            Reactive.events.OnNext(new LookAtPointEvent
            {
                gameObjectId = gameObjectId,
                lookAtPoint = new Vector2(point.x, point.z)
            });
        }

        private void Aim(InputAction.CallbackContext ctx)
        {
            Reactive.events.OnNext(new AimingEvent
            {
                gameObjectId = gameObjectId,
                aiming = ctx.performed
            });
        }
    }
}
