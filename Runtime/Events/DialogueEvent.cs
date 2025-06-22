using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime
{
    public class DialogueEvent : Multiton<DialogueEvent>
    {
        [SerializeField] private List<DialogueSignal> signals;
        private Dictionary<string, UnityEvent> events;

        protected override void Awake()
        {
            base.Awake();
            events = new Dictionary<string, UnityEvent>();
            foreach (var signal in signals)
            {
                events.Add(signal.Name, signal.Event);
            }
        }

        public static void SendSignal(string name)
        {
            if(string.IsNullOrEmpty(name) || _instances == null || _instances.Count == 0) return;
            
            Debug.Log("evento chamado");
            foreach (var instance in _instances)
            {
                if (instance.events.TryGetValue(name, out var sig))
                {
                    sig?.Invoke();
                }
            }
        }
    }
}
