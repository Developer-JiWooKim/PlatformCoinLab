using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace PlatformCoinLab.Runtime
{
    [Serializable]
    public sealed class PlatformConfig
    {
        public int roundSeconds;
    }

    public sealed class PlatformConfigLoader : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private string remoteUrl;

        public void ApplySettings()
        {
            if (session.IsPlaying || session.IsLoading || !session.IsReady)
            {
                return;
            }

            session.SetBusy(true);
            StartCoroutine(LoadSettings());
        }

        private IEnumerator LoadSettings()
        {
            string url = remoteUrl;

            if (string.IsNullOrWhiteSpace(url))
            {
                string path = Application.streamingAssetsPath + "/platform-config.json";
                url = path.Contains("://") ? path : new Uri(path).AbsoluteUri;
            }
            int seconds = 30;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        PlatformConfig config = JsonUtility.FromJson<PlatformConfig>(request.downloadHandler.text);
                        if (config != null && config.roundSeconds >= 10 && config.roundSeconds <= 120)
                        {
                            seconds = config.roundSeconds;
                        }
                        else
                        {
                            Debug.LogWarning("Invalid roundSeconds; use default 30.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Invalid JSON: {ex.Message}; use default 30.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Config failed: {request.error}; use default 30.");
                }
            }

            session.SetRoundSeconds(seconds);
            session.SetBusy(false);
            session.SetStatus($"Next round: {seconds} seconds");
        }
    }
}