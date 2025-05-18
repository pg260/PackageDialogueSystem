using TMPro;
using UnityEngine;

namespace Runtime.DialogueSystem.Runtime.Core
{
    public class FontManager : MonoBehaviour
    {
        public static FontManager Instance { get; private set; }

        [SerializeField] private TMP_FontAsset _defaultFont;
        [SerializeField] private TMP_FontAsset _accessibleFont;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetFont(bool useAccessibleFont)
        {
            TMP_FontAsset newFont = useAccessibleFont ? _accessibleFont : _defaultFont;
            UpdateAllTextComponents(newFont);
        }

        private void UpdateAllTextComponents(TMP_FontAsset font)
        {
            TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in allTexts)
            {
                text.font = font;
            }
        }
    }
}