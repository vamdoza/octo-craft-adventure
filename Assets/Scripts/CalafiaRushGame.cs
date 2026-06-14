using System.Collections.Generic;
using UnityEngine;

namespace CalafiaRush
{
    public sealed class CalafiaRushGame : MonoBehaviour
    {
        private enum GameState { Title, Running, GameOver }
        private enum RoadItemType { Traffic, Passenger, Light, Cop, Coin }

        private sealed class RoadItem
        {
            public GameObject gameObject;
            public RoadItemType type;
            public int lane;
            public bool resolved;
            public float phase;
        }

        private static readonly float[] LaneX = { -3.2f, 0f, 3.2f };
        private readonly List<RoadItem> _items = new List<RoadItem>();
        private readonly List<Transform> _roadSegments = new List<Transform>();

        private GameObject _bus;
        private Transform _busBody;
        private Texture2D _keyArt;
        private GameState _state = GameState.Title;
        private int _lane = 1;
        private int _passengers;
        private int _score;
        private int _lap = 1;
        private int _money = 30;
        private float _speed;
        private float _timeLeft = 75f;
        private float _distance;
        private float _spawnDistance;
        private float _messageUntil;
        private float _blockedUntil;
        private string _message = string.Empty;
        private bool _leftHeld;
        private bool _rightHeld;
        private bool _accelerateHeld;
        private GUIStyle _titleStyle;
        private GUIStyle _hudStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _buttonStyle;
        
        private void Awake()
        {
            _keyArt = Resources.Load<Texture2D>("CalafiaRushKeyArt");
            BuildWorld();
        }

