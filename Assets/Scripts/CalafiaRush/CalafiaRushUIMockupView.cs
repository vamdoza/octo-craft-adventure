using UnityEngine;
using UnityEngine.UI;

namespace CalafiaRush
{
    [DisallowMultipleComponent]
    public sealed class CalafiaRushUIMockupView : MonoBehaviour
    {
        [SerializeField] private CalafiaRushUISpriteCatalog catalog;
        [SerializeField] private bool rebuildOnAwake = true;
        [SerializeField] private bool showGameplayLayer = true;
        [SerializeField] private bool showTitleLayer = true;
        [SerializeField] private bool showToastSample = true;
        [SerializeField] private bool showConfirmSample = true;

        private RectTransform _root;

        public void Configure(CalafiaRushUISpriteCatalog spriteCatalog, bool rebuildOnAwake)
        {
            catalog = spriteCatalog;
            this.rebuildOnAwake = rebuildOnAwake;
        }

        private void Awake()
        {
            if (!rebuildOnAwake)
            {
                return;
            }

            BuildMockup(GetComponent<RectTransform>());
        }

        public void BuildMockup(RectTransform root)
        {
            _root = root;
            ClearExistingChildren();

            if (catalog == null)
            {
                Debug.LogWarning("CalafiaRushUIMockupView is missing a sprite catalog.");
                return;
            }

            var backdrop = CreateStretchImage("Backdrop", new Color(0.04f, 0.07f, 0.11f, 0.92f));
            backdrop.transform.SetAsFirstSibling();

            if (showTitleLayer)
            {
                BuildTitleLayer();
            }

            if (showGameplayLayer)
            {
                BuildGameplayLayer();
            }

            if (showToastSample)
            {
                BuildToastSample();
            }

            if (showConfirmSample)
            {
                BuildConfirmSample();
            }
        }

        private void BuildTitleLayer()
        {
            var layer = CreatePanel("TitleLayer");
            Stretch(layer, 40f, 40f, 40f, 260f);

            CreateLabel(layer, "CALAFIA RUSH", 42, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -24f), new Vector2(640f, 72f),
                new Color(1f, 0.82f, 0.15f, 1f));

            CreateLabel(layer,
                "Race the route. Pick up passengers.\nDodge traffic. Respect the lights.",
                22,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(36f, -110f),
                new Vector2(760f, 120f),
                new Color(0.92f, 0.95f, 0.98f, 1f));

            var menuColumn = CreatePanel("MenuButtons");
            menuColumn.SetParent(layer, false);
            SetRect(menuColumn, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 36f), new Vector2(420f, 360f));

