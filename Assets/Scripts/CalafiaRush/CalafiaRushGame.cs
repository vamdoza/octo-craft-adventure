using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CalafiaRush
{
    public enum CalafiaRushGameState
    {
        Title,
        Running,
        GameOver
    }

    [DefaultExecutionOrder(0)]
    public sealed class CalafiaRushGame : MonoBehaviour
    {
        private enum RoadItemType { Traffic, Passenger, Light, Cop, Coin }

        private sealed class RoadItem
        {
            public GameObject gameObject;
            public RoadItemType type;
            public int lane;
            public bool resolved;
            public float phase;
            public Transform barrierPivot;
            public bool checkpointCleared;
            public float barrierAngle;
            public float trafficSpeed;
            public float trafficTargetSpeed;
        }

        private sealed class BusSkin
        {
            public readonly string name;
            public readonly int price;
            public readonly Color bodyColor;
            public readonly Color stripeColor;

            public BusSkin(string name, int price, Color bodyColor, Color stripeColor)
            {
                this.name = name;
                this.price = price;
                this.bodyColor = bodyColor;
                this.stripeColor = stripeColor;
            }
        }

        private static readonly float[] LaneX = { -3.2f, 0f, 3.2f };
        private const int LeftLaneIndex = 0;
        private const int CenterLaneIndex = 1;
        private const int RightLaneIndex = 2;
        private const int PassengerLaneIndex = RightLaneIndex;
        private static readonly BusSkin[] BusSkins =
        {
            new BusSkin("Classic Azul", 0, new Color(0.92f, 0.94f, 0.91f), new Color(0.04f, 0.45f, 0.75f)),
            new BusSkin("Sunset Coral", 500, new Color(1f, 0.72f, 0.38f), new Color(0.88f, 0.18f, 0.22f)),
            new BusSkin("Neon Ruta", 1200, new Color(0.12f, 0.95f, 0.72f), new Color(0.75f, 0.08f, 0.88f)),
            new BusSkin("Baja Gold", 2500, new Color(1f, 0.82f, 0.2f), new Color(0.07f, 0.32f, 0.68f)),
            new BusSkin("Midnight TJ", 4000, new Color(0.06f, 0.08f, 0.16f), new Color(0.12f, 0.78f, 1f))
        };

        [Header("Bus Speed")]
        [SerializeField] private float _maxSpeed = 24f;
        [SerializeField] private float _cruiseSpeed = 10f;
        [SerializeField] private float _accelResponse = 5.5f;
        [SerializeField] private float _cruiseResponse = 3.2f;
        [SerializeField] private float _brakeResponse = 13f;

        [Header("Bus Steering")]
        [SerializeField] private float _gripAtLowSpeed = 14f;
        [SerializeField] private float _gripAtHighSpeed = 6f;
        [SerializeField] private float _dampingAtLowSpeed = 6.8f;
        [SerializeField] private float _dampingAtHighSpeed = 2f;
        [SerializeField] private float _maxLateralVelocity = 14f;
        [SerializeField] private float _laneImpulseAtLowSpeed = 3.5f;
        [SerializeField] private float _laneImpulseAtHighSpeed = 5.5f;
        [SerializeField] private float _brakeHandlingSpeedThreshold = 13f;
        [SerializeField] private float _brakeGripMultiplier = 0.38f;
        [SerializeField] private float _brakeDampingMultiplier = 0.42f;

        [Header("Bus Visuals")]
        [SerializeField] private float _bodyTiltSlerpSpeed = 7f;

        [Header("Auto Brake")]
        [SerializeField] private bool _autoBrakeEnabled = true;
        [SerializeField] private float _autoBrakeDetectionRange = 12f;
        [SerializeField] private float _autoBrakeStopGap = 1f;
        [SerializeField] private float _autoBrakeDecelRate = 18f;
        [SerializeField] private float _autoBrakeResumeRate = 8f;
        [SerializeField] private float _autoBrakeUrgencyExponent = 1.35f;
        [SerializeField] private float _autoBrakeSpringStiffness = 9f;
        [SerializeField] private float _autoBrakeSpringDamping = 5f;
        [SerializeField] private float _checkpointBarrierLiftSpeed = 110f;

        [Header("Traffic")]
        [SerializeField] private bool _movingCarsEnabled = true;
        [SerializeField] private float _movingCarMinSpeed = 0.02f;
        [SerializeField] private float _movingCarMaxSpeed = 0.22f;
        [SerializeField] private float _movingCarAccelRate = 0.45f;
        [SerializeField] private float _movingCarBrakeRate = 2.5f;
        [SerializeField] private float _movingCarFollowGap = CalafiaRushWorldDraw.TrafficCarLength;

        private const float BusFrontZOffset = 2.1f;
        private const float TrafficRearZOffset = CalafiaRushWorldDraw.TrafficCarLength * 0.5f;
        private const float LightCycleDuration = 9f;
        private const float LightGreenDuration = 4f;
        private const float LightYellowDuration = 2f;
        private const float LightRedDuration = 3f;
        private const int MinPatternsBetweenLights = 6;
        private const int MaxPatternsBetweenLights = 10;
        private const int MinPatternsBetweenCheckpoints = 10;
        private const int MaxPatternsBetweenCheckpoints = 13;

        private enum TrafficLightPhase { Green, Yellow, Red }

        private readonly List<RoadItem> _items = new List<RoadItem>();
        private readonly List<Transform> _roadSegments = new List<Transform>();

        private CalafiaRushInput _input;
        private GameObject _bus;
        private Transform _busBody;
        private Renderer _busBodyRenderer;
        private Renderer _busStripeRenderer;
        private CalafiaRushGameState _state = CalafiaRushGameState.Title;
        private int _lane = 1;
        private int _passengers;
        private int _score;
        private int _lap = 1;
        private int _money = 30;
        private int _garagePoints;
        private int _ownedSkins = 1;
        private int _selectedSkin;
        private float _speed;
        private float _lateralVelocity;
        private float _driftAmount;
        private float _brakeDive;
        private float _timeLeft = 75f;
        private float _distance;
        private float _spawnDistance;
        private float _messageUntil;
        private float _blockedUntil;
        private string _message = string.Empty;
        private bool _scoreBanked;
        private bool _autoBrakeActive;
        private bool _autoBrakeWasActive;
        private RoadItem _pendingCheckpoint;
        private int _patternsUntilNextLight;
        private int _patternsUntilNextCheckpoint;

        public event Action<CalafiaRushGameState> StateChanged;

        public CalafiaRushGameState State => _state;
        public int Score => _score;
        public int Passengers => _passengers;
        public int Lap => _lap;
        public int Money => _money;
        public int GaragePoints => _garagePoints;
        public int SelectedSkinIndex => _selectedSkin;
        public int SkinCount => BusSkins.Length;
        public float TimeLeft => _timeLeft;
        public float Speed => _speed;
        public float DriftAmount => _driftAmount;
        public string Message => _message;
        public bool IsMessageVisible => Time.time < _messageUntil;
        public bool AutoBrakeEnabled => _autoBrakeEnabled;
        public bool AutoBrakeActive => _autoBrakeActive;

        private void Awake()
        {
            _input = GetComponent<CalafiaRushInput>();
            if (_input == null) _input = gameObject.AddComponent<CalafiaRushInput>();

            LoadGarage();
            BuildWorld();
        }

        public string GetSkinName(int index) => BusSkins[index].name;
        public int GetSkinPrice(int index) => BusSkins[index].price;
        public Color GetSkinBodyColor(int index) => BusSkins[index].bodyColor;
        public Color GetSkinStripeColor(int index) => BusSkins[index].stripeColor;
        public bool OwnsSkin(int index) => (_ownedSkins & (1 << index)) != 0;
        public void SetAutoBrakeEnabled(bool enabled)
        {
            _autoBrakeEnabled = enabled;
            SaveGarage();
        }

        public void StartGame()
        {
            foreach (var item in _items)
                if (item.gameObject) Destroy(item.gameObject);
            _items.Clear();
            _lane = 1;
            _passengers = 0;
            _score = 0;
            _lap = 1;
            _money = 30;
            _speed = 0f;
            _lateralVelocity = 0f;
            _driftAmount = 0f;
            _brakeDive = 0f;
            _bus.transform.position = new Vector3(0f, 0.7f, -3.5f);
            _busBody.localRotation = Quaternion.identity;
            _timeLeft = 75f;
            _distance = 0f;
            _spawnDistance = 0f;
            _blockedUntil = 0f;
            _scoreBanked = false;
            _autoBrakeActive = false;
            _autoBrakeWasActive = false;
            _pendingCheckpoint = null;
            _patternsUntilNextLight = Random.Range(MinPatternsBetweenLights, MaxPatternsBetweenLights + 1);
            _patternsUntilNextCheckpoint = Random.Range(MinPatternsBetweenCheckpoints, MaxPatternsBetweenCheckpoints + 1);
            SetState(CalafiaRushGameState.Running);
            ShowMessage("PICK UP PASSENGERS. COMPLETE THE ROUTE!");
        }

        public void ReturnToTitle()
        {
            SetState(CalafiaRushGameState.Title);
        }

        public void TryBribe()
        {
            if (_pendingCheckpoint == null || _pendingCheckpoint.checkpointCleared) return;
            if (_money >= 10)
            {
                _money -= 10;
                _score += 25;
                ClearCheckpoint(_pendingCheckpoint);
                ShowMessage("MORDIDA PAID. VAMONOS!");
            }
            else
            {
                ShowMessage("NOT ENOUGH CASH. PAY $10!");
            }
        }

        public void SelectOrBuySkin(int index)
        {
            index = Mathf.Clamp(index, 0, BusSkins.Length - 1);
            var skin = BusSkins[index];
            if (!OwnsSkin(index))
            {
                if (_garagePoints < skin.price) return;
                _garagePoints -= skin.price;
                _ownedSkins |= 1 << index;
            }

            _selectedSkin = index;
            ApplySelectedSkin();
            SaveGarage();
        }

        private void SetState(CalafiaRushGameState state)
        {
            if (_state == state) return;
            _state = state;
            StateChanged?.Invoke(_state);
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
                var segment = CalafiaRushWorldDraw.DrawRoadSegment(new Vector3(0f, 0f, i * 12f - 10f),
                    new Vector3(11f, 0.15f, 12f), new Color(0.17f, 0.19f, 0.22f));
                _roadSegments.Add(segment.transform);
                DrawRoadDetails(segment.transform, i);
            }

            var skin = BusSkins[_selectedSkin];
            var busVisual = CalafiaRushWorldDraw.DrawBus(skin.bodyColor, skin.stripeColor);
            _bus = busVisual.root;
            _busBody = busVisual.body;
            _busBodyRenderer = busVisual.bodyRenderer;
            _busStripeRenderer = busVisual.stripeRenderer;
            _bus.transform.position = new Vector3(0f, 0.7f, -3.5f);
        }

        private static void DrawRoadDetails(Transform segment, int index)
        {
            for (var laneDivider = -1; laneDivider <= 1; laneDivider += 2)
            {
                for (var stripe = 0; stripe < 3; stripe++)
                {
                    CalafiaRushWorldDraw.DrawLaneMarker(segment,
                        new Vector3(laneDivider * 1.6f, 0.1f, segment.position.z - 4f + stripe * 4f),
                        new Vector3(0.12f, 0.03f, 2f), new Color(1f, 0.91f, 0.45f));
                }
            }

            foreach (var side in new[] { -1f, 1f })
            {
                CalafiaRushWorldDraw.DrawSidewalk(segment,
                    new Vector3(side * 6.5f, 0.12f, segment.position.z),
                    new Vector3(2f, 0.28f, 12f), new Color(0.66f, 0.57f, 0.49f));

                var buildingColor = Color.HSVToRGB((index * 0.13f + (side > 0 ? 0.06f : 0f)) % 1f, 0.58f, 0.88f);
                CalafiaRushWorldDraw.DrawBuilding(segment,
                    new Vector3(side * 8.4f, 1.5f + index % 3, segment.position.z + (index % 2 == 0 ? 2f : -2f)),
                    new Vector3(2.1f, 3f + (index % 3) * 1.2f, 5f), buildingColor);
            }
        }

        private void Update()
        {
            if (_state != CalafiaRushGameState.Running) return;

            if (_input.EndRunPressed)
            {
                FinishRun();
                return;
            }

            if (_input.LaneLeft) ChangeLane(-1);
            if (_input.LaneRight) ChangeLane(1);

            var accelerating = _input.Accelerate;
            var braking = _input.Brake;
            var desiredSpeed = accelerating ? _maxSpeed : _cruiseSpeed;
            if (braking) desiredSpeed = 0f;
            if (Time.time < _blockedUntil) desiredSpeed = 0f;

            var targetSpeed = desiredSpeed;
            _autoBrakeActive = false;
            if (_autoBrakeEnabled && Time.time >= _blockedUntil &&
                TryGetNearestAutoBrakeGap(out var obstacleGap))
            {
                targetSpeed = GetAutoBrakeTargetSpeed(desiredSpeed, obstacleGap, out _autoBrakeActive);
            }

            var longitudinalResponse = braking ? _brakeResponse : accelerating ? _accelResponse : _cruiseResponse;
            if (_autoBrakeActive && _speed > targetSpeed)
                longitudinalResponse = _autoBrakeDecelRate;
            else if (_autoBrakeWasActive && !_autoBrakeActive && _speed < targetSpeed)
                longitudinalResponse = _autoBrakeResumeRate;

            _speed = Mathf.MoveTowards(_speed, targetSpeed, Time.deltaTime * longitudinalResponse);
            _autoBrakeWasActive = _autoBrakeActive;

            if (_input.BribePressed) TryBribe();

            UpdateBusHandling(braking || _autoBrakeActive);

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
                FinishRun();
            }
        }

        private void UpdateBusHandling(bool braking)
        {
            var targetX = LaneX[_lane];
            var position = _bus.transform.position;
            var speedRatio = _maxSpeed > 0f ? Mathf.Clamp01(_speed / _maxSpeed) : 0f;
            var laneError = targetX - position.x;

            var grip = Mathf.Lerp(_gripAtLowSpeed, _gripAtHighSpeed, speedRatio);
            if (braking && _speed > _brakeHandlingSpeedThreshold) grip *= _brakeGripMultiplier;
            var damping = Mathf.Lerp(_dampingAtLowSpeed, _dampingAtHighSpeed, speedRatio);
            if (braking && _speed > _brakeHandlingSpeedThreshold) damping *= _brakeDampingMultiplier;

            var lateralAcceleration = laneError * grip - _lateralVelocity * damping;
            _lateralVelocity += lateralAcceleration * Time.deltaTime;
            _lateralVelocity = Mathf.Clamp(_lateralVelocity, -_maxLateralVelocity, _maxLateralVelocity);
            position.x += _lateralVelocity * Time.deltaTime;

            const float roadEdge = 4.35f;
            if (Mathf.Abs(position.x) > roadEdge)
            {
                position.x = Mathf.Clamp(position.x, -roadEdge, roadEdge);
                _lateralVelocity *= -0.28f;
                _speed = Mathf.Max(4f, _speed - 3f);
                ShowMessage("CURB HIT! HOLD THE ROUTE");
            }

            _bus.transform.position = position;

            var sliding = speedRatio > 0.58f && Mathf.Abs(_lateralVelocity) > 2.25f;
            var driftTarget = sliding ? Mathf.InverseLerp(2.25f, 7f, Mathf.Abs(_lateralVelocity)) : 0f;
            _driftAmount = Mathf.MoveTowards(_driftAmount, driftTarget, Time.deltaTime * (sliding ? 3.5f : 2.2f));
            _brakeDive = Mathf.MoveTowards(_brakeDive, braking && _speed > 5f ? 1f : 0f, Time.deltaTime * 4f);

            var yaw = -_lateralVelocity * Mathf.Lerp(1.4f, 3.4f, _driftAmount);
            var roll = -_lateralVelocity * Mathf.Lerp(2.4f, 4.8f, speedRatio);
            var pitch = _brakeDive * 5.5f;
            _busBody.localRotation = Quaternion.Slerp(_busBody.localRotation,
                Quaternion.Euler(pitch, yaw, roll), Time.deltaTime * _bodyTiltSlerpSpeed);
        }

        private void FinishRun()
        {
            _speed = 0f;
            if (!_scoreBanked)
            {
                _garagePoints += _score;
                _scoreBanked = true;
                SaveGarage();
            }

            SetState(CalafiaRushGameState.GameOver);
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
            _patternsUntilNextLight--;
            _patternsUntilNextCheckpoint--;

            if (_patternsUntilNextLight <= 0)
            {
                SpawnLight();
                _patternsUntilNextLight = Random.Range(MinPatternsBetweenLights, MaxPatternsBetweenLights + 1);
                if (_patternsUntilNextCheckpoint <= 0)
                    _patternsUntilNextCheckpoint = 1;
                return;
            }

            if (_patternsUntilNextCheckpoint <= 0)
            {
                SpawnCop();
                _patternsUntilNextCheckpoint = Random.Range(MinPatternsBetweenCheckpoints,
                    MaxPatternsBetweenCheckpoints + 1);
                return;
            }

            var roll = Random.value;
            if (roll < 0.38f)
            {
                SpawnPassengerGroup();
            }
            else if (roll < 0.74f)
            {
                const float trafficSpawnZ = 42f;
                var firstLane = PickTrafficLane();
                SpawnTraffic(firstLane, trafficSpawnZ);
                if (Random.value < 0.45f)
                {
                    var secondLane = PickTrafficLane();
                    var secondSpawnZ = secondLane == firstLane
                        ? trafficSpawnZ + GetTrafficFollowGap()
                        : trafficSpawnZ;
                    SpawnTraffic(secondLane, secondSpawnZ);
                }
            }
            else
            {
                SpawnCoin(Random.Range(0, 3));
            }
        }

        private static int PickTrafficLane()
        {
            var roll = Random.value;
            if (roll < 0.1f) return LeftLaneIndex;
            if (roll < 0.32f) return CenterLaneIndex;
            return RightLaneIndex;
        }

        private void SpawnPassengerGroup()
        {
            const float sidewalkOffset = 2.1f;
            const float queueSpacing = 0.9f;
            var groupSize = Random.Range(1, 5);

            for (var i = 0; i < groupSize; i++)
            {
                var passenger = CalafiaRushWorldDraw.DrawPassenger(
                    new Vector3(LaneX[PassengerLaneIndex] + sidewalkOffset, 0.85f, 42f + i * queueSpacing),
                    new Vector3(0.55f, 0.85f, 0.55f),
                    Color.HSVToRGB(Random.value, 0.7f, 0.95f));
                if (groupSize > 1) passenger.name = "Waiting Passenger " + (i + 1);
                AddItem(passenger, RoadItemType.Passenger, PassengerLaneIndex);
            }
        }

        private void SpawnTraffic(int lane, float spawnZ = 42f)
        {
            var car = CalafiaRushWorldDraw.DrawCar(new Vector3(LaneX[lane], 0.55f, spawnZ),
                Color.HSVToRGB(Random.value, 0.65f, 0.9f));
            var item = AddItem(car, RoadItemType.Traffic, lane);
            item.trafficTargetSpeed = GetRandomTrafficSpeed(lane);
            item.trafficSpeed = item.trafficTargetSpeed;
        }

        private void SpawnLight()
        {
            var root = CalafiaRushWorldDraw.DrawSemaphore(new Vector3(0f, 0f, 44f),
                CalafiaRushWorldDraw.SemaphoreRedColor);
            AddItem(root, RoadItemType.Light, -1).phase = Random.Range(0f, LightCycleDuration);
        }

        private void SpawnCop()
        {
            var checkpoint = CalafiaRushWorldDraw.DrawCheckpoint(new Vector3(0f, 0f, 46f));
            var item = AddItem(checkpoint.root, RoadItemType.Cop, -1);
            item.barrierPivot = checkpoint.barrierPivot;
        }

        private void SpawnCoin(int lane)
        {
            var coin = CalafiaRushWorldDraw.DrawCoin(new Vector3(LaneX[lane], 1f, 42f),
                CalafiaRushWorldDraw.CoinColor);
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
            UpdateTrafficMovement(movement);

            for (var i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (!item.gameObject)
                {
                    _items.RemoveAt(i);
                    continue;
                }

                if (item.type != RoadItemType.Traffic)
                    item.gameObject.transform.position += Vector3.back * movement;

                var z = item.gameObject.transform.position.z;
                if (item.type == RoadItemType.Coin)
                    item.gameObject.transform.Rotate(0f, Time.deltaTime * 180f, 0f, Space.World);

                if (item.type == RoadItemType.Light)
                {
                    UpdateTrafficLightVisual(item);
                    if (!item.resolved && z < -1f)
                    {
                        item.resolved = true;
                        var phase = GetTrafficLightPhase(item);
                        if (phase == TrafficLightPhase.Red && _speed > 5f)
                        {
                            _timeLeft = Mathf.Max(0f, _timeLeft - 6f);
                            _speed = 3f;
                            ShowMessage("RED LIGHT!  -6 SECONDS");
                        }
                        else if (phase == TrafficLightPhase.Yellow && _speed > 8f)
                        {
                            ShowMessage("YELLOW LIGHT!  SLOW DOWN");
                        }
                        else if (phase == TrafficLightPhase.Green)
                        {
                            _timeLeft += 10f;
                            _score += 75;
                            ShowMessage("GREEN LIGHT!  +10 SECONDS");
                        }
                    }
                }
                else if (item.type == RoadItemType.Cop)
                {
                    UpdateCheckpointBarrier(item);

                    if (!item.checkpointCleared && z < 14f && z > -6f)
                    {
                        _pendingCheckpoint = item;
                        if (!item.resolved && z < 10f)
                        {
                            item.resolved = true;
                            ShowMessage("CHECKPOINT! PRESS B TO PAY $10");
                        }
                    }
                    else if (_pendingCheckpoint == item && (item.checkpointCleared || z < -6f))
                    {
                        _pendingCheckpoint = null;
                    }

                    if (!item.checkpointCleared && item.barrierAngle > -45f &&
                        Mathf.Abs(z + 3.5f) < 1.6f)
                    {
                        _speed = 0f;
                        ShowMessage("CHECKPOINT BLOCKED! PAY $10");
                    }
                }
                else if (!item.resolved && item.lane == CurrentBusLane() && ShouldResolveLaneItem(item, z))
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

        private float GetTrafficFollowGap()
        {
            return Mathf.Max(_movingCarFollowGap, CalafiaRushWorldDraw.TrafficCarLength);
        }

        private void UpdateTrafficMovement(float worldMovement)
        {
            if (Time.deltaTime <= 0f) return;

            if (!_movingCarsEnabled)
            {
                foreach (var item in _items)
                {
                    if (item.type != RoadItemType.Traffic || !item.gameObject) continue;
                    item.gameObject.transform.position += Vector3.back * worldMovement;
                    item.trafficSpeed = 0f;
                }

                return;
            }

            for (var lane = 0; lane < LaneX.Length; lane++)
            {
                var laneTraffic = new List<RoadItem>();
                foreach (var item in _items)
                {
                    if (item.type != RoadItemType.Traffic || !item.gameObject || item.lane != lane) continue;
                    laneTraffic.Add(item);
                }

                laneTraffic.Sort((left, right) =>
                    left.gameObject.transform.position.z.CompareTo(right.gameObject.transform.position.z));

                RoadItem follower = null;
                foreach (var item in laneTraffic)
                {
                    var currentPosition = item.gameObject.transform.position;
                    var targetTrafficSpeed = Mathf.Min(item.trafficTargetSpeed, _movingCarMaxSpeed);
                    var trafficResponse = item.trafficSpeed > targetTrafficSpeed ? _movingCarBrakeRate : _movingCarAccelRate;
                    item.trafficSpeed = Mathf.MoveTowards(item.trafficSpeed, targetTrafficSpeed,
                        trafficResponse * Time.deltaTime);

                    var relativeMovement = worldMovement - item.trafficSpeed * Time.deltaTime;
                    var proposedZ = currentPosition.z - relativeMovement;

                    if (follower != null && follower.gameObject)
                    {
                        var followerLimitZ = follower.gameObject.transform.position.z + GetTrafficFollowGap();
                        if (proposedZ < followerLimitZ) proposedZ = followerLimitZ;
                    }

                    if (!Mathf.Approximately(proposedZ, currentPosition.z))
                    {
                        var actualRelativeMovement = currentPosition.z - proposedZ;
                        item.trafficSpeed = Mathf.Clamp(worldMovement / Time.deltaTime - actualRelativeMovement / Time.deltaTime,
                            0f, _movingCarMaxSpeed);
                    }

                    currentPosition.z = proposedZ;
                    item.gameObject.transform.position = currentPosition;
                    follower = item;
                }
            }
        }

        private TrafficLightPhase GetTrafficLightPhase(RoadItem item)
        {
            var cycleTime = (Time.time + item.phase) % LightCycleDuration;
            if (cycleTime < LightGreenDuration) return TrafficLightPhase.Green;
            if (cycleTime < LightGreenDuration + LightYellowDuration) return TrafficLightPhase.Yellow;
            return TrafficLightPhase.Red;
        }

        private void UpdateTrafficLightVisual(RoadItem item)
        {
            var signalColor = GetTrafficLightPhase(item) switch
            {
                TrafficLightPhase.Red => CalafiaRushWorldDraw.SemaphoreRedColor,
                TrafficLightPhase.Yellow => CalafiaRushWorldDraw.SemaphoreYellowColor,
                _ => CalafiaRushWorldDraw.SemaphoreGreenColor
            };
            CalafiaRushWorldDraw.SetSemaphoreSignalColor(item.gameObject, signalColor);
        }

        private void UpdateCheckpointBarrier(RoadItem item)
        {
            if (!item.barrierPivot) return;

            // Arm extends left (-X) from cabin hinge; negative Z rotation swings it upward.
            var targetAngle = item.checkpointCleared ? -82f : 0f;
            item.barrierAngle = Mathf.MoveTowards(item.barrierAngle, targetAngle,
                Time.deltaTime * _checkpointBarrierLiftSpeed);
            item.barrierPivot.localRotation = Quaternion.Euler(0f, 0f, item.barrierAngle);
        }

        private void ClearCheckpoint(RoadItem item)
        {
            item.checkpointCleared = true;
            if (_pendingCheckpoint == item) _pendingCheckpoint = null;
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

        private void ChangeLane(int direction)
        {
            var previousLane = _lane;
            _lane = Mathf.Clamp(_lane + direction, 0, 2);
            if (_lane != previousLane)
            {
                var speedRatio = _maxSpeed > 0f ? Mathf.Clamp01(_speed / _maxSpeed) : 0f;
                _lateralVelocity += direction * Mathf.Lerp(_laneImpulseAtLowSpeed, _laneImpulseAtHighSpeed, speedRatio);
            }

            _input.ClearLaneHold();
        }

        private int CurrentBusLane()
        {
            var nearestLane = 0;
            var nearestDistance = Mathf.Abs(_bus.transform.position.x - LaneX[0]);
            for (var i = 1; i < LaneX.Length; i++)
            {
                var distance = Mathf.Abs(_bus.transform.position.x - LaneX[i]);
                if (distance >= nearestDistance) continue;
                nearestLane = i;
                nearestDistance = distance;
            }

            return nearestLane;
        }

        private bool TryGetNearestAutoBrakeGap(out float gap)
        {
            var found = false;
            gap = float.MaxValue;

            if (TryGetNearestTrafficGapAhead(out var trafficGap))
            {
                gap = trafficGap;
                found = true;
            }

            if (TryGetNearestCheckpointGapAhead(out var checkpointGap) &&
                (!found || checkpointGap < gap))
            {
                gap = checkpointGap;
                found = true;
            }

            return found;
        }

        private bool TryGetNearestTrafficGapAhead(out float gap)
        {
            gap = float.MaxValue;
            var found = false;
            var busZ = _bus.transform.position.z;
            var busFrontZ = busZ + BusFrontZOffset;
            var busLane = CurrentBusLane();

            foreach (var item in _items)
            {
                if (item.type != RoadItemType.Traffic || !item.gameObject) continue;
                if (item.lane != busLane) continue;

                var trafficZ = item.gameObject.transform.position.z;
                var trafficRearZ = trafficZ - TrafficRearZOffset;
                if (trafficRearZ <= busFrontZ) continue;

                var itemGap = trafficRearZ - busFrontZ;
                if (itemGap >= gap) continue;
                gap = itemGap;
                found = true;
            }

            return found;
        }

        private float GetRandomTrafficSpeed(int lane)
        {
            var maxSpeed = _movingCarMaxSpeed;
            var minSpeed = Mathf.Min(_movingCarMinSpeed, maxSpeed);
            if (maxSpeed <= minSpeed) return minSpeed;

            var laneSpeedScale = GetTrafficLaneSpeedScale(lane);
            var laneMinSpeed = minSpeed * laneSpeedScale;
            var laneMaxSpeed = maxSpeed * laneSpeedScale;
            return Random.Range(laneMinSpeed, Mathf.Max(laneMinSpeed, laneMaxSpeed));
        }

        private static float GetTrafficLaneSpeedScale(int lane)
        {
            return lane switch
            {
                LeftLaneIndex => 1f,
                CenterLaneIndex => 0.45f,
                RightLaneIndex => 0.12f,
                _ => 0.45f
            };
        }

        private bool TryGetNearestCheckpointGapAhead(out float gap)
        {
            gap = float.MaxValue;
            var found = false;
            var busFrontZ = _bus.transform.position.z + BusFrontZOffset;

            foreach (var item in _items)
            {
                if (item.type != RoadItemType.Cop || !item.gameObject || item.checkpointCleared) continue;
                if (item.barrierAngle < -45f) continue;

                var barrierZ = item.gameObject.transform.position.z;
                if (barrierZ <= busFrontZ) continue;

                var itemGap = barrierZ - busFrontZ - 0.35f;
                if (itemGap >= gap) continue;
                gap = itemGap;
                found = true;
            }

            return found;
        }

        private float GetAutoBrakeTargetSpeed(float desiredSpeed, float gap, out bool active)
        {
            if (gap >= _autoBrakeDetectionRange)
            {
                active = false;
                return desiredSpeed;
            }

            active = true;
            if (gap <= _autoBrakeStopGap) return 0f;

            var normalizedGap = Mathf.InverseLerp(_autoBrakeStopGap, _autoBrakeDetectionRange, gap);
            normalizedGap = Mathf.Pow(normalizedGap, _autoBrakeUrgencyExponent);

            var springTarget = desiredSpeed * normalizedGap;
            var gapError = gap - _autoBrakeStopGap;
            var springSpeed = Mathf.Max(0f, desiredSpeed + gapError * _autoBrakeSpringStiffness -
                                            _speed * _autoBrakeSpringDamping);
            return Mathf.Min(desiredSpeed, springTarget, springSpeed);
        }

        private bool ShouldResolveLaneItem(RoadItem item, float z)
        {
            if (Mathf.Abs(z + 3.5f) >= 1.8f) return false;
            if (item.type == RoadItemType.Traffic && _autoBrakeEnabled) return false;
            return true;
        }

        private void LoadGarage()
        {
            _garagePoints = PlayerPrefs.GetInt("CalafiaRush.GaragePoints", 0);
            _ownedSkins = PlayerPrefs.GetInt("CalafiaRush.OwnedSkins", 1) | 1;
            _selectedSkin = Mathf.Clamp(PlayerPrefs.GetInt("CalafiaRush.SelectedSkin", 0), 0, BusSkins.Length - 1);
            _autoBrakeEnabled = PlayerPrefs.GetInt("CalafiaRush.AutoBrakeEnabled", _autoBrakeEnabled ? 1 : 0) != 0;
            if (!OwnsSkin(_selectedSkin)) _selectedSkin = 0;
        }

        private void SaveGarage()
        {
            PlayerPrefs.SetInt("CalafiaRush.GaragePoints", _garagePoints);
            PlayerPrefs.SetInt("CalafiaRush.OwnedSkins", _ownedSkins);
            PlayerPrefs.SetInt("CalafiaRush.SelectedSkin", _selectedSkin);
            PlayerPrefs.SetInt("CalafiaRush.AutoBrakeEnabled", _autoBrakeEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplySelectedSkin()
        {
            if (_busBodyRenderer == null || _busStripeRenderer == null) return;
            var skin = BusSkins[_selectedSkin];
            CalafiaRushWorldDraw.ApplyBusColors(
                new CalafiaRushWorldDraw.BusVisualRefs(_bus, _busBody, _busBodyRenderer, _busStripeRenderer),
                skin.bodyColor, skin.stripeColor);
        }

        private void ShowMessage(string message)
        {
            _message = message;
            _messageUntil = Time.time + 2.4f;
        }

    }
}
