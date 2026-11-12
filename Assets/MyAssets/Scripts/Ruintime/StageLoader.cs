using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace PlatformCoinLab.Runtime
{
    public sealed class StageLoader : MonoBehaviour
    {
        private const string SelectedStageKey = "PlatformCoinLab.SelectedStage";

        [SerializeField] private GameSession session;
        [SerializeField] private string address = "stage-main";
        [SerializeField] private string stageLabel = "stage";
        [SerializeField] private TMP_Text stageNameText;

        private readonly List<string> stageAddresses = new();
        private int selectedIndex;

        private AsyncOperationHandle<GameObject> handle;
        private bool ownerDestroyed;

        public string SelectedAddress => stageAddresses.Count > 0 ? stageAddresses[selectedIndex] : address;

        private void Start()
        {
            session.SetBusy(true);
            session.SetStatus("Finding stages...");

            AsyncOperationHandle<IList<IResourceLocation>> locations = Addressables.LoadResourceLocationsAsync(stageLabel, typeof(GameObject));
            locations.Completed += OnLocationsCompleted;
        }

        private void OnLocationsCompleted(AsyncOperationHandle<IList<IResourceLocation>> operation)
        {
            stageAddresses.Clear();

            if (operation.Status == AsyncOperationStatus.Succeeded && operation.Result != null)
            {
                stageAddresses.AddRange(operation.Result
                    .Select(location => location.PrimaryKey)
                    .Distinct()
                    .OrderBy(key => key, StringComparer.Ordinal));
            }
            else
            {
                Debug.LogWarning($"Stage list failed: {operation.OperationException}; use default '{address}'.");
            }

            Addressables.Release(operation);

            if (ownerDestroyed)
            {
                return;
            }

            if (stageAddresses.Count == 0)
            {
                stageAddresses.Add(address);
            }

            string saved = PlayerPrefs.GetString(SelectedStageKey, address);
            selectedIndex = Mathf.Max(0, stageAddresses.IndexOf(saved));

            session.SetBusy(false);
            ApplySelection();
            LoadStage();
        }

        public void SelectNext()
        {
            ChangeSelection(1);
        }

        public void SelectPrevious()
        {
            ChangeSelection(-1);
        }

        private void ChangeSelection(int step)
        {
            if (session.IsPlaying || session.IsLoading || stageAddresses.Count <= 1)
            {
                return;
            }

            selectedIndex = (selectedIndex + step + stageAddresses.Count) % stageAddresses.Count;

            PlayerPrefs.SetString(SelectedStageKey, SelectedAddress);
            PlayerPrefs.Save();

            ApplySelection();
            LoadStage();
        }

        private void ApplySelection()
        {
            session.SetStageId(SelectedAddress);

            if (stageNameText != null)
            {
                stageNameText.text = $"Stage {selectedIndex + 1}/{stageAddresses.Count}: {SelectedAddress}";
            }
        }

        public void LoadStage()
        {
            if (session.IsPlaying || session.IsLoading)
            {
                return;
            }

            string target = SelectedAddress;

            session.SetBusy(true);
            session.SetStatus($"Loading {target}...");
            session.SetStage(null);

            ReleaseOwnedOperation();

            handle = Addressables.InstantiateAsync(target, Vector3.zero, Quaternion.identity);

            handle.Completed += OnCompleted;
        }

        private void OnCompleted(AsyncOperationHandle<GameObject> operation)
        {
            if (ownerDestroyed)
            {
                ReleaseOwnedOperation();
                return;
            }

            if (operation.Status != AsyncOperationStatus.Succeeded)
            {
                session.SetBusy(false);
                session.SetStatus("Stage load failed. Press Reload Stage.");
                Debug.LogError(operation.OperationException);
                ReleaseOwnedOperation();
                return;
            }

            CoinPickup[] coins = operation.Result.GetComponentsInChildren<CoinPickup>(true);
            if (coins.Length == 0)
            {
                ReleaseOwnedOperation();
                session.SetBusy(false);
                session.SetStatus("Stage has no coins. Check the prefab.");
                return;
            }

            session.SetStage(coins);
            session.SetBusy(false);
            session.SetStatus("Stage ready. Press Start.");
        }

        private void OnDestroy()
        {
            ownerDestroyed = true;

            if (handle.IsValid() && handle.IsDone)
            {
                ReleaseOwnedOperation();
            }
        }

        private void ReleaseOwnedOperation()
        {
            if (!handle.IsValid())
            {
                return;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Addressables.ReleaseInstance(handle);
            }
            else
            {
                Addressables.Release(handle);
            }

            handle = default;
        }
    }
}