            CreateSpriteButton(menuColumn, "btn_play_primary", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(360f, 72f));
            CreateSpriteButton(menuColumn, "btn_garage_menu", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -84f), new Vector2(360f, 72f));
            CreateSpriteButton(menuColumn, "btn_upgrades_menu", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -168f), new Vector2(360f, 72f));
            CreateSpriteButton(menuColumn, "btn_settings_menu", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -252f), new Vector2(360f, 72f));
            CreateSpriteButton(menuColumn, "btn_quit_menu", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -336f), new Vector2(360f, 72f));

            var garagePanel = CreatePanel("GaragePreview");
            garagePanel.SetParent(layer, false);
            SetRect(garagePanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-420f, -420f), new Vector2(360f, 360f));

            CreateSpriteImage(garagePanel, "panel_popup_dark", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var vehicleRow = CreatePanel("VehicleRow");
            vehicleRow.SetParent(garagePanel, false);
            SetRect(vehicleRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150f, -20f), new Vector2(300f, 80f));
            CreateSpriteImage(vehicleRow, "vehicle_yellow", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -35f), new Vector2(70f, 70f));
            CreateSpriteImage(vehicleRow, "vehicle_white", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-35f, -32f), new Vector2(70f, 64f));
            CreateSpriteImage(vehicleRow, "vehicle_green", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-70f, -32f), new Vector2(70f, 64f));

            CreateLabel(garagePanel, "GARAGE PREVIEW", 20, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(280f, 36f),
                new Color(1f, 0.88f, 0.28f, 1f));
        }

        private void BuildGameplayLayer()
        {
            var layer = CreatePanel("GameplayLayer");
            Stretch(layer, 24f, 24f, 180f, 180f);

            var topBar = CreatePanel("TopBar");
            topBar.SetParent(layer, false);
            SetRect(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -110f), new Vector2(0f, 96f));

            CreateSpriteImage(topBar, "hud_money_chip", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -48f), new Vector2(260f, 96f));
            CreateSpriteImage(topBar, "hud_distance_chip", new Vector2(0.34f, 0.5f), new Vector2(0.34f, 0.5f), new Vector2(-130f, -49f), new Vector2(260f, 96f));
            CreateSpriteImage(topBar, "hud_fuel_chip", new Vector2(0.68f, 0.5f), new Vector2(0.68f, 0.5f), new Vector2(-130f, -49f), new Vector2(260f, 96f));
            CreateSpriteImage(topBar, "hud_utility_icons", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-300f, -49f), new Vector2(300f, 96f));

            var controls = CreatePanel("Controls");
            controls.SetParent(layer, false);
            SetRect(controls, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 24f), new Vector2(0f, 130f));

            CreateSpriteButton(controls, "btn_brake", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, -55f), new Vector2(150f, 110f));
            CreateSpriteButton(controls, "btn_play_action", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-75f, -55f), new Vector2(150f, 110f));
            CreateSpriteButton(controls, "btn_garage_action", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-174f, -55f), new Vector2(150f, 110f));

            var iconRow = CreatePanel("IconRow");
            iconRow.SetParent(layer, false);
            SetRect(iconRow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-180f, 170f), new Vector2(360f, 80f));
            CreateSpriteButton(iconRow, "icon_pause", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -38f), new Vector2(76f, 76f));
            CreateSpriteButton(iconRow, "icon_wrench", new Vector2(0.33f, 0.5f), new Vector2(0.33f, 0.5f), new Vector2(-38f, -38f), new Vector2(76f, 76f));
            CreateSpriteButton(iconRow, "icon_gas_can", new Vector2(0.66f, 0.5f), new Vector2(0.66f, 0.5f), new Vector2(-38f, -38f), new Vector2(76f, 76f));
            CreateSpriteButton(iconRow, "icon_van", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-76f, -38f), new Vector2(76f, 76f));
        }

        private void BuildToastSample()
        {
            var toast = CreatePanel("ToastSample");
            Stretch(toast, 0f, 0f, 320f, 320f);
            CreateSpriteImage(toast, "toast_passenger", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-280f, 36f), new Vector2(560f, 90f));
        }

        private void BuildConfirmSample()
        {
            var confirm = CreatePanel("ConfirmSample");
            confirm.SetParent(_root, false);
            SetRect(confirm, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-250f, -190f), new Vector2(500f, 380f));
            CreateSpriteImage(confirm, "panel_confirm", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void ClearExistingChildren()
        {
            for (var i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private RectTransform CreatePanel(string name)
        {
            var panelObject = new GameObject(name, typeof(RectTransform));
            panelObject.transform.SetParent(_root, false);
            return panelObject.GetComponent<RectTransform>();
        }

        private Image CreateStretchImage(string name, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(_root, false);
            var rect = imageObject.GetComponent<RectTransform>();
            Stretch(rect, 0f, 0f, 0f, 0f);
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Image CreateSpriteImage(
            Transform parent,
            string spriteName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var sprite = catalog.Get(spriteName);
            var imageObject = new GameObject(spriteName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var rect = imageObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && HasBorder(sprite) ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private Button CreateSpriteButton(
            Transform parent,
            string spriteName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var image = CreateSpriteImage(parent, spriteName, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private Text CreateLabel(
            Transform parent,
            string text,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            var rect = labelObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            var label = labelObject.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = alignment;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static bool HasBorder(Sprite sprite)
        {
            var border = sprite.border;
            return border.x > 0f || border.y > 0f || border.z > 0f || border.w > 0f;
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            SetRect(rect, Vector2.zero, Vector2.one, new Vector2(left, bottom), new Vector2(-right, -top));
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
