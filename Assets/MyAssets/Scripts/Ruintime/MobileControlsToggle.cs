using TMPro;
using UnityEngine;

namespace PlatformCoinLab.Runtime
{
    public sealed class MobileControlsToggle : MonoBehaviour
    {
        public enum Layout
        {
            Joystick = 0,
            Keypad = 1,
        }

        private const string LayoutKey = "PlatformCoinLab.TouchLayout";

        [SerializeField] private GameObject joystick;
        [SerializeField] private GameObject[] keypadButtons;
        [SerializeField] private TMP_Text label;

        private Layout current;

        private void Start()
        {
            // 저장값이 없거나 알 수 없는 값이면 조이스틱으로 시작
            int saved = PlayerPrefs.GetInt(LayoutKey, (int)Layout.Joystick);
            Apply(saved == (int)Layout.Keypad ? Layout.Keypad : Layout.Joystick);
        }

        public void Toggle()
        {
            Apply(current == Layout.Joystick ? Layout.Keypad : Layout.Joystick);

            PlayerPrefs.SetInt(LayoutKey, (int)current);
            PlayerPrefs.Save();
        }

        private void Apply(Layout layout)
        {
            current = layout;
            bool useKeypad = layout == Layout.Keypad;

            joystick.SetActive(!useKeypad);
            foreach (GameObject button in keypadButtons)
            {
                button.SetActive(useKeypad);
            }

            if (label != null)
            {
                label.text = useKeypad ? "Stick" : "Keypad";
            }
        }
    }
}
