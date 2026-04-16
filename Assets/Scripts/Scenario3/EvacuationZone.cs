using UnityEngine;
using UnityEngine.Events;

namespace VRPCCC.Scenario3
{
    public class EvacuationZone : MonoBehaviour
    {
        public string playerTag = "Player";
        public UnityEvent OnPlayerEnter;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                OnPlayerEnter?.Invoke();
            }
        }
    }
}