using Core.Pooling;
using UnityEngine;

namespace Core.Utils
{
    [AddComponentMenu("Core/Utils/Out Of Bounds Trigger")]
    public class OutOfBoundsTrigger : MonoBehaviour
    {
        [SerializeField] private bool recycleToPool = true;
        [SerializeField] private LayerMask targetLayers = ~0;

        private void OnTriggerEnter(Collider other)
        {
            if ((targetLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            HandleOutOfBounds(other.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((targetLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            HandleOutOfBounds(other.gameObject);
        }

        private void HandleOutOfBounds(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (recycleToPool && PoolManager.HasInstance)
            {
                PoolManager.Instance.Despawn(target);
            }
            else
            {
                Destroy(target);
            }
        }
    }
}
