using System;
using System.Collections.Generic;

namespace KingdomPipeWrench.Interaction
{
    /// <summary>Runtime-only marker eligibility/removals. No config or save serialization.</summary>
    internal sealed class SessionGrabbers<T> where T : class
    {
        private object session;
        private readonly Dictionary<T, HashSet<int>> hidden = new Dictionary<T, HashSet<int>>();
        private readonly Dictionary<T, HashSet<int>> inserted = new Dictionary<T, HashSet<int>>();
        private readonly HashSet<T> automatic = new HashSet<T>();
        public void SetSession(object identity)
        {
            if (ReferenceEquals(identity, session)) return;
            session = identity; hidden.Clear(); inserted.Clear(); automatic.Clear();
        }
        public void Add(T pipe) { Ensure(pipe); automatic.Add(pipe); }
        private void Ensure(T pipe)
        {
            if (hidden.ContainsKey(pipe)) return;
            hidden.Add(pipe, new HashSet<int>()); inserted.Add(pipe, new HashSet<int>());
        }
        public bool Contains(T pipe) => hidden.ContainsKey(pipe);
        public bool IsVisible(T pipe, int segment, bool allowAutomatic = true)
            => hidden.TryGetValue(pipe, out var removed) && !removed.Contains(segment)
                && (allowAutomatic && automatic.Contains(pipe) || inserted[pipe].Contains(segment));
        public void Hide(T pipe, int segment) { if (hidden.TryGetValue(pipe, out var removed)) removed.Add(segment); }
        public void Restore(T pipe, int segment) { Ensure(pipe); inserted[pipe].Add(segment); hidden[pipe].Remove(segment); }
        public void Remap(T pipe, Func<int, int?> mapping)
        {
            if (!Contains(pipe)) return;
            RemapSet(hidden[pipe], mapping); RemapSet(inserted[pipe], mapping);
        }
        private static void RemapSet(HashSet<int> values, Func<int, int?> mapping)
        {
            var mapped = new HashSet<int>();
            foreach (int value in values) { var next = mapping(value); if (next.HasValue) mapped.Add(next.Value); }
            values.Clear(); values.UnionWith(mapped);
        }
    }
}
