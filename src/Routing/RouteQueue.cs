using System.Collections.Generic;

namespace KingdomPipeWrench.Routing
{
    /// <summary>Binary min-heap, also available on the game's netstandard runtime.</summary>
    internal sealed class RouteQueue
    {
        private readonly List<(int State, double Score)> items = new List<(int, double)>();
        public int Count => items.Count;
        public void Push(int state, double score)
        {
            items.Add((state, score));
            int i = items.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (items[parent].Score <= score) break;
                items[i] = items[parent]; i = parent;
            }
            items[i] = (state, score);
        }
        public int Pop()
        {
            int result = items[0].State;
            var last = items[items.Count - 1]; items.RemoveAt(items.Count - 1);
            if (items.Count == 0) return result;
            int i = 0;
            while (i * 2 + 1 < items.Count)
            {
                int child = i * 2 + 1;
                if (child + 1 < items.Count && items[child + 1].Score < items[child].Score) child++;
                if (items[child].Score >= last.Score) break;
                items[i] = items[child]; i = child;
            }
            items[i] = last; return result;
        }
    }
}
