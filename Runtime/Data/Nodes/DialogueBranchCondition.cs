using Runtime.DialogueSystem.Runtime.Data.Enums;
using UnityEngine;

namespace Runtime.DialogueSystem.Runtime.Data.Nodes
{
    [System.Serializable]
    public class DialogueBranchCondition
    {
        [Tooltip("Game state variable to check")]
        public string ConditionKey;

        [Tooltip("Comparison type")]
        public EComparisonType Comparison;

        [Tooltip("Required value")]
        public float RequiredValue;

        [Tooltip("Node to jump to if condition is met")]
        public string TargetNodeId;
    }
}