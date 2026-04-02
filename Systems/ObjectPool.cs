using System;
using System.Collections.Generic;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Generic object pool that recycles instances to avoid per-frame allocations.
    ///
    /// Team 3 (Technical Lead)       — reduce GC pressure on hot gameplay paths.
    /// Team 8 (Systems Programmer)   — shared pool infrastructure for entities.
    /// Team 10 (Engine Optimizer)    — memory optimization for particle/projectile spam.
    ///
    /// Usage:
    ///   var pool = new ObjectPool&lt;FloatingTextEntry&gt;(() =&gt; new FloatingTextEntry(), 32);
    ///   var entry = pool.Get();
    ///   pool.Return(entry);
    /// </summary>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Stack<T>   _available;
        private readonly Func<T>    _factory;
        private readonly Action<T>  _onGet;     // optional reset action when handing out
        private readonly Action<T>  _onReturn;  // optional cleanup action when returning
        private readonly int        _maxSize;

        /// <summary>Number of items currently waiting in the pool.</summary>
        public int Available => _available.Count;

        /// <summary>
        /// Creates a new pool.
        /// </summary>
        /// <param name="factory">Called to create a new instance when the pool is empty.</param>
        /// <param name="initialSize">Pre-warm count — these instances are created immediately.</param>
        /// <param name="maxSize">Maximum objects kept idle; extras are discarded on Return.</param>
        /// <param name="onGet">Optional delegate run each time an item is handed out.</param>
        /// <param name="onReturn">Optional delegate run each time an item is returned.</param>
        public ObjectPool(Func<T> factory, int initialSize = 0, int maxSize = 128,
                          Action<T> onGet = null, Action<T> onReturn = null)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            _factory   = factory;
            _onGet     = onGet;
            _onReturn  = onReturn;
            _maxSize   = maxSize;
            _available = new Stack<T>(Math.Max(initialSize, 4));

            // Pre-warm
            for (int i = 0; i < initialSize; i++)
                _available.Push(_factory());
        }

        /// <summary>
        /// Retrieves an item from the pool (or creates one if empty).
        /// </summary>
        public T Get()
        {
            T item = _available.Count > 0 ? _available.Pop() : _factory();
            _onGet?.Invoke(item);
            return item;
        }

        /// <summary>
        /// Returns an item to the pool for future reuse.
        /// Items beyond maxSize are simply discarded (eligible for GC).
        /// </summary>
        public void Return(T item)
        {
            if (item == null) return;
            _onReturn?.Invoke(item);
            if (_available.Count < _maxSize)
                _available.Push(item);
        }

        /// <summary>Discards all pooled items (useful on scene reset).</summary>
        public void Clear() => _available.Clear();
    }
}
