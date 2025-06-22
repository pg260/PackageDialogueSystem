using System.Collections.Generic;
using Runtime.DialogueSystem.Runtime.Data.Enums;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;

namespace Runtime.DialogueSystem.Runtime.Data.Nodes
{
    [System.Serializable]
    public class DialogueNode
    {
        [Tooltip("Unique identifier for this node")]
        public string NodeId;

        [Tooltip("Determines how this node behaves in the dialogue flow")]
        public EDialogueType NodeType = EDialogueType.Message;

        [Header("Text Configuration")]
        [Tooltip("Localized text content using Unity's Localization system")]
        public LocalizedString LocalizedText;

        [Tooltip("Enable to use pluralization rules for this text")]
        public bool UsePlural;

        [Tooltip("Quantity value used for pluralization")]
        [Min(0)] public int PluralQuantity = 1;

        [Header("Speaker Configuration")]
        [Tooltip("Localized speaker name (optional)")]
        public LocalizedString SpeakerNameKey;

        [Tooltip("Speaker icon")]
        public Sprite SpeakerIcon;

        [Tooltip("Listener icon")]
        public Sprite ListenerIcon;

        [Space]
        [Tooltip("Show speaker icon in UI")]
        public bool ShowSpeakerIcon = true;

        [Tooltip("Show listener icon in UI")]
        public bool ShowListenerIcon = true;

        [Header("Flow Control")]
        [Tooltip("Next node for automatic progression")]
        public string DefaultNextNodeId;

        [Tooltip("Conditions for branching nodes")]
        public List<DialogueBranchCondition> BranchConditions = new();

        [Header("Choices")]
        [Tooltip("Available choices for this node")]
        public List<DialogueChoice> Choices = new();

        [Header("Events")]
        [Tooltip("Triggered when node becomes active")]
        public UnityEvent OnNodeEnter;

        [Tooltip("Triggered when node is exited")]
        public UnityEvent OnNodeExit;
        
        #region Graph
        [HideInInspector]
        public Vector2 Position = new Vector2(0, 0);
        #endregion
    }
}