using UnityEngine;

namespace SandJamTest
{
    // Singleton pattern for scene services: one live instance reachable through Instance; a second copy
    // (e.g. after a scene reload) destroys itself. Subclasses override OnSingletonAwake / OnSingletonDestroy
    // instead of Awake / OnDestroy, and set Persistent to survive scene loads.
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }
        public static bool Ready { get { return Instance != null; } }
        protected virtual bool Persistent { get { return false; } }

        protected void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = (T)this;
            if (Persistent && transform.parent == null) DontDestroyOnLoad(gameObject);
            OnSingletonAwake();
        }

        protected void OnDestroy()
        {
            if (Instance != this) return;
            OnSingletonDestroy();
            Instance = null;
        }

        protected virtual void OnSingletonAwake() { }
        protected virtual void OnSingletonDestroy() { }
    }
}
