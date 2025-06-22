using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Runtime.DialogueSystem.Runtime.Core;
using Runtime.DialogueSystem.Runtime.Data.Nodes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.DialogueSystem.Runtime.UI
{
    /// <summary>
    /// Controla a interface de diálogo: texto, escolhas e ícones.
    /// </summary>
    public class DialogueUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _speakerIcon;
        [SerializeField] private Image _listenerIcon;
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private Button _choiceButtonPrefab;

        [Header("Animation Settings")]
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private CanvasGroup _mainCanvasGroup;

        private List<Button> _currentChoices = new();
        private LayoutGroup _choicesLayout;
    
        private void Awake()
        {
            _choicesLayout = _choicesContainer.GetComponent<LayoutGroup>();
            LocalizationManager.OnLanguageChanged += RefreshChoiceButtonsAsync;
        }

        public async Task SetVisibilityAsync(bool isVisible)
        {
            if (isVisible)
            {
                gameObject.SetActive(true);
                await FadeCanvasAsync(0, 1);
            }
            else
            {
                await FadeCanvasAsync(1, 0);
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Define os sprites dos ícones de speaker e listener e aplica animação de entrada.
        /// </summary>
        public void UpdateSpeakerIcons(Sprite speaker, Sprite listener)
        {
            _speakerIcon.sprite = speaker;
            _speakerIcon.gameObject.SetActive(speaker != null);

            _listenerIcon.sprite = listener;
            _listenerIcon.gameObject.SetActive(listener != null);

            // Anima os ícones
            StartCoroutine(AnimateIcon(_speakerIcon));
            StartCoroutine(AnimateIcon(_listenerIcon));
        }

        /// <summary>
        /// Define o texto base do diálogo e reseta o contador de caracteres visíveis.
        /// </summary>
        public void SetText(string text)
        {
            _dialogueText.text = text;
            _dialogueText.maxVisibleCharacters = text.Length;
        }

        /// <summary>
        /// Adiciona o próximo caractere ao texto visível, imitando digitação.
        /// </summary>
        public void AppendText(string character)
        {
            _dialogueText.maxVisibleCharacters++;
            _dialogueText.text += character;
        }

        /// <summary>
        /// Remove todos os botões de escolha atuais.
        /// </summary>
        public void ClearChoices()
        {
            foreach (var button in _currentChoices)
            {
                button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }
            _currentChoices.Clear();
        }
    
        /// <summary>
        /// Instancia e configura um botão de escolha com texto localizado.
        /// </summary>
        public async Task CreateChoiceButtonAsync(DialogueChoice choice)
        {
            var button = Instantiate(_choiceButtonPrefab, _choicesContainer);
            _currentChoices.Add(button);

            var textComponent = button.GetComponentInChildren<TMP_Text>();
            textComponent.text = await LocalizationManager.Instance.GetLocalizedStringAsync(choice.ChoiceLocalizationKey);

            button.onClick.AddListener(() =>
            {
                DialogueManager.Instance.SelectChoice(choice);
                ClearChoices();
            });

            // Força atualização do layout para reposicionar botões
            Canvas.ForceUpdateCanvases();
            _choicesLayout.enabled = false;
            _choicesLayout.enabled = true;
        }
        
        /// <summary>
        /// Limpa as escolhas antigas e cria novos botões para as escolhas fornecidas.
        /// </summary>
        /// <param name="choices">A lista de escolhas a serem exibidas.</param>
        public async Task DisplayChoicesAsync(List<DialogueChoice> choices)
        {
            ClearChoices();

            if (choices == null || choices.Count == 0) return;

            var buttonCreationTasks = new List<Task>();
            foreach (var choice in choices)
            {
                // Adiciona a tarefa de criação de cada botão a uma lista
                buttonCreationTasks.Add(CreateChoiceButtonAsync(choice));
            }
    
            // Espera que todos os botões sejam criados e seus textos localizados de forma concorrente
            await Task.WhenAll(buttonCreationTasks);
        }

        /// <summary>
        /// Atualiza textos dos botões de escolha quando o idioma muda.
        /// </summary>
        private async void RefreshChoiceButtonsAsync()
        {
            foreach (var button in _currentChoices)
            {
                if (button.TryGetComponent<ChoiceData>(out var data))
                {
                    var textComponent = button.GetComponentInChildren<TMP_Text>();
                    textComponent.text = await LocalizationManager.Instance.GetLocalizedStringAsync(data.Choice.ChoiceLocalizationKey);
                }
            }
        }

        /// <summary>
        /// Anima fade do CanvasGroup entre dois valores de alpha.
        /// </summary>
        private IEnumerator AnimateUI(CanvasGroup group, float start, float end)
        {
            var elapsed = 0f;
            group.alpha = start;
            while (elapsed < _fadeDuration)
            {
                group.alpha = Mathf.Lerp(start, end, elapsed / _fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            group.alpha = end;
            group.interactable = group.alpha > 0;
            group.blocksRaycasts = group.interactable;
        }
    
        /// <summary>
        /// Wrapper que retorna uma Task para animações baseadas em coroutine.
        /// </summary>
        private Task FadeCanvasAsync(float start, float end)
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(RunCoroutineWrapper(AnimateUI(_mainCanvasGroup, start, end), tcs));
            return tcs.Task;
        }
    
        private IEnumerator RunCoroutineWrapper(IEnumerator coroutine, TaskCompletionSource<bool> tcs)
        {
            yield return StartCoroutine(coroutine);
            tcs.SetResult(true);
        }

        // Atualização no método SetVisibility
        public async void SetVisibility(bool isVisible)
        {
            await SetVisibilityAsync(isVisible);
        }

        /// <summary>
        /// Anima crescimento do ícone de zero ao tamanho original.
        /// </summary>
        private IEnumerator AnimateIcon(Image icon)
        {
            icon.color = Color.white;
        
            var original = icon.transform.localScale;
            icon.transform.localScale = Vector3.zero;
            float elapsed = 0f;
            const float duration = 0.2f;
            while (elapsed < duration)
            {
                icon.transform.localScale = Vector3.Lerp(Vector3.zero, original, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            icon.transform.localScale = original;
        }

        private void OnDestroy()
        {
            LocalizationManager.OnLanguageChanged -= RefreshChoiceButtonsAsync;
        }
    }

// Classe auxiliar para manter referência às escolhas
    public class ChoiceData : MonoBehaviour
    {
        public DialogueChoice Choice;
    }
}