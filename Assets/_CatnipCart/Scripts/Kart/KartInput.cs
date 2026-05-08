using UnityEngine;
using UnityEngine.InputSystem;

namespace CatnipCart.Kart
{
    /// <summary>
    /// Input interface that both player input and AI input implement.
    /// This decouples the kart controller from the input source.
    /// </summary>
    public interface IKartInput
    {
        /// <summary>Acceleration input: 0 to 1</summary>
        float Accelerate { get; }

        /// <summary>Brake/reverse input: 0 to 1</summary>
        float Brake { get; }

        /// <summary>Steering input: -1 (left) to 1 (right)</summary>
        float Steer { get; }

        /// <summary>Whether the drift button is held</summary>
        bool Drift { get; }

        /// <summary>Whether the use item button was pressed this frame</summary>
        bool UseItem { get; }

        /// <summary>Whether looking back</summary>
        bool LookBack { get; }
    }

    /// <summary>
    /// Player input reader using Unity's New Input System.
    /// Supports WASD and Arrow Keys.
    /// </summary>
    public class KartInput : MonoBehaviour, IKartInput
    {
        public float Accelerate
        {
            get
            {
                var kb = Keyboard.current;
                if (kb == null) return 0f;
                return (kb.wKey.isPressed || kb.upArrowKey.isPressed) ? 1f : 0f;
            }
        }

        public float Brake
        {
            get
            {
                var kb = Keyboard.current;
                if (kb == null) return 0f;
                return (kb.sKey.isPressed || kb.downArrowKey.isPressed) ? 1f : 0f;
            }
        }

        public float Steer
        {
            get
            {
                var kb = Keyboard.current;
                if (kb == null) return 0f;
                
                float keyboard = 0f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) keyboard -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) keyboard += 1f;

                return keyboard;
            }
        }

        public bool Drift
        {
            get
            {
                var kb = Keyboard.current;
                return kb != null && (kb.spaceKey.isPressed || kb.leftShiftKey.isPressed);
            }
        }

        public bool UseItem
        {
            get
            {
                var kb = Keyboard.current;
                return kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
            }
        }

        public bool LookBack
        {
            get
            {
                var kb = Keyboard.current;
                return kb != null && (kb.qKey.isPressed || kb.tabKey.isPressed);
            }
        }
    }
}