        private void BuildWorld()
        {
            RenderSettings.ambientLight = new Color(0.62f, 0.68f, 0.74f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.54f, 0.76f, 0.86f);
            RenderSettings.fogDensity = 0.012f;

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 11.5f, -11f);
            cameraObject.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
            camera.fieldOfView = 55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.43f, 0.72f, 0.86f);

            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.91f, 0.72f);
            light.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            for (var i = 0; i < 7; i++)
            {
                var segment = CreateCube("Road Segment", new Vector3(0f, 0f, i * 12f - 10f),
                    new Vector3(11f, 0.15f, 12f), new Color(0.17f, 0.19f, 0.22f));
                _roadSegments.Add(segment.transform);
                CreateRoadDetails(segment.transform, i);
            }

            _bus = CreateBus();
            _bus.transform.position = new Vector3(0f, 0.7f, -3.5f);
        }

        private void CreateRoadDetails(Transform segment, int index)
        {
            for (var laneDivider = -1; laneDivider <= 1; laneDivider += 2)
            {
                for (var stripe = 0; stripe < 3; stripe++)
                {
                    var marker = CreateCube("Lane Marker", new Vector3(laneDivider * 1.6f, 0.1f,
                            segment.position.z - 4f + stripe * 4f), new Vector3(0.12f, 0.03f, 2f),
                        new Color(1f, 0.91f, 0.45f));
                    marker.transform.SetParent(segment, true);
                }
            }

            foreach (var side in new[] { -1f, 1f })
            {
                var sidewalk = CreateCube("Sidewalk", new Vector3(side * 6.5f, 0.12f, segment.position.z),
                    new Vector3(2f, 0.28f, 12f), new Color(0.66f, 0.57f, 0.49f));
                sidewalk.transform.SetParent(segment, true);

                var buildingColor = Color.HSVToRGB((index * 0.13f + (side > 0 ? 0.06f : 0f)) % 1f, 0.58f, 0.88f);
                var building = CreateCube("Colorful Building",
                    new Vector3(side * 8.4f, 1.5f + index % 3, segment.position.z + (index % 2 == 0 ? 2f : -2f)),
                    new Vector3(2.1f, 3f + (index % 3) * 1.2f, 5f), buildingColor);
                building.transform.SetParent(segment, true);
            }
        }

        private GameObject CreateBus()
        {
            var root = new GameObject("Calafia");
            _busBody = CreateCube("Bus Body", Vector3.zero, new Vector3(2.2f, 1.2f, 4.2f),
                new Color(0.92f, 0.94f, 0.91f)).transform;
            _busBody.SetParent(root.transform, false);
            _busBody.localPosition = new Vector3(0f, 0.55f, 0f);

            var stripe = CreateCube("Blue Stripe", Vector3.zero, new Vector3(2.28f, 0.28f, 4.25f),
                new Color(0.04f, 0.45f, 0.75f));
            stripe.transform.SetParent(root.transform, false);
            stripe.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            var windshield = CreateCube("Windshield", Vector3.zero, new Vector3(1.75f, 0.55f, 0.08f),
                new Color(0.08f, 0.19f, 0.25f));
            windshield.transform.SetParent(root.transform, false);
            windshield.transform.localPosition = new Vector3(0f, 0.85f, -2.13f);

            for (var z = -1.25f; z <= 1.25f; z += 0.85f)
            {
                foreach (var side in new[] { -1f, 1f })
                {
                    var window = CreateCube("Window", Vector3.zero, new Vector3(0.06f, 0.48f, 0.62f),
                        new Color(0.1f, 0.25f, 0.31f));
                    window.transform.SetParent(root.transform, false);
                    window.transform.localPosition = new Vector3(side * 1.12f, 0.85f, z);
                }
            }

            foreach (var x in new[] { -0.82f, 0.82f })
            {
                foreach (var z in new[] { -1.35f, 1.35f })
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = "Wheel";
                    wheel.transform.SetParent(root.transform, false);
                    wheel.transform.localPosition = new Vector3(x, 0.1f, z);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    wheel.transform.localScale = new Vector3(0.45f, 0.18f, 0.45f);
                    wheel.GetComponent<Renderer>().material.color = new Color(0.04f, 0.04f, 0.04f);
                }
            }

            return root;
        }

        private void Update()
        {
            if (_state != GameState.Running)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) StartGame();
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || _leftHeld) ChangeLane(-1);
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || _rightHeld) ChangeLane(1);

            var accelerating = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) || _accelerateHeld;
            var braking = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            var targetSpeed = accelerating ? 24f : 10f;
            if (braking) targetSpeed = 2.5f;
            if (Time.time < _blockedUntil) targetSpeed = 0f;
            _speed = Mathf.MoveTowards(_speed, targetSpeed, Time.deltaTime * (accelerating ? 10f : 7f));

            if (Input.GetKeyDown(KeyCode.B)) TryBribe();

            var targetX = LaneX[_lane];
            var position = _bus.transform.position;
            position.x = Mathf.MoveTowards(position.x, targetX, Time.deltaTime * 9f);
            _bus.transform.position = position;
            _busBody.localRotation = Quaternion.Slerp(_busBody.localRotation,
                Quaternion.Euler(0f, 0f, (targetX - position.x) * -4f), Time.deltaTime * 7f);

            var movement = _speed * Time.deltaTime;
            _distance += movement;
            _spawnDistance += movement;
            MoveWorld(movement);
            UpdateItems(movement);

            if (_spawnDistance >= 11f)
            {
                _spawnDistance = 0f;
                SpawnPattern();
            }

            if (_distance >= 550f)
            {
                _distance -= 550f;
                _lap++;
                _score += 250 + _passengers * 20;
                _timeLeft += 12f;
                ShowMessage("LAP " + _lap + "  +12 SECONDS");
            }

            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f)
            {
                _timeLeft = 0f;
                _state = GameState.GameOver;
                _speed = 0f;
            }
        }

        private void MoveWorld(float movement)
        {
            foreach (var segment in _roadSegments)
            {
                segment.position += Vector3.back * movement;
                if (segment.position.z < -22f) segment.position += Vector3.forward * 84f;
            }
        }

        private void SpawnPattern()
        {
            var roll = Random.value;
            if (roll < 0.35f)
            {
                SpawnPassenger(Random.Range(0, 3));
            }
            else if (roll < 0.67f)
            {
                SpawnTraffic(Random.Range(0, 3));
                if (Random.value < 0.35f) SpawnTraffic(Random.Range(0, 3));
            }
            else if (roll < 0.82f)
            {
                SpawnLight();
            }
            else if (roll < 0.92f)
            {
                SpawnCop();
            }
            else
            {
                SpawnCoin(Random.Range(0, 3));
            }
        }

        private void SpawnPassenger(int lane)
        {
            var side = lane == 0 ? -1f : 1f;
            var passenger = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            passenger.name = "Waiting Passenger";
            passenger.transform.position = new Vector3(LaneX[lane] + side * 1.1f, 0.85f, 42f);
            passenger.transform.localScale = new Vector3(0.55f, 0.85f, 0.55f);
            passenger.GetComponent<Renderer>().material.color = Color.HSVToRGB(Random.value, 0.7f, 0.95f);
            AddItem(passenger, RoadItemType.Passenger, lane);
        }

        private void SpawnTraffic(int lane)
        {
            var car = CreateCube("Traffic", new Vector3(LaneX[lane], 0.55f, 42f),
                new Vector3(1.8f, 1f, 3.4f), Color.HSVToRGB(Random.value, 0.65f, 0.9f));
            var glass = CreateCube("Traffic Windshield", Vector3.zero, new Vector3(1.5f, 0.45f, 0.08f),
                new Color(0.08f, 0.16f, 0.2f));
            glass.transform.SetParent(car.transform, false);
            glass.transform.localPosition = new Vector3(0f, 0.25f, -1.72f);
            AddItem(car, RoadItemType.Traffic, lane);
        }

        private void SpawnLight()
        {
            var root = new GameObject("Traffic Light");
            root.transform.position = new Vector3(0f, 0f, 44f);
            foreach (var side in new[] { -1f, 1f })
            {
                var pole = CreateCube("Pole", Vector3.zero, new Vector3(0.18f, 4f, 0.18f), new Color(0.2f, 0.2f, 0.2f));
                pole.transform.SetParent(root.transform, false);
                pole.transform.localPosition = new Vector3(side * 5.2f, 2f, 0f);
                var arm = CreateCube("Arm", Vector3.zero, new Vector3(5f, 0.16f, 0.16f), new Color(0.2f, 0.2f, 0.2f));
                arm.transform.SetParent(root.transform, false);
                arm.transform.localPosition = new Vector3(side * 2.7f, 3.8f, 0f);
            }
            var signal = CreateCube("Signal", Vector3.zero, new Vector3(0.7f, 1.5f, 0.5f), new Color(0.06f, 0.06f, 0.06f));
            signal.transform.SetParent(root.transform, false);
            signal.transform.localPosition = new Vector3(0f, 3.35f, 0f);
            AddItem(root, RoadItemType.Light, -1).phase = Random.Range(0f, 4f);
        }

        private void SpawnCop()
        {
            var root = new GameObject("Police Checkpoint");
            root.transform.position = new Vector3(0f, 0f, 46f);
            var barrier = CreateCube("Checkpoint Barrier", Vector3.zero, new Vector3(9f, 0.3f, 0.35f),
                new Color(0.95f, 0.86f, 0.24f));
            barrier.transform.SetParent(root.transform, false);
            barrier.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var officer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            officer.name = "Officer";
            officer.transform.SetParent(root.transform, false);
            officer.transform.localPosition = new Vector3(4.2f, 1f, 0f);
            officer.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            officer.GetComponent<Renderer>().material.color = new Color(0.1f, 0.18f, 0.42f);
            AddItem(root, RoadItemType.Cop, -1);
        }

        private void SpawnCoin(int lane)
        {
            var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Fare Bonus";
            coin.transform.position = new Vector3(LaneX[lane], 1f, 42f);
            coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            coin.transform.localScale = new Vector3(0.65f, 0.12f, 0.65f);
            coin.GetComponent<Renderer>().material.color = new Color(1f, 0.78f, 0.12f);
            AddItem(coin, RoadItemType.Coin, lane);
        }

        private RoadItem AddItem(GameObject gameObject, RoadItemType type, int lane)
        {
            var item = new RoadItem { gameObject = gameObject, type = type, lane = lane };
            _items.Add(item);
            return item;
        }

        private void UpdateItems(float movement)
        {
            for (var i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (!item.gameObject)
                {
                    _items.RemoveAt(i);
                    continue;
                }

                item.gameObject.transform.position += Vector3.back * movement;
                var z = item.gameObject.transform.position.z;
                if (item.type == RoadItemType.Coin)
                    item.gameObject.transform.Rotate(0f, Time.deltaTime * 180f, 0f, Space.World);

                if (item.type == RoadItemType.Light)
                {
                    var red = IsLightRed(item);
                    item.gameObject.transform.Find("Signal").GetComponent<Renderer>().material.color =
                        red ? new Color(1f, 0.08f, 0.04f) : new Color(0.08f, 0.95f, 0.24f);
                    if (!item.resolved && z < -1f)
                    {
                        item.resolved = true;
                        if (red && _speed > 5f)
                        {
                            _timeLeft = Mathf.Max(0f, _timeLeft - 6f);
                            _speed = 3f;
                            ShowMessage("RED LIGHT!  -6 SECONDS");
                        }
                    }
                }
                else if (item.type == RoadItemType.Cop && !item.resolved && z < 2f)
                {
                    item.resolved = true;
                    _blockedUntil = Time.time + 8f;
                    ShowMessage("CHECKPOINT! PRESS B TO PAY $10");
                }
                else if (!item.resolved && Mathf.Abs(z + 3.5f) < 1.8f && item.lane == _lane)
                {
                    ResolveLaneItem(item);
                }

                if (z < -18f)
                {
                    Destroy(item.gameObject);
                    _items.RemoveAt(i);
                }
            }
        }

        private bool IsLightRed(RoadItem item)
        {
            return (Time.time + item.phase) % 7f < 4.2f;
        }

        private void ResolveLaneItem(RoadItem item)
        {
            item.resolved = true;
            switch (item.type)
            {
                case RoadItemType.Passenger:
                    if (_passengers >= 12)
                    {
                        ShowMessage("CALAFIA FULL!");
                    }
                    else if (_speed <= 13f)
                    {
                        _passengers++;
                        _score += 100;
                        _money += 4;
                        ShowMessage("PASSENGER ABOARD  +100  +$4");
                        Destroy(item.gameObject);
                    }
                    else
                    {
                        ShowMessage("TOO FAST! SLOW DOWN TO PICK UP");
                    }
                    break;
                case RoadItemType.Traffic:
                    _speed = 2f;
                    _timeLeft = Mathf.Max(0f, _timeLeft - 4f);
                    _score = Mathf.Max(0, _score - 50);
                    ShowMessage("TRAFFIC HIT  -4 SECONDS");
                    Destroy(item.gameObject);
                    break;
                case RoadItemType.Coin:
                    _money += 8;
                    _score += 40;
                    ShowMessage("EXTRA FARES  +$8");
                    Destroy(item.gameObject);
                    break;
            }
        }

        private void TryBribe()
        {
            if (Time.time >= _blockedUntil) return;
            if (_money >= 10)
            {
                _money -= 10;
                _blockedUntil = Time.time;
                _score += 25;
                ShowMessage("MORDIDA PAID. VAMONOS!");
            }
            else
            {
                ShowMessage("NOT ENOUGH CASH. WAIT IT OUT!");
            }
        }

        private void ChangeLane(int direction)
        {
            _lane = Mathf.Clamp(_lane + direction, 0, 2);
            _leftHeld = false;
            _rightHeld = false;
        }

        private void StartGame()
        {
            foreach (var item in _items) if (item.gameObject) Destroy(item.gameObject);
            _items.Clear();
            _lane = 1;
            _passengers = 0;
            _score = 0;
            _lap = 1;
            _money = 30;
            _speed = 0f;
            _timeLeft = 75f;
            _distance = 0f;
            _spawnDistance = 0f;
            _blockedUntil = 0f;
            _state = GameState.Running;
            ShowMessage("PICK UP PASSENGERS. COMPLETE THE ROUTE!");
        }

        private void ShowMessage(string message)
        {
            _message = message;
            _messageUntil = Time.time + 2.4f;
        }

        private void OnGUI()
        {
            BuildStyles();
            _leftHeld = _rightHeld = _accelerateHeld = false;

            if (_state == GameState.Title)
            {
                DrawTitle();
                return;
            }

            DrawHud();
            DrawControls();

            if (_state == GameState.GameOver)
            {
                GUI.Box(new Rect(Screen.width / 2f - 230f, Screen.height / 2f - 145f, 460f, 290f), string.Empty);
                GUI.Label(new Rect(Screen.width / 2f - 210f, Screen.height / 2f - 120f, 420f, 55f), "ROUTE FINISHED", _titleStyle);
                GUI.Label(new Rect(Screen.width / 2f - 180f, Screen.height / 2f - 50f, 360f, 110f),
                    "Score: " + _score + "\nPassengers: " + _passengers + "\nLaps: " + (_lap - 1), _centerStyle);
                if (GUI.Button(new Rect(Screen.width / 2f - 110f, Screen.height / 2f + 70f, 220f, 52f), "RUN IT AGAIN", _buttonStyle))
                    StartGame();
            }
        }

        private void DrawTitle()
        {
            if (_keyArt) GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _keyArt, ScaleMode.ScaleAndCrop);
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), string.Empty);
            GUI.Label(new Rect(40f, 35f, Screen.width - 80f, 80f), "CALAFIA RUSH", _titleStyle);
            GUI.Label(new Rect(55f, 115f, 480f, 150f),
                "Race the route. Pick up passengers.\nDodge traffic. Respect the lights.\nKeep cash ready for checkpoints.", _hudStyle);
            GUI.Label(new Rect(55f, Screen.height - 160f, 560f, 90f),
                "MOVE: A / D or ARROWS     ACCELERATE: W / UP\nBRAKE: S / DOWN     CHECKPOINT: B", _hudStyle);
            if (GUI.Button(new Rect(55f, Screen.height - 75f, 240f, 52f), "START THE ROUTE", _buttonStyle)) StartGame();
        }

        private void DrawHud()
        {
            GUI.Box(new Rect(12f, 12f, Screen.width - 24f, 62f), string.Empty);
            GUI.Label(new Rect(24f, 19f, Screen.width - 48f, 52f),
                "TIME  " + Mathf.CeilToInt(_timeLeft).ToString("00") +
                "     SCORE  " + _score.ToString("00000") +
                "     RIDERS  " + _passengers + "/12" +
                "     CASH  $" + _money +
                "     LAP  " + _lap +
                "     SPEED  " + Mathf.RoundToInt(_speed * 4.2f) + " km/h", _hudStyle);

            if (Time.time < _messageUntil)
                GUI.Label(new Rect(Screen.width / 2f - 360f, 86f, 720f, 50f), _message, _centerStyle);
        }

        private void DrawControls()
        {
            var y = Screen.height - 78f;
            if (GUI.RepeatButton(new Rect(18f, y, 95f, 58f), "LEFT", _buttonStyle)) _leftHeld = true;
            if (GUI.RepeatButton(new Rect(123f, y, 95f, 58f), "RIGHT", _buttonStyle)) _rightHeld = true;
            if (GUI.RepeatButton(new Rect(Screen.width - 218f, y, 95f, 58f), "GAS", _buttonStyle)) _accelerateHeld = true;
            if (GUI.Button(new Rect(Screen.width - 113f, y, 95f, 58f), "PAY $10", _buttonStyle)) TryBribe();
        }

        private void BuildStyles()
        {
            if (_titleStyle != null) return;
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.82f, 0.15f) }
            };
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

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Color color)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().material.color = color;
            return gameObject;
        }
    }
}
