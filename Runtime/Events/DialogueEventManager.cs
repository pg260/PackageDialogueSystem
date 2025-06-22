using UnityEngine;

namespace Runtime
{
    [CreateAssetMenu(fileName = "DialogueEventManager", menuName = "Dialogue/DialogueEventManager")]
    public class DialogueEventManager : ScriptableObject
    {
        public static void SendSignal( string signal ) =>
            DialogueEvent.SendSignal( signal );
    }
}
