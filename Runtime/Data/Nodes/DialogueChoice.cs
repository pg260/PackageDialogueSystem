using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Localization;

namespace Runtime.DialogueSystem.Runtime.Data.Nodes
{
    [System.Serializable]
    public class DialogueChoice
    {
        public LocalizedString ChoiceLocalizationKey;
        public string TargetNodeId;
        public bool IsExitChoice;
        public UnityEvent OnSelect;
        public List<DialogueCondition> RequiredConditions;
    }
}
