using UnityEngine;

namespace CalafiaRush
{
    public static class CalafiaRushWorldDraw
    {
        public const float TrafficCarLength = 3.4f;

        public static readonly Color SemaphoreRedColor = new Color(1f, 0.08f, 0.04f);
        public static readonly Color SemaphoreYellowColor = new Color(1f, 0.82f, 0.08f);
        public static readonly Color SemaphoreGreenColor = new Color(0.08f, 0.95f, 0.24f);

        public static readonly Color CoinColor = new Color(1f, 0.78f, 0.12f);
        public static readonly Color WheelColor = new Color(0.04f, 0.04f, 0.04f);
        public static readonly Color WindshieldColor = new Color(0.08f, 0.19f, 0.25f);
        public static readonly Color WindowColor = new Color(0.1f, 0.25f, 0.31f);

        public readonly struct BusVisualRefs
        {
            public readonly GameObject root;
            public readonly Transform body;
            public readonly Renderer bodyRenderer;
            public readonly Renderer stripeRenderer;

            public BusVisualRefs(GameObject root, Transform body, Renderer bodyRenderer, Renderer stripeRenderer)
            {
                this.root = root;
                this.body = body;
                this.bodyRenderer = bodyRenderer;
                this.stripeRenderer = stripeRenderer;
            }
        }

        public readonly struct CheckpointVisualRefs
        {
            public readonly GameObject root;
            public readonly Transform barrierPivot;

            public CheckpointVisualRefs(GameObject root, Transform barrierPivot)
            {
                this.root = root;
                this.barrierPivot = barrierPivot;
            }
        }

        public static GameObject DrawRoadSegment(Vector3 position, Vector3 scale, Color color)
        {
            return CreateCube("Road Segment", position, scale, color);
        }

        public static void DrawLaneMarker(Transform parent, Vector3 worldPosition, Vector3 scale, Color color)
        {
            var marker = CreateCube("Lane Marker", worldPosition, scale, color);
            marker.transform.SetParent(parent, true);
        }

        public static void DrawSidewalk(Transform parent, Vector3 worldPosition, Vector3 scale, Color color)
        {
            var sidewalk = CreateCube("Sidewalk", worldPosition, scale, color);
            sidewalk.transform.SetParent(parent, true);
        }

        public static void DrawBuilding(Transform parent, Vector3 worldPosition, Vector3 scale, Color color)
        {
            var building = CreateCube("Colorful Building", worldPosition, scale, color);
            building.transform.SetParent(parent, true);
        }

        public static BusVisualRefs DrawBus(Color bodyColor, Color stripeColor)
        {
            var root = new GameObject("Calafia");
            var bodyTransform = new GameObject("Bus Visual").transform;
            bodyTransform.SetParent(root.transform, false);

            var body = CreateChildCube(bodyTransform, "Bus Body", new Vector3(0f, 0.55f, 0f),
                new Vector3(2.2f, 1.2f, 4.2f), bodyColor);
            var stripe = CreateChildCube(bodyTransform, "Color Stripe", new Vector3(0f, 0.55f, 0f),
                new Vector3(2.28f, 0.28f, 4.25f), stripeColor);

            CreateChildCube(bodyTransform, "Windshield", new Vector3(0f, 0.85f, -2.13f),
                new Vector3(1.75f, 0.55f, 0.08f), WindshieldColor);

            for (var z = -1.25f; z <= 1.25f; z += 0.85f)
            {
                foreach (var side in new[] { -1f, 1f })
                {
                    CreateChildCube(bodyTransform, "Window", new Vector3(side * 1.12f, 0.85f, z),
                        new Vector3(0.06f, 0.48f, 0.62f), WindowColor);
                }
            }

            foreach (var x in new[] { -0.82f, 0.82f })
            {
                foreach (var z in new[] { -1.35f, 1.35f })
                    DrawBusWheel(bodyTransform, new Vector3(x, 0.1f, z));
            }

            return new BusVisualRefs(root, bodyTransform, body.GetComponent<Renderer>(),
                stripe.GetComponent<Renderer>());
        }

        public static void ApplyBusColors(BusVisualRefs bus, Color bodyColor, Color stripeColor)
        {
            if (bus.bodyRenderer) bus.bodyRenderer.material.color = bodyColor;
            if (bus.stripeRenderer) bus.stripeRenderer.material.color = stripeColor;
        }

        public static GameObject DrawCar(Vector3 position, Color bodyColor)
        {
            return CreateCube("Traffic", position, new Vector3(1.8f, 1f, TrafficCarLength), bodyColor);
        }

