using UnityEngine;

namespace MCPTest
{
    public sealed class CoinPickup : MonoBehaviour
    {
        private bool collected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !other.TryGetComponent<PlayerController>(out _))
            {
                return;
            }

            CoinManager manager = FindAnyObjectByType<CoinManager>();
            if (manager == null)
            {
                Debug.LogError("MCPTest CoinPickup could not find CoinManager.", this);
                return;
            }

            collected = true;
            manager.CollectCoin(gameObject);
        }
    }
}
