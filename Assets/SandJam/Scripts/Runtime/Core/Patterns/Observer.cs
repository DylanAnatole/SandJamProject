using System;
using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest
{
    // Observer pattern. A GameEvent is a subject: listeners subscribe with += and unsubscribe with -=, Raise notifies
    // every listener. Raising allocates nothing (some events fire ~50 times a second while sand pours), a listener
    // may unsubscribe itself while being notified, and one failing listener is logged without stopping the others.
    // GameEvents (Managers/GameEvents.cs) declares the game's events with these types.
    public abstract class GameEventBase<TListener> where TListener : Delegate
    {
        protected readonly List<TListener> listeners = new List<TListener>();
        public int ListenerCount { get { return listeners.Count; } }
        protected void Add(TListener listener) { if (listener != null && !listeners.Contains(listener)) listeners.Add(listener); }
        protected void Remove(TListener listener) { listeners.Remove(listener); }
        public void Clear() { listeners.Clear(); }
        // Raise walks the list back to front, so a listener removing itself does not skip the next one.
    }

    public sealed class GameEvent : GameEventBase<Action>
    {
        public static GameEvent operator +(GameEvent e, Action listener) { e.Add(listener); return e; }
        public static GameEvent operator -(GameEvent e, Action listener) { e.Remove(listener); return e; }
        public void Raise()
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (i >= listeners.Count) continue;
                try { listeners[i](); } catch (Exception e) { Debug.LogException(e); }
            }
        }
    }

    public sealed class GameEvent<T> : GameEventBase<Action<T>>
    {
        public static GameEvent<T> operator +(GameEvent<T> e, Action<T> listener) { e.Add(listener); return e; }
        public static GameEvent<T> operator -(GameEvent<T> e, Action<T> listener) { e.Remove(listener); return e; }
        public void Raise(T a)
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (i >= listeners.Count) continue;
                try { listeners[i](a); } catch (Exception e) { Debug.LogException(e); }
            }
        }
    }

    public sealed class GameEvent<T1, T2> : GameEventBase<Action<T1, T2>>
    {
        public static GameEvent<T1, T2> operator +(GameEvent<T1, T2> e, Action<T1, T2> listener) { e.Add(listener); return e; }
        public static GameEvent<T1, T2> operator -(GameEvent<T1, T2> e, Action<T1, T2> listener) { e.Remove(listener); return e; }
        public void Raise(T1 a, T2 b)
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (i >= listeners.Count) continue;
                try { listeners[i](a, b); } catch (Exception e) { Debug.LogException(e); }
            }
        }
    }

    public sealed class GameEvent<T1, T2, T3> : GameEventBase<Action<T1, T2, T3>>
    {
        public static GameEvent<T1, T2, T3> operator +(GameEvent<T1, T2, T3> e, Action<T1, T2, T3> listener) { e.Add(listener); return e; }
        public static GameEvent<T1, T2, T3> operator -(GameEvent<T1, T2, T3> e, Action<T1, T2, T3> listener) { e.Remove(listener); return e; }
        public void Raise(T1 a, T2 b, T3 c)
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (i >= listeners.Count) continue;
                try { listeners[i](a, b, c); } catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
