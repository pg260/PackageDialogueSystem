using System;
using UnityEngine.Events;

namespace Runtime
{
    [Serializable]
    public class DialogueSignal
    {
        public string Name;
        public UnityEvent Event;
    }
}
