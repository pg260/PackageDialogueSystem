using System.Collections.Generic;
using UnityEngine;

namespace Runtime
{
    public class Multiton<T> : MonoBehaviour where T : Multiton<T>
    {
        public static HashSet<T> _instances;
    
        protected virtual void Awake()
        {
            _instances ??= new();
            _instances.Add((T)this);
        }

        private void OnDestroy()
        {
            _instances.Remove((T)this);
        }
    }
}
