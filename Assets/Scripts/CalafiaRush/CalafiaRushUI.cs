using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CalafiaRush
{
    [DefaultExecutionOrder(-25)]
    public sealed class CalafiaRushUI : MonoBehaviour
    {
        [SerializeField] private CalafiaRushGame _game;
        [SerializeField] private CalafiaRushInput _input;

        [Header("Prefab Buttons")]
        [SerializeField] private Button _primaryButtonPrefab;
        [SerializeField] private Button _secondaryButtonPrefab;

        [Header("Garage")]
        [SerializeField] private Sprite _garageListPanelSprite;
        [SerializeField] private Sprite _garageSkinSwatchSprite;

        private GameObject _mainMenuPanel;
        private GameObject _gameUiPanel;
        private GameObject _garagePanel;
        private GameObject _gameOverPanel;
        private Transform _garageSkinList;
        private TextMeshProUGUI _garagePointsLabel;
        private TextMeshProUGUI _gameOverSummary;
        private Button _playButton;
        private Button _garageButton;
        private Button _settingsButton;
        private Button _quitButton;
        private Button _garageBackButton;
        private Button _retryButton;
        private Button _menuButton;
        private GUIStyle _hudStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _buttonStyle;
        private bool _garageVisible;

        private void Awake()
        {
            if (_game == null) _game = FindFirstObjectByType<CalafiaRushGame>();
            if (_input == null) _input = FindFirstObjectByType<CalafiaRushInput>();

            CachePanelReferences();
            EnsureGaragePanel();
        }

        private void Start()
        {
            WireMenuButtons();
            WireGameOverButtons();
            BuildGarageSkinList();
            ShowTitleScreen();

            if (_game == null) return;
            _game.StateChanged += OnGameStateChanged;
            OnGameStateChanged(_game.State);
        }

        private void OnDestroy()
        {
            if (_game != null) _game.StateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            if (_game == null || _input == null) return;

            if (_game.State != CalafiaRushGameState.Running && _input.StartPressed)
                _game.StartGame();
        }

        private void OnGUI()
        {
            if (_game == null || _input == null || _game.State != CalafiaRushGameState.Running) return;

            _input.SetLeftHeld(false);
            _input.SetRightHeld(false);
            _input.SetAccelerateHeld(false);

            BuildImGuiStyles();
            DrawHud();
            DrawControls();
        }

        private void CachePanelReferences()
        {
            _mainMenuPanel = transform.Find("MainMenuPanel")?.gameObject;
            _gameUiPanel = transform.Find("GameUIPanel")?.gameObject;
            _garagePanel = transform.Find("GaragePanel")?.gameObject;

            if (_mainMenuPanel != null)
            {
                var menuOptions = _mainMenuPanel.transform.Find("MenuOptions");
                if (menuOptions != null)
                {
                    _playButton = menuOptions.Find("Play")?.GetComponent<Button>();
                    _garageButton = menuOptions.Find("Garage")?.GetComponent<Button>();
                    _settingsButton = menuOptions.Find("Settings")?.GetComponent<Button>();
                    _quitButton = menuOptions.Find("Quit")?.GetComponent<Button>();
                }
            }
        }

        private void EnsureGaragePanel()
        {
            if (_garagePanel != null) return;

            var mainMenuImage = _mainMenuPanel != null
                ? _mainMenuPanel.GetComponent<Image>()
                : null;

            _garagePanel = new GameObject("GaragePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _garagePanel.transform.SetParent(transform, false);

            var rect = _garagePanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var background = _garagePanel.GetComponent<Image>();
            if (mainMenuImage != null)
            {
                background.sprite = mainMenuImage.sprite;
                background.type = mainMenuImage.type;
            }

            background.color = mainMenuImage != null ? mainMenuImage.color : new Color(0.1f, 0.12f, 0.16f, 0.95f);
            background.raycastTarget = true;
            _garagePanel.SetActive(false);

            const float headerY = -36f;
            var headerColor = new Color(1f, 0.82f, 0.15f);

            var titleObject = new GameObject("GarageTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(_garagePanel.transform, false);
            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(40f, headerY);
            titleRect.sizeDelta = new Vector2(360f, 48f);
            var titleText = titleObject.GetComponent<TextMeshProUGUI>();
            titleText.text = "CALAFIA GARAGE";
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.fontSize = 34f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = headerColor;

            var pointsObject = new GameObject("GaragePoints", typeof(RectTransform), typeof(TextMeshProUGUI));
            pointsObject.transform.SetParent(_garagePanel.transform, false);
            var pointsRect = pointsObject.GetComponent<RectTransform>();
            pointsRect.anchorMin = new Vector2(0.5f, 1f);
            pointsRect.anchorMax = new Vector2(1f, 1f);
            pointsRect.pivot = new Vector2(1f, 1f);
            pointsRect.anchoredPosition = new Vector2(-40f, headerY);
            pointsRect.sizeDelta = new Vector2(360f, 48f);
            _garagePointsLabel = pointsObject.GetComponent<TextMeshProUGUI>();
            _garagePointsLabel.alignment = TextAlignmentOptions.Right;
            _garagePointsLabel.fontSize = 28f;
            _garagePointsLabel.fontStyle = FontStyles.Bold;
            _garagePointsLabel.color = headerColor;

            var listFrameObject = new GameObject("SkinListFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            listFrameObject.transform.SetParent(_garagePanel.transform, false);
            var listFrameRect = listFrameObject.GetComponent<RectTransform>();
            listFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
            listFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
            listFrameRect.pivot = new Vector2(0.5f, 0.5f);
            listFrameRect.anchoredPosition = new Vector2(0f, -10f);
            listFrameRect.sizeDelta = new Vector2(540f, 340f);
            var listFrameImage = listFrameObject.GetComponent<Image>();
            ApplyPanelSprite(listFrameImage, _garageListPanelSprite);
            listFrameImage.color = new Color(0.1f, 0.12f, 0.16f, 0.78f);
            listFrameImage.raycastTarget = false;

            var listObject = new GameObject("SkinList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listObject.transform.SetParent(listFrameObject.transform, false);
            var listRect = listObject.GetComponent<RectTransform>();
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = Vector2.one;
            listRect.offsetMin = new Vector2(18f, 18f);
            listRect.offsetMax = new Vector2(-18f, -18f);
            var layout = listObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            _garageSkinList = listObject.transform;

            _garageBackButton = InstantiatePrefabButton(_secondaryButtonPrefab, _garagePanel.transform, "Back",
                new Vector2(0f, 40f), new Vector2(220f, 50f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f));
        }

        private Button CreateMenuButton(string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Transform parent)
        {
            var buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 24f;

            return buttonObject.GetComponent<Button>();
        }

        private void WireMenuButtons()
        {
            if (_playButton != null) _playButton.onClick.AddListener(OnPlayClicked);
            if (_garageButton != null) _garageButton.onClick.AddListener(ToggleGarage);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettingsClicked);
            if (_quitButton != null) _quitButton.onClick.AddListener(OnQuitClicked);
            if (_garageBackButton != null) _garageBackButton.onClick.AddListener(HideGarage);
        }

        private void EnsureGameOverPanel()
        {
            if (_gameOverPanel != null) return;

            _gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _gameOverPanel.transform.SetParent(transform, false);

            var rect = _gameOverPanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var background = _gameOverPanel.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.55f);

            var summaryObject = new GameObject("Summary", typeof(RectTransform), typeof(TextMeshProUGUI));
            summaryObject.transform.SetParent(_gameOverPanel.transform, false);
            var summaryRect = summaryObject.GetComponent<RectTransform>();
            summaryRect.anchorMin = new Vector2(0.5f, 0.5f);
            summaryRect.anchorMax = new Vector2(0.5f, 0.5f);
            summaryRect.pivot = new Vector2(0.5f, 0.5f);
            summaryRect.anchoredPosition = new Vector2(0f, 40f);
            summaryRect.sizeDelta = new Vector2(520f, 180f);
            _gameOverSummary = summaryObject.GetComponent<TextMeshProUGUI>();
            _gameOverSummary.alignment = TextAlignmentOptions.Center;
            _gameOverSummary.fontSize = 24f;

            _retryButton = InstantiatePrefabButton(_primaryButtonPrefab, _gameOverPanel.transform, "RUN IT AGAIN",
                new Vector2(-120f, -80f), new Vector2(220f, 50f));
            _menuButton = InstantiatePrefabButton(_secondaryButtonPrefab, _gameOverPanel.transform, "GARAGE / MENU",
                new Vector2(120f, -80f), new Vector2(220f, 50f));
            _gameOverPanel.SetActive(false);
        }

        private Button InstantiatePrefabButton(Button prefab, Transform parent, string label, Vector2 anchoredPosition,
            Vector2 sizeDelta, Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            var anchor = anchorMin ?? new Vector2(0.5f, 0.5f);
            var anchorMaxValue = anchorMax ?? anchor;
            var pivotValue = pivot ?? new Vector2(0.5f, 0.5f);

            if (prefab == null)
            {
                Debug.LogWarning("CalafiaRushUI is missing a button prefab reference for \"" + label + "\".");
                return CreateMenuButton(label, anchor, anchoredPosition, sizeDelta, parent);
            }

            var instance = Instantiate(prefab, parent);
            instance.name = label.Replace(" ", string.Empty) + "Button";

            var rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchorMaxValue;
            rect.pivot = pivotValue;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            SetButtonLabel(instance, label);
            return instance;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) text.text = label;
        }

        private GameObject CreateSkinSwatch(Transform parent, int skinIndex)
        {
            var swatchRoot = new GameObject("Swatch", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            swatchRoot.transform.SetParent(parent, false);

            var swatchRect = swatchRoot.GetComponent<RectTransform>();
            swatchRect.sizeDelta = new Vector2(46f, 32f);

            var swatchLayout = swatchRoot.GetComponent<HorizontalLayoutGroup>();
            swatchLayout.spacing = 4f;
            swatchLayout.childAlignment = TextAnchor.MiddleLeft;
            swatchLayout.childControlWidth = false;
            swatchLayout.childControlHeight = false;
            swatchLayout.childForceExpandWidth = false;
            swatchLayout.childForceExpandHeight = false;

            CreateSwatchImage(swatchRoot.transform, "Body", new Vector2(32f, 32f), _game.GetSkinBodyColor(skinIndex));
            CreateSwatchImage(swatchRoot.transform, "Stripe", new Vector2(10f, 32f), _game.GetSkinStripeColor(skinIndex));
            return swatchRoot;
        }

        private void CreateSwatchImage(Transform parent, string name, Vector2 size, Color color)
        {
            var swatchObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            swatchObject.transform.SetParent(parent, false);

            var rect = swatchObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            var image = swatchObject.GetComponent<Image>();
            ApplyPanelSprite(image, _garageSkinSwatchSprite);
            image.color = color;
            image.raycastTarget = false;
        }

        private static void ApplyPanelSprite(Image image, Sprite sprite)
        {
            image.sprite = sprite != null ? sprite : GetBuiltinUISprite();
            image.type = Image.Type.Sliced;
        }

        private static Sprite GetBuiltinUISprite()
        {
            return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static void PinLeft(RectTransform rect, float padding, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(padding, 0f);
            rect.sizeDelta = size;
        }

        private static void PinRight(RectTransform rect, float padding, Vector2 size)
        {
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-padding, 0f);
            rect.sizeDelta = size;
        }

        private static void StretchHorizontal(RectTransform rect, float leftInset, float rightInset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(leftInset, 0f);
            rect.offsetMax = new Vector2(-rightInset, 0f);
            rect.anchoredPosition = Vector2.zero;
        }

        private void WireGameOverButtons()
        {
            EnsureGameOverPanel();
            if (_retryButton != null) _retryButton.onClick.AddListener(OnPlayClicked);
            if (_menuButton != null) _menuButton.onClick.AddListener(OnReturnToMenuClicked);
        }

        private void BuildGarageSkinList()
        {
            if (_game == null || _garageSkinList == null) return;

            for (var i = _garageSkinList.childCount - 1; i >= 0; i--)
                Destroy(_garageSkinList.GetChild(i).gameObject);

            for (var i = 0; i < _game.SkinCount; i++)
            {
                var index = i;
                const float rowHeight = 52f;
                const float horizontalPadding = 4f;
                const float spacing = 12f;
                const float swatchWidth = 46f;
                const float swatchHeight = 32f;
                const float buttonWidth = 140f;
                const float buttonHeight = 44f;
                var nameLeftInset = horizontalPadding + swatchWidth + spacing;
                var nameRightInset = horizontalPadding + buttonWidth + spacing;

                var row = new GameObject("SkinRow" + index, typeof(RectTransform), typeof(LayoutElement));
                row.transform.SetParent(_garageSkinList, false);
                var rowRect = row.GetComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(0f, rowHeight);
                var rowLayoutElement = row.GetComponent<LayoutElement>();
                rowLayoutElement.preferredHeight = rowHeight;
                rowLayoutElement.flexibleWidth = 1f;

                var swatch = CreateSkinSwatch(row.transform, index);
                PinLeft(swatch.GetComponent<RectTransform>(), horizontalPadding, new Vector2(swatchWidth, swatchHeight));

                var nameObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
                nameObject.transform.SetParent(row.transform, false);
                StretchHorizontal(nameObject.GetComponent<RectTransform>(), nameLeftInset, nameRightInset);
                var nameText = nameObject.GetComponent<TextMeshProUGUI>();
                nameText.text = _game.GetSkinName(index);
                nameText.fontSize = 22f;
                nameText.fontStyle = FontStyles.Bold;
                nameText.color = Color.white;
                nameText.alignment = TextAlignmentOptions.MidlineLeft;
                nameText.enableWordWrapping = false;
                nameText.overflowMode = TextOverflowModes.Ellipsis;

                var actionButton = InstantiatePrefabButton(_secondaryButtonPrefab, row.transform,
                    GetGarageActionLabel(index), Vector2.zero, new Vector2(buttonWidth, buttonHeight));
                PinRight(actionButton.GetComponent<RectTransform>(), horizontalPadding,
                    new Vector2(buttonWidth, buttonHeight));

                var owned = _game.OwnsSkin(index);
                var canUse = _game.SelectedSkinIndex != index && (owned || _game.GaragePoints >= _game.GetSkinPrice(index));
                actionButton.interactable = canUse;
                actionButton.onClick.AddListener(() =>
                {
                    _game.SelectOrBuySkin(index);
                    RefreshGarage();
                });
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_garageSkinList.GetComponent<RectTransform>());
            RefreshGaragePointsLabel();
        }

        private string GetGarageActionLabel(int index)
        {
            if (_game.SelectedSkinIndex == index) return "EQUIPPED";
            return _game.OwnsSkin(index) ? "EQUIP" : _game.GetSkinPrice(index) + " PTS";
        }

        private void RefreshGarage()
        {
            RefreshGaragePointsLabel();
            BuildGarageSkinList();
        }

        private void RefreshGaragePointsLabel()
        {
            if (_garagePointsLabel == null || _game == null) return;
            _garagePointsLabel.text = "POINTS  " + _game.GaragePoints;
        }

        private void OnPlayClicked()
        {
            HideGarage();
            _game.StartGame();
        }

        private void OnReturnToMenuClicked()
        {
            _game.ReturnToTitle();
        }

        private void ToggleGarage()
        {
            if (_garageVisible) HideGarage();
            else ShowGarage();
        }

        private void ShowGarage()
        {
            _garageVisible = true;
            if (_garagePanel != null) _garagePanel.SetActive(true);
            RefreshGarage();
        }

        private void HideGarage()
        {
            _garageVisible = false;
            if (_garagePanel != null) _garagePanel.SetActive(false);
        }

        private static void OnSettingsClicked()
        {
            Debug.Log("CalafiaRush settings are not implemented yet.");
        }

        private static void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnGameStateChanged(CalafiaRushGameState state)
        {
            switch (state)
            {
                case CalafiaRushGameState.Title:
                    ShowTitleScreen();
                    break;
                case CalafiaRushGameState.Running:
                    ShowGameplayScreen();
                    break;
                case CalafiaRushGameState.GameOver:
                    ShowGameOverScreen();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void ShowTitleScreen()
        {
            HideGarage();
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(true);
            if (_gameUiPanel != null) _gameUiPanel.SetActive(false);
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
            RefreshGaragePointsLabel();
        }

        private void ShowGameplayScreen()
        {
            HideGarage();
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
            if (_gameUiPanel != null) _gameUiPanel.SetActive(true);
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
        }

        private void ShowGameOverScreen()
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
            if (_gameUiPanel != null) _gameUiPanel.SetActive(true);
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
                if (_gameOverSummary != null)
                {
                    _gameOverSummary.text = "ROUTE FINISHED\n\nScore banked: " + _game.Score +
                                             "\nGarage Points: " + _game.GaragePoints +
                                             "\nPassengers: " + _game.Passengers +
                                             "    Laps: " + (_game.Lap - 1);
                }
            }

            RefreshGaragePointsLabel();
        }

        private void DrawHud()
        {
            GUI.Box(new Rect(12f, 12f, Screen.width - 24f, 62f), string.Empty);
            GUI.Label(new Rect(24f, 19f, Screen.width - 48f, 52f),
                "TIME  " + Mathf.CeilToInt(_game.TimeLeft).ToString("00") +
                "     SCORE  " + _game.Score.ToString("00000") +
                "     GARAGE  " + _game.GaragePoints +
                "     RIDERS  " + _game.Passengers + "/12" +
                "     CASH  $" + _game.Money +
                "     LAP  " + _game.Lap +
                "     SPEED  " + Mathf.RoundToInt(_game.Speed * 4.2f) + " km/h" +
                (_game.DriftAmount > 0.2f ? "     DRIFT" : string.Empty), _hudStyle);

            if (_game.IsMessageVisible)
                GUI.Label(new Rect(Screen.width / 2f - 360f, 86f, 720f, 50f), _game.Message, _centerStyle);
        }

        private void DrawControls()
        {
            var y = Screen.height - 78f;
            if (GUI.Button(new Rect(18f, y, 95f, 58f), "LEFT", _buttonStyle))
                _input.PulseLaneLeft();
            if (GUI.Button(new Rect(123f, y, 95f, 58f), "RIGHT", _buttonStyle))
                _input.PulseLaneRight();
            if (GUI.RepeatButton(new Rect(Screen.width - 218f, y, 95f, 58f), "GAS", _buttonStyle))
                _input.SetAccelerateHeld(true);
            if (GUI.Button(new Rect(Screen.width - 113f, y, 95f, 58f), "PAY $10", _buttonStyle))
                _game.TryBribe();
        }

        private void BuildImGuiStyles()
        {
            if (_hudStyle != null) return;
            _hudStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            _centerStyle = new GUIStyle(_hudStyle)
            {
                fontSize = 23,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.88f, 0.28f) }
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }
}
