using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace MyPoeLikeGame
{
    public class Pool : MonoBehaviour
    {
        private readonly Dictionary<Transform, Stack<PoolObject>> pools = new();

        public class PoolEvent : IEvent
        {
            public enum PoolEventState
            {
                FIRE,
                RECYCLE,
            }

            public PoolEventState state;
            public PoolObject poolObject;
            public Vector3 offset;
            public Vector3 direction;
            public Transform sender;
        }

        private IDisposable disposable;

        public void Awake()
        {
            DontDestroyOnLoad(this);
        }

        public void OnEnable()
        {
            var observable = Reactive.events
                .OfType<IEvent, PoolEvent>();

            var builder = Disposable.CreateBuilder();

            observable
                .Where(e => e.state == PoolEvent.PoolEventState.FIRE)
                .Subscribe(Fire)
                .AddTo(ref builder);

            observable.
                Where(e => e.state == PoolEvent.PoolEventState.RECYCLE)
                .Subscribe(Recycle)
                .AddTo(ref builder);

            disposable = builder.Build();
        }

        public void OnDisable()
        {
            disposable.Dispose();
        }

        private void Fire(PoolEvent e)
        {
            var prefab = e.poolObject;
            var offset = e.offset;
            var direction = e.direction;
            var sender = e.sender;

            if (!pools.TryGetValue(sender, out var pool))
            {
                pool = new();
                pools.Add(sender, pool);
            }

            if (!pool.TryPop(out var go))
            {
                go = Instantiate(prefab, transform);
                pool.Push(go);
            }

            var localToWorld = sender.localToWorldMatrix;
            var position = localToWorld.MultiplyPoint(offset);
            var rotation = Quaternion.LookRotation(localToWorld.MultiplyVector(direction), sender.up);

            go.Sender = sender;
            go.gameObject.SetActive(true);
            go.transform.SetPositionAndRotation(position, rotation);
        }

        private void Recycle(PoolEvent e)
        {
            var sender = e.sender;
            var poolObject = e.poolObject;

            if (pools.TryGetValue(sender, out var pool))
            {
                pool.Push(poolObject);
            }
        }
    }
}
