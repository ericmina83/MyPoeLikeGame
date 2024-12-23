using R3;
using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.TextCore.Text;

namespace MyPoeLikeGame.Handlers
{
    public class AimingHandler : MonoBehaviour
    {
        public class AimingEvent : IEvent
        {
            public bool aiming;
        }

        private IDisposable disposable;

        private string gameObjectId;

        private void Awake()
        {
            gameObjectId = gameObject.GetInstanceID().ToString();
        }

        private Quaternion? lookAtRotation = null;

        private bool canAim = true;

        private void OnEnable()
        {
            var observable = Reactive.events
                .Where(e => e.gameObjectId == gameObjectId);

            var builder = Disposable.CreateBuilder();

            observable
                .OfType<IEvent, PlayerInputHandler.LookEvent>()
                .Select(e => e.mousePosition)
                .Subscribe(Look)
                .AddTo(ref builder);

            observable
                .OfType<IEvent, CharacterStateHandler.StateEvent>()
                .Select(e => e.state)
                .Subscribe(StateHandler)
                .AddTo(ref builder);

            disposable = builder.Build();

            canAim = true;
        }

        private void OnDisable()
        {
            disposable.Dispose();
        }

        private void StateHandler(CharacterStateHandler.CharacterState state)
        {
            if (state == CharacterStateHandler.CharacterState.DODGE)
            {
                canAim = false;
            }
            else
            {
                canAim = true;
            }
        }

        private void Look(Vector2 mousePosition)
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

            point.y = transform.position.y;

            lookAtRotation = Quaternion.LookRotation(point - transform.position);
        }

        private void Update()
        {
            if (!canAim)
                return;

            transform.rotation = lookAtRotation ?? transform.rotation;
        }
    }
}
