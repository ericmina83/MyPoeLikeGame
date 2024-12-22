using R3;
using System;
using UnityEngine;

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

        private void OnEnable()
        {
            disposable = Reactive.events
                .Where(e => e.gameObjectId == gameObjectId)
                .OfType<IEvent, PlayerInputHandler.LookEvent>()
                .Select(e => e.mousePosition)
                .Subscribe(Look);
        }

        private void OnDisable()
        {
            disposable.Dispose();
        }

        private void Look(Vector2 mousePosition)
        {
            var ray = Camera.main.ScreenPointToRay(mousePosition);

            var height = ray.origin.y;
            var distance = Mathf.Abs(height / ray.direction.y);

            var point = ray.GetPoint(distance);

            // If ray hit something, make the object look at the hit point
            if (Physics.Raycast(ray, out var hit, distance))
            {
                point = hit.point;
            }

            transform.LookAt(new Vector3(point.x, transform.position.y, point.z));
        }
    }
}
