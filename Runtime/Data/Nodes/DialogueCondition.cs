using Runtime.DialogueSystem.Runtime.Data.Enums;

namespace Runtime.DialogueSystem.Runtime.Data.Nodes
{
    [System.Serializable]
    public class DialogueCondition
    {
        public string VariableName;
        public EComparisonType Comparison;
        public float RequiredValue;
    }
}