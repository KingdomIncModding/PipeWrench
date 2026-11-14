using System;
using System.IO;
using KingdomFoundation.Json;

namespace KingdomPipeWrench.UI
{
    internal sealed class ToolbarLayout
    {
        public static ToolbarNode Load()
        {
            using (var stream = typeof(ToolbarLayout).Assembly.GetManifestResourceStream("KingdomPipeWrench.Toolbar.json"))
            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException("Toolbar layout is missing.")))
            {
                var nodes = JsonValue.Parse(reader.ReadToEnd())["nodes"].AsJsonArray;
                if (nodes == null || nodes.Count != 1)
                    throw new InvalidOperationException("Expected the designer's Pipe Wrench toolbar group.");
                var root = new ToolbarNode(nodes[0]);
                if (root.type != "group" || root.w <= 0 || root.h <= 0 || root.children.Length == 0)
                    throw new InvalidOperationException("The toolbar group has no controls or invalid dimensions.");
                return root;
            }
        }
    }
}
