using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// タイトル/ゲーム開始導線のUI表示切替とシーン遷移を担当する。
/// 明示的なデータ保存（ArcadeState）により、JsonUtilityによる破壊的な上書きを回避しつつ状態を維持する。
/// </summary>
public class ChangeScene: MonoBehaviour
{
    public static ChangeScene Instance { get; private set; }
    private static ChangeScene bootstrapSource;
    private bool isPersistentInstance = false;
    private string lastActiveSceneName;

    [Serializable]
    public class ArcadeState
    {
        // Money & Request
        public int money;
        public int requestsCompleted;

        // Inventory (ItemNames to restore)
        public string[] inventoryItemNames = new string[4];
    }

    [Header("UI設定")]
    public GameObject targetUI;
    public bool playButtonSound = true;
    public KeyCode hideKey = KeyCode.Escape;

    [Header("Arcade状態管理")]
    [SerializeField] private string arcadeSceneName = "arcade";
    [SerializeField] private List<string> resetArcadeFromScenes = new List<string>();
    [SerializeField] private bool debugArcadeState = false;
    
    private bool isUIVisible = false;
    private ArcadeState savedState = null;

    void Awake()
    {
        if (transform.parent != null)
        {
            if (Instance == null)
            {
                bootstrapSource = this;
                GameObject host = new GameObject("ChangeScene(Persistent)");
                host.AddComponent<ChangeScene>(); 
            }
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (bootstrapSource != null && bootstrapSource != this)
        {
            CopySettingsFrom(bootstrapSource);
            bootstrapSource = null;
        }

        Instance = this;
        isPersistentInstance = true;
        QualitySettings.vSyncCount = 1; 
        DontDestroyOnLoad(gameObject); 
        lastActiveSceneName = SceneManager.GetActiveScene().name;

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (isPersistentInstance)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        // シーンがアンロードされる直前（オブジェクトがまだ生きている間）に保存する
        if (string.Equals(scene.name, arcadeSceneName, StringComparison.OrdinalIgnoreCase))
        {
            SaveArcadeStateExplicit();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isPersistentInstance) return;

        string previousName = lastActiveSceneName;
        string nextName = scene.name;
        lastActiveSceneName = nextName;

        OnActiveSceneChanged(previousName, nextName);
    }

    private void CopySettingsFrom(ChangeScene source)
    {
        if (source == null) return;
        targetUI = source.targetUI;
        playButtonSound = source.playButtonSound;
        hideKey = source.hideKey;
        arcadeSceneName = source.arcadeSceneName;
        resetArcadeFromScenes = source.resetArcadeFromScenes;
        debugArcadeState = source.debugArcadeState;
    }

    private void OnActiveSceneChanged(string previousName, string nextName)
    {
        if (!string.Equals(nextName, arcadeSceneName, StringComparison.OrdinalIgnoreCase)) return;

        DisableGlobalDepthOfField();

        bool shouldReset = false;
        if (resetArcadeFromScenes == null || resetArcadeFromScenes.Count == 0)
        {
            shouldReset = string.Equals(previousName, SceneNames.Title, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            foreach (var s in resetArcadeFromScenes)
            {
                if (string.Equals(previousName, s, StringComparison.OrdinalIgnoreCase))
                {
                    shouldReset = true;
                    break;
                }
            }
        }

        if (shouldReset)
        {
            savedState = null;
            ResetArcadeRuntimeState();
            return;
        }

        StartCoroutine(RestoreArcadeStateExplicit());
    }

    private void SaveArcadeStateExplicit()
    {
        savedState = new ArcadeState();
        savedState.money = MoneyManager.currentMoney;
        savedState.requestsCompleted = RequestManager.RequestCompleted;

        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            for (int i = 0; i < 4; i++)
            {
                var slot = inv.GetSlot(i);
                if (slot != null && slot.CurrentItem != null)
                    savedState.inventoryItemNames[i] = slot.CurrentItem.itemName;
            }
        }

        if (debugArcadeState) Debug.Log("ArcadeState: Saved inventory and basic stats.");
    }

    private IEnumerator RestoreArcadeStateExplicit()
    {
        yield return null; 

        if (savedState == null) yield break;

        MoneyManager.currentMoney = savedState.money;
        RequestManager.RequestCompleted = savedState.requestsCompleted;

        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            inv.RestoreInventory(savedState.inventoryItemNames);
        }

        if (debugArcadeState) Debug.Log("ArcadeState: Restored inventory and basic stats.");
    }

    private void ResetArcadeRuntimeState()
    {
        OwnedProgressManager.ResetAll();
    }

    void Update()
    {
        if (isUIVisible && Input.GetKeyDown(hideKey))
        {
            HideUI();
        }
    }
    
    public void change_button()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.deliverySound);
        }

        if (string.Equals(SceneManager.GetActiveScene().name, SceneNames.Title, StringComparison.OrdinalIgnoreCase))
        {
            MoneyManager.currentMoney = 0;
            RequestManager.RequestCompleted = 0;
            OwnedProgressManager.ResetAll();
            DayAdvanceButton.ResetPersistentState();
            GameClockText.ResetPersistentState();
            savedState = null;
        }

        FadeManager.Instance.LoadSceneWithFade(SceneNames.Arcade);
    }
    
    public void ShowUI()
    {
        if (playButtonSound && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.buttonClickSound);
        }
        
        if (targetUI != null)
        {
            targetUI.SetActive(true);
            isUIVisible = true;
        }
    }
    
    public void HideUI()
    {
        if (targetUI != null)
        {
            targetUI.SetActive(false);
            isUIVisible = false;
        }
    }
    
    public void ToggleUI()
    {
        if (isUIVisible) HideUI();
        else ShowUI();
    }
    
    public bool IsUIVisible() => isUIVisible;

    private void DisableGlobalDepthOfField()
    {
        Volume volume = FindGlobalVolume();
        if (volume == null)
        {
            return;
        }

        VolumeProfile profile = volume.profile;
        if (profile != null && profile.TryGet(out DepthOfField depthOfField))
        {
            depthOfField.active = false;
        }
    }

    private Volume FindGlobalVolume()
    {
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        for (int i = 0; i < volumes.Length; i++)
        {
            if (volumes[i] != null && volumes[i].isGlobal)
            {
                return volumes[i];
            }
        }
        return null;
    }
}
