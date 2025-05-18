using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Runtime.DialogueSystem.Runtime.Core
{
    /// <summary>
    /// Singleton que inicializa o sistema de localização da Unity,
    /// troca de idioma em runtime e expõe métodos de busca de conteúdo.
    /// </summary>

    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }
        public static event System.Action OnLanguageChanged;
        [SerializeField] private Locale _defaultLocale;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLocalization();
        }
    
        /// <summary>
        /// Registra callback e aplica idioma padrão.
        /// </summary>
        private void InitializeLocalization()
        {
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
            _ = SetLanguageAsync(_defaultLocale.Identifier.Code);
        }
    
        public void ChangeLanguage(string language) => _ = SetLanguageAsync(language);
    
        /// <summary>
        /// Troca para o idioma dado pelo código (ex: "pt", "en").
        /// </summary>
        /// <param name="languageCode">ISO code do idioma.</param>
        public async Task SetLanguageAsync(string languageCode)
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale(languageCode);
            if (locale == null) return;
            await LocalizationSettings.InitializationOperation.Task;
            LocalizationSettings.SelectedLocale = locale;
        }
    
        private void HandleLocaleChanged(Locale newLocale)
        {
            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// Busca string localizada síncrona.
        /// </summary>
        public string GetLocalizedString(LocalizedString reference)
            => reference.GetLocalizedString();
    
        /// <summary>
        /// Busca string localizada de forma assíncrona.
        /// </summary>
        public async Task<string> GetLocalizedStringAsync(LocalizedString reference)
        {
            var op = reference.GetLocalizedStringAsync();
            await op.Task;
            return op.Result;
        }
    

        /// <summary>
        /// Obtem pluralização para quantidade dada.
        /// </summary>
        public string GetPlural(LocalizedString reference, int quantity)
            => reference.GetLocalizedString(quantity);
    }
}
