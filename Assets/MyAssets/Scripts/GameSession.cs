using UnityEngine;
using TMPro;
using Unity.Profiling;

namespace PlatformCoinLab
{
    public sealed class GameSession : MonoBehaviour
    {
        private static readonly ProfilerMarker UiMarker = new("CoinLab.PresentUi");

        // 최고 점수는 스테이지마다 따로 저장. 예: PlatformCoinLab.Best.stage-main
        private const string BestKeyPrefix = "PlatformCoinLab.Best.";
        private const string LegacyBestKey = "PlatformCoinLab.Best";
        private const string LegacyStageId = "stage-main";

        [SerializeField] private Transform player;
        [SerializeField] private CoinPickup[] coins;
        [SerializeField] private AudioSource collectAudio;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text hud;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private UnityEngine.UI.Button startButton;
        [SerializeField] private TMP_Text bestText;

        private int shownScore = -1;
        private int shownSeconds = -1;
        private int shownBest = -1;
        private bool shownPlaying;
        private bool shownResult;
        private bool shownReady;

        private int best;
        private string bestKey;
        private AudioClip generatedCollectClip;
        private CoinRound round;
        private Vector3 startPosition;
        private float roundSeconds = 30f;
        private bool playing;
        private bool showingResult;
        private bool busy;
        private bool paused;
        public int Score => round.Score;
        public bool IsPlaying => playing;
        public bool CanMove => playing && !paused;
        public bool IsLoading => busy;
        public bool IsReady => coins != null && coins.Length > 0 && !busy;

        private void Awake()
        {
            round = new CoinRound(roundSeconds);
            startPosition = player.position;

            MigrateLegacyBest();
            // StageLoader가 스테이지 목록을 받기 전까지는 기본 스테이지의 기록을 표시합니다.
            // RefreshUi가 round를 사용하므로 round 생성 뒤에 호출합니다.
            SetStageId(LegacyStageId);

            // 외부 AudioClip이 연결되어 있지 않을 때만 짧은 수집음을 직접 만듭니다.
            if (collectAudio != null && collectAudio.clip == null)
            {
                // 초당 22,050개 표본 중 2,205개를 사용하므로 약 0.1초 길이입니다.
                float[] samples = new float[2205];
                for (int i = 0; i < samples.Length; i++)
                {
                    // i / 22050f는 현재 표본의 시간(초), 880f는 1초에 진동하는 횟수입니다.
                    // Mathf.PI는 원주율(π)입니다. Sin(2π × 진동수 × 시간)에 남은 길이 비율을 곱해 소리를 서서히 줄입니다.
                    // (float)는 정수 나눗셈을 피하기 위한 변환이며, 0.25f는 파형의 크기를 줄이는 계수입니다.
                    samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * i / 22050f) * (1f - (float)i / samples.Length) * 0.25f;
                }

                // 표본 수·모노 1채널·초당 표본 수를 지정한 뒤, SetData로 처음 위치 0부터 파형을 채웁니다.
                generatedCollectClip = AudioClip.Create("CollectTone", samples.Length, 1, 22050, false);
                generatedCollectClip.SetData(samples, 0);

                collectAudio.clip = generatedCollectClip;
            }
        }

        private void OnDestroy()
        {
            if (generatedCollectClip != null)
            {
                Destroy(generatedCollectClip);
            }
        }

        private void Start()
        {
            SetStage(coins);
        }

        private void Update()
        {
            if (playing && !paused)
            {
                round.Tick(Time.deltaTime);

                if (round.IsFinished)
                {
                    FinishRound();
                }
            }

            RefreshUi();
        }

        public void BeginRound()
        {
            if (!IsReady || playing)
            {
                return;
            }

            round.Reset(roundSeconds);
            player.position = startPosition;

            foreach (CoinPickup coin in coins)
            {
                coin.gameObject.SetActive(true);
            }

            playing = true;
            showingResult = false;
            paused = false;

            RefreshUi();
        }

        public bool TryCollect(int id)
        {
            if (!CanMove || !round.TryCollect(id))
            {
                return false;
            }

            if (collectAudio != null)
            {
                collectAudio.Play();
            }

            return true;
        }

        public void ReturnToMenu()
        {
            playing = false;
            showingResult = false;
            paused = false;

            RefreshUi();
        }

        public void SetStage(CoinPickup[] stageCoins)
        {
            coins = stageCoins ?? new CoinPickup[0];
            foreach (CoinPickup coin in coins)
            {
                coin.Initialize(this, player);
            }

            RefreshUi();
        }

        public void SetBusy(bool value)
        {
            busy = value;

            RefreshUi();
        }

        public void SetStatus(string message)
        {
            statusText.text = message;
        }

        public void SetRoundSeconds(int seconds)
        {
            if (playing)
            {
                return;
            }

            roundSeconds = Mathf.Clamp(seconds, 10, 120);
        }

        private void FinishRound()
        {
            playing = false;
            showingResult = true;
            SaveBest();
        }
        public void SetStageId(string stageId)
        {
            if (playing)
            {
                return;
            }

            bestKey = BestKeyPrefix + (string.IsNullOrEmpty(stageId) ? LegacyStageId : stageId);
            best = PlayerPrefs.GetInt(bestKey, 0);

            RefreshUi();
        }

        private static void MigrateLegacyBest()
        {
            if (!PlayerPrefs.HasKey(LegacyBestKey))
            {
                return;
            }

            string newKey = BestKeyPrefix + LegacyStageId;
            if (!PlayerPrefs.HasKey(newKey))
            {
                PlayerPrefs.SetInt(newKey, PlayerPrefs.GetInt(LegacyBestKey, 0));
            }

            PlayerPrefs.DeleteKey(LegacyBestKey);
            PlayerPrefs.Save();
        }

        public void SaveBest()
        {
            best = Mathf.Max(best, Score);
            PlayerPrefs.SetInt(bestKey, best);
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool value)
        {
            // 중단:true, 복귀:false 
            paused = value;
            if (value)
            {
                SaveBest();
            }
        }

        private void RefreshUi()
        {
            // 기존 최적화 전 코드 - 텍스트가 매 프레임 생성 및 갱신되며 GC 발생함
            // menuPanel.SetActive(!playing && !showingResult);
            // hudPanel.SetActive(playing);
            // resultPanel.SetActive(showingResult);
            // startButton.interactable = IsReady;
            // hud.text = $"Score: {Score}  Time: {Mathf.CeilToInt(round.Remaining)}";
            // resultText.text = $"Score: {Score}";

            // bestText.text = $"Best: {best}";

            using (UiMarker.Auto())
            {
                int seconds = Mathf.CeilToInt(round.Remaining);
                bool ready = IsReady;

                if (shownScore == Score
                    && shownSeconds == seconds
                    && shownBest == best
                    && shownPlaying == playing
                    && shownResult == showingResult
                    && shownReady == ready)
                {
                    return;
                }

                shownScore = Score;
                shownSeconds = seconds;
                shownBest = best;
                shownPlaying = playing;
                shownResult = showingResult;
                shownReady = ready;
                menuPanel.SetActive(!playing && !showingResult);
                hudPanel.SetActive(playing);
                resultPanel.SetActive(showingResult);
                startButton.interactable = ready;
                hud.text = $"Score: {Score}  Time: {seconds}";
                resultText.text = $"Score: {Score}";
                bestText.text = $"Best: {best}";
            }
        }
    }

}