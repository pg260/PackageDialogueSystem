using UnityEngine;

namespace Runtime.DialogueSystem.Runtime.Data.Enums
{
    public enum EComparisonType
    {
        [InspectorName("> Greater Than")]
        GreaterThan,
        [InspectorName("< Less Than")]
        LessThan,
        [InspectorName("== Equals")]
        Equals,
        [InspectorName("!= Not Equal")]
        NotEqual,
        [InspectorName("Exists in Inventory")]
        Exists,
        [InspectorName("Quest Active")]
        QuestActive
    }
}