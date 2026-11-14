using System;
using System.Linq;
using KingdomFoundation.Api;
using KingdomPipeWrench.Plugin;
using KingdomPipeWrench.Routing;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KingdomPipeWrench.UI
{
    /// <summary>A slim sibling of the mod bar, anchored to its live HUD bounds.</summary>
    internal sealed class PipeToolbar : IDisposable
    {
        private readonly PipeWrenchPlugin plugin;
        private readonly ToolbarNode layout = ToolbarLayout.Load();
        private TMP_FontAsset pixelFont, sansFont;
        private float referencePixels;
        private RectTransform root, anchor;
        private Sprite dark, selected;
        private readonly Image[] buttons = new Image[4];
        private readonly TMP_Text[] labels = new TMP_Text[4];
        private readonly Vector3[] corners = new Vector3[4];
        public PipeToolbar(PipeWrenchPlugin plugin) { this.plugin = plugin; }
        public bool IsPointerOver
        {
            get
            {
                if (!root || !root.gameObject.activeInHierarchy || Mouse.current == null) return false;
                var canvas = root.GetComponentInParent<Canvas>();
                var camera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                return RectTransformUtility.RectangleContainsScreenPoint(root, Mouse.current.position.ReadValue(), camera);
            }
        }
        public void Tick()
        {
            var bounds = FoundationApi.UiAccess.ModBarBounds;
            if (bounds != anchor || !root)
            {
                Dispose(); anchor = bounds;
                if (anchor) Build();
            }
            if (!root) return;
            root.gameObject.SetActive(plugin.ShowTools);
            Position();
            for (int i = 0; i < buttons.Length; i++)
            {
                bool active = (int)plugin.Avoidance.Value == i;
                var sprite = active ? selected : dark;
                if (sprite) { buttons[i].sprite = sprite; buttons[i].type = Image.Type.Sliced; buttons[i].color = Color.white; }
                else buttons[i].color = active ? new Color(.78f, .61f, .32f) : new Color(.32f, .28f, .25f);
                labels[i].color = active ? Ink("#241e1b") : Ink("#e8e0cf");
            }
        }
        private void Build()
        {
            root = Rect("PipeWrenchToolbar", (RectTransform)anchor.parent, layout.x, 0, layout.w, layout.h);
            var canvas = root.GetComponentInParent<Canvas>();
            referencePixels = canvas ? canvas.referencePixelsPerUnit : 100;
            pixelFont = anchor.GetComponentInChildren<TMP_Text>(true)?.font;
            sansFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
                ?? Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f && f.name.IndexOf("LiberationSans", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? TMP_Settings.defaultFontAsset ?? pixelFont;
            dark = FoundationApi.Presentation.FindSprite("DecoRectDark");
            selected = FoundationApi.Presentation.FindSprite("DecoRectLight");
            FoundationApi.NativeUiFactory.BlockWorkspaceZoom(root.gameObject);
            foreach (var node in layout.children)
            {
                var view = CreateVisual(node, root);
                if (node.type == "group") BindRule(node, view);
            }
            Position();
        }
        private RectTransform CreateVisual(ToolbarNode node, RectTransform parent)
        {
            var rect = Rect(node.name, parent, node.x, node.y, node.w, node.h);
            if (node.type == "sprite") CreateSprite(node, rect);
            else if (node.type == "text") CreateLabel(node, rect);
            else if (node.children != null)
                foreach (var child in node.children) CreateVisual(child, rect);
            return rect;
        }
        private void CreateSprite(ToolbarNode node, RectTransform rect)
        {
            string spriteName = node.asset == "sharedassets0.assets:41" ? "ItemSlotMini"
                : node.asset == "sharedassets1.assets:184" ? "DecoRectLight"
                : node.asset == "sharedassets1.assets:142" ? "DecoRectDark" : node.name;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = FoundationApi.Presentation.FindSprite(spriteName);
            image.type = node.slice ? Image.Type.Sliced : Image.Type.Simple;
            image.color = new Color(1, 1, 1, node.opacity);
            if (image.sprite && node.pixelScale > 0)
                image.pixelsPerUnitMultiplier = referencePixels / (image.sprite.pixelsPerUnit * node.pixelScale);
        }
        private void CreateLabel(ToolbarNode node, RectTransform rect)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = node.font == "sans" ? sansFont : pixelFont;
            label.text = node.text; label.color = Ink(node.color); label.fontSize = node.fontSize;
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        }
        private void BindRule(ToolbarNode node, RectTransform rect)
        {
            string[] names = { "Buildings", "Overlap", "Both", "Strict" };
            int index = Array.FindIndex(names, name => node.name == "Rule - " + name);
            if (index < 0) return;
            var image = rect.GetComponentInChildren<Image>(); buttons[index] = image;
            labels[index] = rect.GetComponentInChildren<TMP_Text>();
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => { plugin.SetAvoidance((AvoidanceMode)index); FoundationApi.UiAccess.PlaySound(GameUiSound.Button); });
            var trigger = rect.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => FoundationApi.UiAccess.ShowStatusMessage("Pipe Wrench: " + AvoidanceRules.Label((AvoidanceMode)index) + " — Shift cycles rules"));
            trigger.triggers.Add(enter);
        }
        private static Color Ink(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.white;
        private static RectTransform Rect(string name, RectTransform parent, float x, float y, float width, float height)
        {
            var rect = FoundationApi.NativeUiFactory.CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        private void Position()
        {
            anchor.GetWorldCorners(corners);
            var parent = (RectTransform)root.parent;
            float bottom = parent.InverseTransformPoint(corners[0]).y;
            float available = Mathf.Max(1, parent.rect.width - layout.x * 2);
            root.localScale = Vector3.one * Mathf.Min(1, available / layout.w);
            root.anchoredPosition = new Vector2(layout.x, bottom - parent.rect.yMax - 4);
            root.SetAsLastSibling();
        }
        public void Dispose()
        { if (root) UnityEngine.Object.Destroy(root.gameObject); root = null; anchor = null; }
    }
}
