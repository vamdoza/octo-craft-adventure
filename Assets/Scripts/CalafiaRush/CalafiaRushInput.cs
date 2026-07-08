using UnityEngine;

namespace CalafiaRush
{
    [DefaultExecutionOrder(-100)]
    public sealed class CalafiaRushInput : MonoBehaviour
    {
        private bool _leftHeld;
        private bool _rightHeld;
        private bool _accelerateHeld;
        private bool _laneLeftPulse;
        private bool _laneRightPulse;

        public bool LaneLeft { get; private set; }
        public bool LaneRight { get; private set; }
        public bool Accelerate { get; private set; }
        public bool Brake { get; private set; }
        public bool BribePressed { get; private set; }
        public bool StartPressed { get; private set; }
        public bool EndRunPressed { get; private set; }

        private void Update()
        {
            LaneLeft = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || _laneLeftPulse;
            LaneRight = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || _laneRightPulse;
            Accelerate = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) || _accelerateHeld;
            Brake = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            BribePressed = Input.GetKeyDown(KeyCode.B);
            StartPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
            EndRunPressed = Input.GetKeyDown(KeyCode.Q);

            _laneLeftPulse = false;
            _laneRightPulse = false;
        }

        public void PulseLaneLeft() => _laneLeftPulse = true;
        public void PulseLaneRight() => _laneRightPulse = true;

        public void SetLeftHeld(bool held) => _leftHeld = held;
        public void SetRightHeld(bool held) => _rightHeld = held;
        public void SetAccelerateHeld(bool held) => _accelerateHeld = held;

        public void ClearLaneHold()
        {
            _leftHeld = false;
            _rightHeld = false;
        }
    }
}
