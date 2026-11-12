using UnityEngine;

namespace PlatformCoinLab.Runtime
{
    public sealed class FramePolicy : MonoBehaviour
    {
        [SerializeField] private int androidTargetFps = 30;

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.targetFrameRate = -1;
#elif UNITY_ANDROID && !UNITY_EDITOR
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = androidTargetFps;
            
            // 터치 없이 시간이 흐르는 라운드 도중 화면이 꺼지지 않게
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#else
            // PC와 Editor는 화면 주사율을 따르도록 수직 동기화를 한 주기마다 수행
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
#endif

            Debug.Log($"[FramePolicy] platform={Application.platform} vSync={QualitySettings.vSyncCount} " +
                      $"targetFps={Application.targetFrameRate} refresh={Screen.currentResolution.refreshRateRatio.value:0.##}Hz " +
                      $"screen={Screen.width}x{Screen.height}");
        }
    }
}
