using UnityEngine;

namespace MCPTest
{
    public sealed class CoinManager : MonoBehaviour
    {
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private Camera targetCamera;
        [SerializeField, Range(0f, 0.25f)] private float viewportPadding = 0.1f;

        private GameObject currentCoin;

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            SpawnCoin();
        }

        public void CollectCoin(GameObject coin)
        {
            if (coin == null)
            {
                return;
            }

            bool wasManagedCoin = coin == currentCoin;
            Destroy(coin);

            if (wasManagedCoin)
            {
                currentCoin = null;
                SpawnCoin();
            }
        }

        private void SpawnCoin()
        {
            if (coinPrefab == null)
            {
                Debug.LogError("MCPTest CoinManager requires a coinPrefab reference.", this);
                return;
            }

            if (targetCamera == null)
            {
                Debug.LogError("MCPTest CoinManager could not find a camera.", this);
                return;
            }

            float depth = Mathf.Abs(targetCamera.transform.position.z);
            Vector3 minimum = targetCamera.ViewportToWorldPoint(
                new Vector3(viewportPadding, viewportPadding, depth));
            Vector3 maximum = targetCamera.ViewportToWorldPoint(
                new Vector3(1f - viewportPadding, 1f - viewportPadding, depth));

            float x = Random.Range(minimum.x, maximum.x);
            float y = Random.Range(minimum.y, maximum.y);
            currentCoin = Instantiate(coinPrefab, new Vector3(x, y, 0f), Quaternion.identity);
        }
    }
}
