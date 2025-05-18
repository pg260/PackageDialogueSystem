using UnityEngine;
using UnityEngine.Events;

namespace Runtime.DialogueSystem.Runtime.Core
{
    public class AccessibilityManager : MonoBehaviour
    {
        public static AccessibilityManager Instance { get; private set; }

        // Eventos
        public UnityEvent<bool> OnFontChanged; 
        public UnityEvent OnAccessibilitySettingsChanged;
    
        public bool UseAccessibleFont;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadSettings()
        {
            ApplySettings();
        }

        private void ApplySettings()
        {
            FontManager.Instance.SetFont(UseAccessibleFont);
            OnFontChanged.Invoke(UseAccessibleFont);
            OnAccessibilitySettingsChanged.Invoke();
        }

        public void ToggleFont()
        {
            UseAccessibleFont = !UseAccessibleFont;
            ApplySettings();
        }
    }
}
