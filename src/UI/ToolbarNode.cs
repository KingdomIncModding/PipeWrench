using System;
using System.Linq;
using KingdomFoundation.Json;

namespace KingdomPipeWrench.UI
{
    /// <summary>Native designer geometry and appearance, in canvas units.</summary>
    internal sealed class ToolbarNode
    {
        public string type, name, asset, text, font, color;
        public float x, y, w, h, fontSize, pixelScale, opacity;
        public bool slice;
        public ToolbarNode[] children;
        public ToolbarNode(JsonValue data)
        {
            type = data["type"].AsString; name = data["name"].AsString;
            asset = data["asset"].AsString; text = data["text"].AsString;
            font = data["font"].AsString; color = data["color"].AsString;
            x = Number(data, "x"); y = Number(data, "y"); w = Number(data, "w"); h = Number(data, "h");
            fontSize = Number(data, "fontSize"); pixelScale = Number(data, "pixelScale", 2); opacity = Number(data, "opacity", 1);
            slice = data["slice"].IsBoolean && data["slice"].AsBoolean;
            children = data["children"].IsJsonArray ? data["children"].AsJsonArray.Select(child => new ToolbarNode(child)).ToArray() : Array.Empty<ToolbarNode>();
        }
        private static float Number(JsonValue data, string name, float fallback = 0)
            => data[name].IsNumber ? (float)data[name].AsNumber : fallback;
    }
}
