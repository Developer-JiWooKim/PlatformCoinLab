using TMPro;
using UnityEngine;

namespace PlatformCoinLab
{
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private Transform _player;
        [SerializeField] private CoinPickup[] _coins;
        [SerializeField] private AudioSource _collectAudio;
        [SerializeField] private GameObject _menuPanel;
        [SerializeField] private GameObject _hudPanel;
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TMP_Text _hud;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private UnityEngine.UI.Button _startButton;

        private AudioClip _generatedCollectClip;
        private CoinRound _round;
        private Vector3 _startPosition;
        private float _roundSeconds = 30f;
        private bool _playing;
        private bool _showingResult;
        private bool _busy;
        private bool _paused;

        public int Score => _round.Score;
        public bool IsPlaying => _playing;
        public bool CanMove => _playing && !_paused;
        public bool IsLoading => _busy;
        public bool IsReady => _coins != null && _coins.Length > 0 && !_busy;

        private void Awake()
        {
            _round = new(_roundSeconds);
            _startPosition = _player.position;

            if (_collectAudio != null && _collectAudio.clip == null)
            {
                float[] samples = new float[2205];
                for (int i = 0; i < samples.Length; i++)
                {
                    // i / 22050f는 현재 표본의 시간(초), 880f는 1초에 진동하는 횟수입니다.
                    // Mathf.PI는 원주율(π)입니다. Sin(2π × 진동수 × 시간)에 남은 길이 비율을 곱해 소리를 서서히 줄입니다.
                    // (float)는 정수 나눗셈을 피하기 위한 변환이며, 0.25f는 파형의 크기를 줄이는 계수입니다.
                    samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * i / 22050f) * (1f - (float)i / samples.Length) * 0.25f;
                }

                _generatedCollectClip = AudioClip.Create("CollectTone", samples.Length, 1, 22050, false);
                _generatedCollectClip.SetData(samples, 0);

                _collectAudio.clip = _generatedCollectClip;
            }
        }

        private void OnDestroy()
        {
            if (_generatedCollectClip != null)
            {
                Destroy(_generatedCollectClip);
            }
        }

        private void Start()
        {
            if (_playing && !_paused)
            {
                _round.Tick(Time.deltaTime);
                if (_round.IsFinished)
                {
                    FinishRound();
                }
            }

            RefreshUi();
        }

        private void Update()
        {
            if (_playing && !_paused)
            {
                // 위->아래: 이번 프레임의 시간을 반영한 뒤 종료 여부를 판단합니다.
                _round.Tick(Time.deltaTime);

                if (_round.IsFinished)
                {
                    FinishRound();
                }
            }

            RefreshUi();
        }

        public void BeginRound()
        {
            if (!IsReady || _playing)
            {
                return;
            }

            _round.Reset(_roundSeconds);
            _player.position = _startPosition;

            foreach (CoinPickup coin in _coins)
            {
                coin.gameObject.SetActive(true);
            }

            _playing = true;
            _showingResult = false;
            _paused = false;

            RefreshUi();
        }
        public bool TryCollect(int id)
        {
            // 진행 중이 아니면 수집을 요청하지 않고, 진행 중이어도 최종 승인 여부는 CoinRound가 판단합니다.
            if (!CanMove || !_round.TryCollect(id))
            {
                return false;
            }

            if (_collectAudio != null)
            {
                _collectAudio.Play();
            }

            return true;
        }

        public void ReturnToMenu()
        {
            _playing = false;
            _showingResult = false;
            _paused = false;

            RefreshUi();
        }

        public void SetStage(CoinPickup[] stageCoins)
        {
            // null은 아직 스테이지가 없다는 뜻입니다. 빈 배열로 바꾸어 아래 foreach를 안전하게 건너뜁니다.
            _coins = stageCoins ?? new CoinPickup[0];
            foreach (CoinPickup coin in _coins)
            {
                coin.Initialize(this, _player);
            }

            RefreshUi();
        }

        public void SetBusy(bool value)
        {
            _busy = value;

            RefreshUi();
        }

        public void SetStatus(string message)
        {
            _statusText.text = message;
        }

        public void SetRoundSeconds(int seconds)
        {
            if (_playing)
            {
                return;
            }

            // 다음 라운드의 제한 시간을 허용 범위인 10~120초 안으로 맞춥니다.
            _roundSeconds = Mathf.Clamp(seconds, 10, 120);
        }

        private void FinishRound()
        {
            _playing = false;
            _showingResult = true;
        }

        private void RefreshUi()
        {
            _menuPanel.SetActive(!_playing && !_showingResult);
            _hudPanel.SetActive(_playing);
            _resultPanel.SetActive(_showingResult);
            _startButton.interactable = IsReady;
            // 0.2초가 남았을 때 0으로 보이지 않도록 올림해서 1초로 표시합니다.
            _hud.text = $"Score: {Score}  Time: {Mathf.CeilToInt(_round.Remaining)}";
            _resultText.text = $"Score: {Score}";
        }
    }
}