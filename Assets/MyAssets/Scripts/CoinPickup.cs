using UnityEngine;

namespace PlatformCoinLab
{
    public sealed class CoinPickup : MonoBehaviour
    {
        [SerializeField] private int _coinId;

        private GameSession _session;
        private Transform _player;

        public void Initialize(GameSession owner, Transform target)
        {
            _session = owner;
            _player = target;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 아직 초기화되지 않았거나, 접촉한 Collider가 지정한 Player의 것이 아니면 무시합니다.
            if (_session == null || other.transform != _player)
            {
                return;
            }

            // 게임 규칙에서 수집을 승인한 경우에만 코인을 숨깁니다.
            if (_session.TryCollect(_coinId))
            {
                gameObject.SetActive(false);
            }
        }
    }
}