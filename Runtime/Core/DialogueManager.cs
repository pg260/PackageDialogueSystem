using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Runtime.DialogueSystem.Runtime.Data.Containers;
using Runtime.DialogueSystem.Runtime.Data.Enums;
using Runtime.DialogueSystem.Runtime.Data.Nodes;
using Runtime.DialogueSystem.Runtime.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.DialogueSystem.Runtime.Core
{
    /// <summary>
    /// Gerencia o fluxo de diálogos: inicia, processa nós, avança, e finaliza diálogos.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Intervalo entre caracteres ao digitar o texto")]
        [SerializeField]
        private float _textSpeed = 0.05f;

        [Header("Dependencies")]
        [Tooltip("Referência ao gerenciador de UI de diálogo")]
        [SerializeField]
        private DialogueUIManager _dialogueUI;

        private DialogueContainer _currentDialogue;
        private DialogueNode _currentNode;
        private bool _isTyping;
        private Coroutine _typingCoroutine;
    
        private readonly List<Task> _assetLoadingTasks = new();
        
        private List<DialogueEvent> _eventListeners;

        public UnityEvent OnDialogueStart;
        public UnityEvent OnDialogueEnd;

        #region Unity Callbacks
    
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
                return;
            }

            // Atualiza diálogo quando o idioma muda
            LocalizationManager.OnLanguageChanged += RefreshDialogueAsync;
        }
    
        private void OnDestroy()
        {
            LocalizationManager.OnLanguageChanged -= RefreshDialogueAsync;
            if (_typingCoroutine != null)
                StopCoroutine(_typingCoroutine);

            foreach (var task in _assetLoadingTasks)
            {
                if (!task.IsCompleted && task is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    
        #endregion

        #region Public API
    
        /// <summary>
        /// Inicia um novo diálogo usando o container fornecido.
        /// </summary>
        /// <param name="dialogue">Container contendo os nós de diálogo.</param>
        public async void StartDialogue(DialogueContainer dialogue)
        {
            _currentDialogue = dialogue;
            _currentNode = _currentDialogue.Nodes[0];//pegando o primeiro nó pelo index da lista
        
            await _dialogueUI.SetVisibilityAsync(true);
            OnDialogueStart?.Invoke();

            ProcessNode(_currentNode);
        }
    
        /// <summary>
        /// Avança para o próximo nó de diálogo ou conclui a digitação atual.
        /// </summary>
        public void AdvanceDialogue()
        {
            if (_isTyping)
            {
                SkipTyping();
                return;
            }

            if (_currentNode.NodeType == EDialogueType.Choice)
                return;

            var next = _currentDialogue.GetNextNode(_currentNode);
            if (next != null)
            {
                ProcessNode(next);
            }
            else
            {
                EndDialogue();
            }
        }
    
        /// <summary>
        /// Seleciona uma escolha em um nó de diálogo do tipo Choice.
        /// </summary>
        /// <param name="choice">Escolha selecionada.</param>
        public void SelectChoice(DialogueChoice choice)
        {
            if (_isTyping)
            {
                SkipTyping();
                return;
            }

            foreach (var events in choice.OnSelect)
            {
                DialogueEvent.SendSignal(events);
            }

            if (choice.IsExitChoice)
            {
                EndDialogue();
                return;
            }

            var target = _currentDialogue.FindNode(choice.TargetNodeId);
            if (target != null)
            {
                ProcessNode(target);
            }
            else
            {
                Debug.LogError($"Invalid target node: {choice.TargetNodeId}");
                EndDialogue();
            }
        }
    
        /// <summary>
        /// Interrompe a digitação e exibe o texto completo imediatamente.
        /// </summary>
        public void SkipTyping()
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }

            if (_currentNode != null)
            {
                string full = LocalizationManager.Instance.GetLocalizedString(_currentNode.LocalizedText);
                _dialogueUI.SetText(full);
            }

            _isTyping = false;
            OnTypingCompleted();
        }
    
        /// <summary>
        /// Finaliza o diálogo atual e esconde a UI.
        /// </summary>
        public async void EndDialogue()
        {
            foreach (var events in _currentNode.OnNodeExit)
            {
                DialogueEvent.SendSignal(events);
            }

            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }
            
            _dialogueUI.ClearChoices();

            await _dialogueUI.SetVisibilityAsync(false);
            OnDialogueEnd?.Invoke();

            _currentDialogue = null;
            _currentNode = null;
        }
    
        /// <summary>
        /// Registra callbacks para início e fim de diálogos.
        /// </summary>
        public void RegisterDialogueEvents(UnityAction onStart, UnityAction onEnd)
        {
            OnDialogueStart.AddListener(onStart);
            OnDialogueEnd.AddListener(onEnd);
        }
    
        /// <summary>
        /// Remove callbacks registrados.
        /// </summary>
        public void UnregisterDialogueEvents(UnityAction onStart, UnityAction onEnd)
        {
            OnDialogueStart.RemoveListener(onStart);
            OnDialogueEnd.RemoveListener(onEnd);
        }
    
        /// <summary>
        /// Retorna true se um diálogo estiver ativo.
        /// </summary>
        public bool IsDialogueActive() => _currentDialogue != null;
    
        /// <summary>
        /// Define velocidade de digitação em tempo de execução.
        /// </summary>
        public void SetTextSpeed(float speed)
        {
            _textSpeed = Mathf.Clamp(speed, 0.01f, 0.2f);
        }

        /// <summary>
        /// Carrega um novo diálogo imediatamente, sem animações.
        /// </summary>
        public void LoadDialogueImmediate(DialogueContainer dialogue)
        {
            EndDialogue();
            _currentDialogue = dialogue;
            _currentNode = _currentDialogue.FindNode("0");
        }
    
        #endregion
    
        #region Internal Logic
    
        private void FindAndRegisterListeners()
        {
            _eventListeners = new List<DialogueEvent>();
            _eventListeners = FindObjectsByType<DialogueEvent>(FindObjectsSortMode.None).ToList();
        }
        
        /// <summary>
        /// Reprocessa o nó atual ao mudar idioma.
        /// </summary>
        private void RefreshDialogueAsync()
        {
            if (_currentNode != null) ProcessNode(_currentNode);
        }
    
        /// <summary>
        /// Processa a exibição de um nó.
        /// </summary>
        private void ProcessNode(DialogueNode node)
        {
            if (node != _currentNode)
            {
                foreach (var events in _currentNode.OnNodeExit)
                {
                    DialogueEvent.SendSignal(events);
                }   
            }
            
            _currentNode = node;
            _dialogueUI.ClearChoices();

            if (_currentNode.NodeType == EDialogueType.Event)
            {
                foreach (var events in node.OnNodeEnter)
                {
                    DialogueEvent.SendSignal(events);
                }
                
                AdvanceDialogue();
                return;
            }

            _dialogueUI.UpdateSpeakerIcons(
                node.ShowSpeakerIcon ? node.SpeakerIcon : null,
                node.ShowListenerIcon ? node.ListenerIcon : null);

            string text = node.UsePlural
                ? LocalizationManager.Instance.GetPlural(node.LocalizedText, node.PluralQuantity)
                : LocalizationManager.Instance.GetLocalizedString(node.LocalizedText);
            
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            _typingCoroutine = StartCoroutine(TypeTextRoutine(text));

            foreach (var events in node.OnNodeEnter)
            {
                DialogueEvent.SendSignal(events);
            }
        }

        /// <summary>
        /// Coroutine que revela o texto caractere a caractere.
        /// </summary>
        private IEnumerator TypeTextRoutine(string text)
        {
            _isTyping = true;
            _dialogueUI.SetText(string.Empty);
            foreach (char c in text)
            {
                _dialogueUI.AppendText(c.ToString());
                yield return new WaitForSecondsRealtime(_textSpeed);
            }
            _isTyping = false;
            OnTypingCompleted();
        }
        
        /// <summary>
        /// Chamado quando a digitação termina (naturalmente ou por skip).
        /// Verifica se o nó atual é de escolha e instancia os botões.
        /// </summary>
        private async void OnTypingCompleted()
        {
            _isTyping = false;
            _typingCoroutine = null;
            
            if (_currentNode.NodeType == EDialogueType.Choice)
            {
                await _dialogueUI.DisplayChoicesAsync(_currentNode.Choices);
            }
        }
    
        #endregion
    }
}
