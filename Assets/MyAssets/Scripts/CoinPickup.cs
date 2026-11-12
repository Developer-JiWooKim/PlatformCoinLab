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
            if (_session == null || other.transform != _player)
            {
                return;
            }

            if (_session.TryCollect(_coinId))
            {
                gameObject.SetActive(false);
            }
        }
    }
}