using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace SandJamTest
{
    // Object pooling pattern: frequently spawned objects (sand pellets, effect sprites...) are reused instead of
    // being created and destroyed, which avoids GC spikes and Instantiate cost on mobile.
    //
    // Pool<T> works for any object that owns a GameObject (a small class holding its renderers, a component...):
    // give it how to create one and how to reach its GameObject; Get activates it, Release deactivates it.
    // Active() lists the objects currently out, so the owner can update and release them in one loop.
    public sealed class Pool<T> : IDisposable where T : class
    {
        readonly ObjectPool<T> pool;
        readonly Func<T, GameObject> gameObjectOf;
        readonly List<T> active = new List<T>();

        public Pool(Func<T> create, Func<T, GameObject> gameObjectOf, int prewarm = 0, int maxSize = 128)
        {
            this.gameObjectOf = gameObjectOf;
            pool = new ObjectPool<T>(create,
                item => { var go = gameObjectOf(item); if (go) go.SetActive(true); },
                item => { var go = gameObjectOf(item); if (go) go.SetActive(false); },
                item => { var go = gameObjectOf(item); if (go) UnityEngine.Object.Destroy(go); },
                false, Mathf.Max(prewarm, 1), maxSize);
            if (prewarm > 0)
            {
                var warm = new List<T>(prewarm);
                for (int i = 0; i < prewarm; i++) warm.Add(pool.Get());
                foreach (var item in warm) pool.Release(item);
            }
        }

        public int CountActive { get { return active.Count; } }
        public int CountInactive { get { return pool.CountInactive; } }
        public List<T> Active() { return active; }

        public T Get() { var item = pool.Get(); active.Add(item); return item; }

        public void Release(T item)
        {
            if (item == null || !active.Remove(item)) return;
            pool.Release(item);
        }

        // Releases the active item at index (cheap removal while iterating Active() backwards).
        public void ReleaseAt(int index)
        {
            var item = active[index]; active.RemoveAt(index); pool.Release(item);
        }

        public void ReleaseAll()
        {
            for (int i = active.Count - 1; i >= 0; i--) pool.Release(active[i]);
            active.Clear();
        }

        public void Dispose()
        {
            foreach (var item in active) { var go = gameObjectOf(item); if (go) UnityEngine.Object.Destroy(go); }
            active.Clear();
            pool.Dispose();
        }
    }
}
