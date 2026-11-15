using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KingdomPipeWrench.Interaction
{
    /// <summary>Runtime-only marker eligibility/removals. No config or save serialization.</summary>
    internal sealed class SessionGrabbers<T> where T : class
    {
        private sealed class State
        {
            public readonly HashSet<int> Hidden = new HashSet<int>();
            public readonly HashSet<int> Inserted = new HashSet<int>();
            public bool Automatic;
        }

        private object session;
        private ConditionalWeakTable<T, State> states = new ConditionalWeakTable<T, State>();

        public void SetSession(object identity)
        {
            if (ReferenceEquals(identity, session)) return;
            session = identity;
            states = new ConditionalWeakTable<T, State>();
        }

        public void Add(T pipe)
        {
            if (pipe == null) return;
            Ensure(pipe).Automatic = true;
        }

        private State Ensure(T pipe) => states.GetValue(pipe, _ => new State());

        public bool Contains(T pipe) => pipe != null && states.TryGetValue(pipe, out _);

        public bool IsVisible(T pipe, int segment, bool allowAutomatic = true)
            => pipe != null && states.TryGetValue(pipe, out var state) && !state.Hidden.Contains(segment)
                && (allowAutomatic && state.Automatic || state.Inserted.Contains(segment));

        public void Hide(T pipe, int segment)
        {
            if (pipe != null && states.TryGetValue(pipe, out var state)) state.Hidden.Add(segment);
        }

        public void Restore(T pipe, int segment)
        {
            if (pipe == null) return;
            var state = Ensure(pipe);
            state.Inserted.Add(segment);
            state.Hidden.Remove(segment);
        }

        public void Remap(T pipe, Func<int, int?> mapping)
        {
            if (pipe == null || !states.TryGetValue(pipe, out var state)) return;
            RemapSet(state.Hidden, mapping);
            RemapSet(state.Inserted, mapping);
        }

        private static void RemapSet(HashSet<int> values, Func<int, int?> mapping)
        {
            var mapped = new HashSet<int>();
            foreach (int value in values) { var next = mapping(value); if (next.HasValue) mapped.Add(next.Value); }
            values.Clear(); values.UnionWith(mapped);
        }
    }
}