        public static GameObject DrawPassenger(Vector3 position, Vector3 scale, Color color)
        {
            var passenger = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            passenger.name = "Waiting Passenger";
            passenger.transform.position = position;
            passenger.transform.localScale = scale;
            passenger.GetComponent<Renderer>().material.color = color;
            return passenger;
        }

        public static GameObject DrawSemaphore(Vector3 position, Color initialSignalColor)
        {
            var root = new GameObject("Traffic Light");
            root.transform.position = position;

            foreach (var side in new[] { -1f, 1f })
            {
                CreateChildCube(root.transform, side < 0f ? "Pole Left" : "Pole Right",
                    new Vector3(side * 5.2f, 2f, 0f), new Vector3(0.18f, 4f, 0.18f),
                    new Color(0.2f, 0.2f, 0.2f));
            }

            const float spanLength = 10.4f;
            CreateChildCube(root.transform, "Span Arm", new Vector3(0f, 3.8f, 0f),
                new Vector3(spanLength, 0.16f, 0.16f), new Color(0.2f, 0.2f, 0.2f));
            CreateChildCube(root.transform, "Signal", new Vector3(0f, 3.8f, -0.14f),
                new Vector3(3.8f, 0.42f, 0.1f), initialSignalColor);

            return root;
        }

        public static void SetSemaphoreSignalColor(GameObject semaphore, Color signalColor)
        {
            var signal = semaphore.transform.Find("Signal");
            if (!signal) return;

            var renderer = signal.GetComponent<Renderer>();
            if (renderer) renderer.material.color = signalColor;
        }

        public static CheckpointVisualRefs DrawCheckpoint(Vector3 position)
        {
            var root = new GameObject("Police Checkpoint");
            root.transform.position = position;

            const float sidewalkX = 6.5f;
            var tollBooth = new GameObject("Toll Booth").transform;
            tollBooth.SetParent(root.transform, false);
            tollBooth.localPosition = new Vector3(sidewalkX, 0f, 0f);

            CreateChildCube(tollBooth, "Toll Base", new Vector3(0f, 0.14f, 0f),
                new Vector3(1.5f, 0.28f, 1.25f), new Color(0.58f, 0.5f, 0.44f));
            CreateChildCube(tollBooth, "Toll Cabin", new Vector3(0f, 0.95f, 0f),
                new Vector3(1.35f, 1.75f, 1.1f), new Color(0.82f, 0.84f, 0.88f));
            CreateChildCube(tollBooth, "Toll Roof", new Vector3(0f, 1.92f, 0f),
                new Vector3(1.55f, 0.18f, 1.35f), new Color(0.14f, 0.2f, 0.36f));
            CreateChildCube(tollBooth, "Toll Window", new Vector3(-0.7f, 1.05f, 0f),
                new Vector3(0.08f, 0.55f, 0.7f), new Color(0.12f, 0.28f, 0.34f));
            CreateChildCube(tollBooth, "Toll Sign", new Vector3(-0.72f, 1.5f, 0.62f),
                new Vector3(0.55f, 0.35f, 0.06f), new Color(0.95f, 0.86f, 0.24f));

            var barrierPivot = new GameObject("Barrier Pivot").transform;
            barrierPivot.SetParent(tollBooth, false);
            barrierPivot.localPosition = new Vector3(-0.72f, 0.6f, 0f);

            const float roadLeftEdge = -5.5f;
            var pivotWorldX = sidewalkX + barrierPivot.localPosition.x;
            var armLength = pivotWorldX - roadLeftEdge;
            CreateChildCube(barrierPivot, "Checkpoint Barrier", new Vector3(-armLength * 0.5f, 0f, 0f),
                new Vector3(armLength, 0.22f, 0.35f), new Color(0.95f, 0.86f, 0.24f));
            CreateChildCube(barrierPivot, "Barrier Hinge", Vector3.zero,
                new Vector3(0.28f, 0.35f, 0.4f), new Color(0.2f, 0.2f, 0.22f));

            return new CheckpointVisualRefs(root, barrierPivot);
        }

        public static GameObject DrawCoin(Vector3 position, Color color)
        {
            var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Fare Bonus";
            coin.transform.position = position;
            coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            coin.transform.localScale = new Vector3(0.65f, 0.12f, 0.65f);
            coin.GetComponent<Renderer>().material.color = color;
            return coin;
        }

        private static void DrawBusWheel(Transform parent, Vector3 localPosition)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Wheel";
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = localPosition;
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(0.45f, 0.18f, 0.45f);
            wheel.GetComponent<Renderer>().material.color = WheelColor;
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

        private static GameObject CreateChildCube(Transform parent, string name, Vector3 localPosition,
            Vector3 localScale, Color color)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;
            gameObject.GetComponent<Renderer>().material.color = color;
            return gameObject;
        }
    }
}
