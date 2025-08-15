using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Runtime.DialogueSystem.Runtime.Core;
using Runtime.DialogueSystem.Runtime.Data.Enums;
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

        [Header("DialoguePanel")]
        [SerializeField] private TextMeshProUGUI SpeakerName;
        [SerializeField] private TextMeshProUGUI _dialogueText;
        
        [Header("ChoicePanel")] 
        [SerializeField] private CanvasGroup choiceCanvasGroup;
        [SerializeField] private TextMeshProUGUI choiceText;
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private Button _choiceButtonPrefab;

        [Header("Animation Settings")]
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private CanvasGroup _mainCanvasGroup;

        private CanvasGroup _currentCanvasGroup;
        private TextMeshProUGUI currentDialogueText;
        
        private List<Button> _currentChoices = new();
        private LayoutGroup _choicesLayout;
    
        private void Awake()
        {
            _choicesLayout = _choicesContainer.GetComponent<LayoutGroup>();
            LocalizationManager.OnLanguageChanged += RefreshChoiceButtonsAsync;
        }

        public Task ChangeDialogueType(DialogueNode node)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            if (_currentCanvasGroup != choiceCanvasGroup && node.NodeType == EDialogueType.Choice)
            {
                currentDialogueText = choiceText;
                _currentCanvasGroup = choiceCanvasGroup;
                currentDialogueText.text = "";
                
                StartCoroutine(AnimateUI(_mainCanvasGroup, 1, 0));
                StartCoroutine(RunCoroutineWrapper(AnimateUI(choiceCanvasGroup, 0, 1), tcs));
            }
            else if (_currentCanvasGroup != _mainCanvasGroup && node.NodeType == EDialogueType.Message)
            {
                currentDialogueText = _dialogueText;
                _currentCanvasGroup = _mainCanvasGroup;
                currentDialogueText.text = "";
                
                StartCoroutine(RunCoroutineWrapper(AnimateUI(_mainCanvasGroup, 0, 1), tcs));
                StartCoroutine(AnimateUI(choiceCanvasGroup, 1, 0));
            }
            else
            {
                tcs.SetResult(true);
            }
            
            return tcs.Task;
        }

        public async Task EndDialogue()
        {
            StartCoroutine(RemoveIcon(_speakerIcon));
            StartCoroutine(RemoveIcon(_listenerIcon));
            await FadeCanvasAsync(1, 0, _currentCanvasGroup);
            _currentCanvasGroup = null;
            currentDialogueText = null;
        }

        /// <summary>
        /// Define os sprites dos ícones de speaker e listener e aplica animação de entrada.
        /// </summary>
        public void UpdateSpeakerIcons(Sprite speaker, Sprite listener, string speakerName)
        {
            if (_speakerIcon.color.a < 1)
            {
                StartCoroutine(AnimateIcon(_speakerIcon));
                _speakerIcon.color = new Color(1, 1, 1, 1);
            }

            if (_listenerIcon.color.a < 1)
            {
                StartCoroutine(AnimateIcon(_listenerIcon));
                _listenerIcon.color = new Color(1, 1, 1, 1);
            }
            
            SpeakerName.text = speakerName;
            _speakerIcon.sprite = speaker;
            _speakerIcon.gameObject.SetActive(speaker != null);

            _listenerIcon.sprite = listener;
            _listenerIcon.gameObject.SetActive(listener != null);
        }

        /// <summary>
        /// Define o texto base do diálogo e reseta o contador de caracteres visíveis.
        /// </summary>
        public void SetText(string text)
        {
            currentDialogueText.text = text;
            currentDialogueText.maxVisibleCharacters = text.Length;
        }

        /// <summary>
        /// Adiciona o próximo caractere ao texto visível, imitando digitação.
        /// </summary>
        public void AppendText(string character)
        {
            currentDialogueText.maxVisibleCharacters++;
            currentDialogueText.text += character;
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
        public async Task DisplayChoicesAsync(DialogueNode currentNode)
        {
            ClearChoices();
            
            var choices = currentNode.Choices;
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
            if(group.alpha == end) yield break;
            
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
        private Task FadeCanvasAsync(float start, float end, CanvasGroup group)
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(RunCoroutineWrapper(AnimateUI(group, start, end), tcs));
            return tcs.Task;
        }
    
        private IEnumerator RunCoroutineWrapper(IEnumerator coroutine, TaskCompletionSource<bool> tcs)
        {
            yield return StartCoroutine(coroutine);
            tcs.SetResult(true);
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

        private IEnumerator RemoveIcon(Image icon)
        {
            icon.color = Color.white;
        
            var original = icon.transform.localScale;
            float elapsed = 0f;
            const float duration = 0.2f;
            while (elapsed < duration)
            {
                icon.transform.localScale = Vector3.Lerp(original, Vector3.zero, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            icon.transform.localScale = Vector3.zero;
            icon.color = Color.clear;
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