using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// タイトル/ゲーム開始導線のUI表示切替とシーン遷移を担当する。
/// `SoundManager` と `FadeManager` を利用して遷移演出を統一する。
/// </summary>
public class ChangeScene: MonoBehaviour
{
    public static ChangeScene Instance { get; private set; }
    private static ChangeScene bootstrapSource;
    private bool isPersistentInstance = false;
    private string lastActiveSceneName;

    [Serializable]
    private class SavedComponentState
    {
        public string objectPath;
        public string componentType;
        public string json;
    }
    [Header("UI設定")]
    [Tooltip("表示するUIオブジェクト")]
    public GameObject targetUI;
    
    [Tooltip("UI表示時のボタンクリック音を再生するか")]
    public bool playButtonSound = true;
    
/*    [Header("点滅UI設定")]
    [Tooltip("点滅させるUIオブジェクト")]
    public GameObject blinkUI;
    
    [Tooltip("点滅の速度（秒）")]
    public float blinkSpeed = 2f;
    
    [Tooltip("点滅の最小アルファ値")]
    [Range(0f, 1f)]
    public float minAlpha = 0.2f;
    
    [Tooltip("点滅の最大アルファ値")]
    [Range(0f, 1f)]
    public float maxAlpha = 1f; */
    
    [Header("キー設定")]
    [Tooltip("UIを非表示にするキー")]
    public KeyCode hideKey = KeyCode.Escape;

    [Header("Arcade状態管理")]
    [Tooltip("状態保存/復元の対象となるシーン名")]
    [SerializeField] private string arcadeSceneName = "arcade";
    [Tooltip("このシーンから arcade に遷移した場合は保存状態を使わず初期化する")]
    [SerializeField] private List<string> resetArcadeFromScenes = new List<string>();
    [Tooltip("Arcade状態管理のログを出力する")]
    [SerializeField] private bool debugArcadeState = false;
    
    private bool isUIVisible = false;
  //  private bool isBlinking = false;
    private Coroutine blinkCoroutine;
    private CanvasGroup blinkCanvasGroup;

    // arcade の実行時状態スナップショット
    private bool hasSavedArcadeState = false;
    private int savedMoney = 0;
    private int savedRequestCompleted = 0;
    private int savedDay = 1;
    private int savedCompleteMoneyThreshold = 0;
    private bool savedRequestBoardPlaySound = true;
    private readonly List<SavedComponentState> savedComponentStates = new List<SavedComponentState>();
    
    void Awake()
    {
        // UI配下に置かれている ChangeScene は「ボタン用の入口」として残す。
        // 常駐処理（保存/復元）は root のインスタンスだけが担当する。
        if (transform.parent != null)
        {
            if (Instance == null)
            {
                bootstrapSource = this;
                GameObject host = new GameObject("ChangeScene(Persistent)");
                host.AddComponent<ChangeScene>(); // Awake内で bootstrapSource から設定を引き継ぐ
            }
            // UI側はDontDestroy/イベント購読しない（ボタン機能は残す）
            return;
        }

        // ここに来るのは root（常駐）候補のみ
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // bootstrap（UI上）から生成された常駐側なら、設定を引き継ぐ
        if (bootstrapSource != null && bootstrapSource != this)
        {
            CopySettingsFrom(bootstrapSource);
            bootstrapSource = null;
        }

        Instance = this;
        isPersistentInstance = true;
        QualitySettings.vSyncCount = 1; // VSyncを無効にすることでtargetFrameRateが有効になる
        DontDestroyOnLoad(gameObject); // シーンをまたいでも破棄されないようにする
        lastActiveSceneName = SceneManager.GetActiveScene().name;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(SoundManager.Instance.soundData.gameplayBGM);
        }

        // activeSceneChanged は環境によっては期待通り追えないことがあるため、
        // より確実な sceneLoaded を主に使って遷移を検出する。
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (debugArcadeState)
        {
            Debug.Log($"ArcadeState: Persistent Awake (active='{lastActiveSceneName}')");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (isPersistentInstance)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isPersistentInstance) return;

        // sceneLoaded の時点で activeScene は基本この scene になるが、
        // 直前の activeScene 名を控えておき、擬似的に previous/next を作る。
        string previousName = lastActiveSceneName;
        string nextName = scene.name;
        lastActiveSceneName = nextName;

        if (debugArcadeState)
        {
            Debug.Log($"ArcadeState: sceneLoaded prev='{previousName}' next='{nextName}' mode={mode}");
        }

        // 既存ロジックに合わせて Scene 構造に変換
        Scene previousScene = SceneManager.GetSceneByName(previousName);
        Scene nextScene = scene;
        OnActiveSceneChanged(previousScene, nextScene);
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

    private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        // arcade を離れるタイミングで状態をスナップショット化
        if (previousScene.name == arcadeSceneName)
        {
            SaveArcadeState(previousScene);
        }

        if (nextScene.name != arcadeSceneName) return;

        // 仕様: title から arcade に入った場合のみ初期化したい。
        // インスペクタ側の設定漏れ/誤設定で他シーンからも初期化されないよう、
        // リセット対象リストが空の場合は "title" のみを初期化対象にする。
        bool shouldReset;
        if (resetArcadeFromScenes == null || resetArcadeFromScenes.Count == 0)
        {
            shouldReset = previousScene.name == "title";
        }
        else
        {
            shouldReset = resetArcadeFromScenes.Contains(previousScene.name);
        }
        if (shouldReset)
        {
            // 指定シーンから arcade に入る場合は、保存状態を使わず初期状態へ
            ClearSavedArcadeState();
            ResetArcadeRuntimeState();
            if (debugArcadeState) Debug.Log($"ArcadeState: '{previousScene.name}' から遷移したため初期化しました");
            return;
        }

        // それ以外は保存済み状態を復元（1フレーム待って各コンポーネント初期化後に上書き）
        StartCoroutine(RestoreArcadeStateNextFrame(nextScene));
    }

    private void SaveArcadeState(Scene arcadeScene)
    {
        savedComponentStates.Clear();

        // 静的状態も含めて保存（title→arcade の場合のみリセットする）
        savedMoney = MoneyManager.currentMoney;
        savedRequestCompleted = RequestManager.RequestCompleted;
        savedRequestBoardPlaySound = RequestBoard.playRequestSound;

        // GameClockText / DayAdvanceButton は復元が外れるケースがあるため、値を明示的に保存する
        GameObject[] rootsForScan = arcadeScene.GetRootGameObjects();
        for (int i = 0; i < rootsForScan.Length; i++)
        {
            if (rootsForScan[i] == null) continue;

            if (savedDay == 1)
            {
                DayAdvanceButton dayButton = rootsForScan[i].GetComponentInChildren<DayAdvanceButton>(true);
                if (dayButton != null) savedDay = dayButton.GetDay();
            }

            if (savedCompleteMoneyThreshold == 0)
            {
                GameClockText clock = rootsForScan[i].GetComponentInChildren<GameClockText>(true);
                if (clock != null) savedCompleteMoneyThreshold = clock.GetCompleteMoneyThreshold();
            }

            if (savedDay != 1 && savedCompleteMoneyThreshold != 0) break;
        }

        GameObject[] roots = arcadeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            MonoBehaviour[] behaviours = roots[i].GetComponentsInChildren<MonoBehaviour>(true);
            for (int j = 0; j < behaviours.Length; j++)
            {
                MonoBehaviour behaviour = behaviours[j];
                if (behaviour == null) continue;

                string json = JsonUtility.ToJson(behaviour);
                if (string.IsNullOrEmpty(json)) continue;

                savedComponentStates.Add(new SavedComponentState
                {
                    objectPath = GetSceneObjectPath(behaviour.transform),
                    componentType = behaviour.GetType().AssemblyQualifiedName,
                    json = json
                });
            }
        }

        hasSavedArcadeState = true;
        if (debugArcadeState) Debug.Log($"ArcadeState: {savedComponentStates.Count} コンポーネント分を保存しました");
    }

    private IEnumerator RestoreArcadeStateNextFrame(Scene arcadeScene)
    {
        yield return null;

        if (!hasSavedArcadeState) yield break;

        MoneyManager.currentMoney = savedMoney;
        RequestManager.RequestCompleted = savedRequestCompleted;
        RequestBoard.playRequestSound = savedRequestBoardPlaySound;

        for (int i = 0; i < savedComponentStates.Count; i++)
        {
            SavedComponentState state = savedComponentStates[i];
            if (string.IsNullOrEmpty(state.objectPath) || string.IsNullOrEmpty(state.componentType)) continue;

            Transform target = FindTransformByPath(arcadeScene, state.objectPath);
            if (target == null) continue;

            Type type = Type.GetType(state.componentType);
            if (type == null) continue;

            Component targetComponent = target.GetComponent(type);
            MonoBehaviour targetBehaviour = targetComponent as MonoBehaviour;
            if (targetBehaviour == null) continue;

            JsonUtility.FromJsonOverwrite(state.json, targetBehaviour);
        }

        // 明示保存した値を最後に適用（Json復元が外れた場合でも保持させる）
        DayAdvanceButton dayButtonAfter = FindAnyObjectByType<DayAdvanceButton>();
        if (dayButtonAfter != null)
        {
            dayButtonAfter.SetDay(savedDay);
        }

        GameClockText clockAfter = FindAnyObjectByType<GameClockText>();
        if (clockAfter != null)
        {
            clockAfter.SetCompleteMoneyThreshold(savedCompleteMoneyThreshold);
        }

        // 仕様: arcade に入るたび border だけは初期値へ戻す（他の変数は保存状態を維持）
        // FadeManager 経由でも activeSceneChanged が発火するため、ここで確定的に適用する。
        if (clockAfter != null)
        {
            clockAfter.ResetBorderToDefault();
        }

        if (debugArcadeState) Debug.Log("ArcadeState: 保存済み状態を復元しました");
    }

    private void ResetArcadeRuntimeState()
    {
        OwnedProgressManager.ResetAll();
    }

    private void ClearSavedArcadeState()
    {
        hasSavedArcadeState = false;
        savedMoney = 0;
        savedRequestCompleted = 0;
        savedDay = 1;
        savedCompleteMoneyThreshold = 0;
        savedRequestBoardPlaySound = true;
        savedComponentStates.Clear();
    }

    private string GetSceneObjectPath(Transform target)
    {
        if (target == null) return string.Empty;

        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    private Transform FindTransformByPath(Scene scene, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        string[] parts = path.Split('/');
        if (parts.Length == 0) return null;

        GameObject[] roots = scene.GetRootGameObjects();
        Transform current = null;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == parts[0])
            {
                current = roots[i].transform;
                break;
            }
        }
        if (current == null) return null;

        for (int i = 1; i < parts.Length; i++)
        {
            Transform next = current.Find(parts[i]);
            if (next == null) return null;
            current = next;
        }
        return current;

    }
    void Start()
    {
        // 点滅UIの初期化
        //InitializeBlinkUI();
    }
    
    void Update()
    {
        // EscキーでUIを非表示
        if (isUIVisible && Input.GetKeyDown(hideKey))
        {
            HideUI();
        }
        
        // 点滅の制御
    //    UpdateBlinking();
    }
 /*   
    /// <summary>
    /// 点滅UIの初期化
    /// </summary>
    void InitializeBlinkUI()
    {
        if (blinkUI != null)
        {
            // CanvasGroupを取得または追加
            blinkCanvasGroup = blinkUI.GetComponent<CanvasGroup>();
            if (blinkCanvasGroup == null)
            {
                blinkCanvasGroup = blinkUI.AddComponent<CanvasGroup>();
            }
            
            // 初期状態は非表示
            blinkUI.SetActive(false);
        }
    }
    
    /// <summary>
    /// 点滅の制御
    /// </summary>
    void UpdateBlinking()
    {
        if (blinkUI == null) return;
        
        // isUIVisibleがtrueの時は点滅、falseの時は非表示
        if (isUIVisible && !isBlinking)
        {
            StartBlinking();
        }
        else if (!isUIVisible && isBlinking)
        {
            StopBlinking();
        }
    }
    
    /// <summary>
    /// 点滅を開始
    /// </summary>
    void StartBlinking()
    {
        if (blinkUI == null || blinkCanvasGroup == null) return;
        
        isBlinking = true;
        blinkUI.SetActive(true);
        
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        
        blinkCoroutine = StartCoroutine(BlinkCoroutine());
    }
    
    /// <summary>
    /// 点滅を停止
    /// </summary>
    void StopBlinking()
    {
        isBlinking = false;
        
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        if (blinkUI != null)
        {
            blinkUI.SetActive(false);
        }
    }
    
    /// <summary>
    /// 点滅コルーチン
    /// </summary>
    IEnumerator BlinkCoroutine()
    {
        while (isBlinking)
        {
            // フェードイン
            float elapsedTime = 0f;
            while (elapsedTime < blinkSpeed / 2f && isBlinking)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, elapsedTime / (blinkSpeed / 2f));
                blinkCanvasGroup.alpha = alpha;
                yield return null;
            }
            
            if (!isBlinking) break;
            
            // フェードアウト
            elapsedTime = 0f;
            while (elapsedTime < blinkSpeed / 2f && isBlinking)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(maxAlpha, minAlpha, elapsedTime / (blinkSpeed / 2f));
                blinkCanvasGroup.alpha = alpha;
                yield return null;
            }
        }
    } */
    
    public void change_button()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.deliverySound);
        }

        // title → arcade の「次のゲーム開始」だけは初期化する
        // （それ以外の arcade 遷移では、変数を保持したままにする）
        if (SceneManager.GetActiveScene().name == "title")
        {
            MoneyManager.currentMoney = 0;
            RequestManager.RequestCompleted = 0;
            OwnedProgressManager.ResetAll();
            DayAdvanceButton.ResetPersistentState();
            GameClockText.ResetPersistentState();
        }

        FadeManager.Instance.LoadSceneWithFade("arcade");
    }
    
    /// <summary>
    /// 特定のUIを表示し、ボタンクリック音を再生
    /// </summary>
    public void ShowUI()
    {
        // ボタンクリック音を再生
        if (playButtonSound && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.buttonClickSound);
        }
        
        // UIを表示
        if (targetUI != null)
        {
            targetUI.SetActive(true);
            isUIVisible = true;
        }
        else
        {
            Debug.LogWarning("ChangeScene: 表示するUIが設定されていません");
        }
    }
    
    /// <summary>
    /// 特定のUIを非表示
    /// </summary>
    public void HideUI()
    {
        if (targetUI != null)
        {
            targetUI.SetActive(false);
            isUIVisible = false;
        }
    }
 /*   
    /// <summary>
    /// 点滅を手動で開始
    /// </summary>
    public void StartBlinkingManual()
    {
        if (isUIVisible)
        {
            StartBlinking();
        }
    }
    
    /// <summary>
    /// 点滅を手動で停止
    /// </summary>
    public void StopBlinkingManual()
    {
        StopBlinking();
    }
    
    /// <summary>
    /// 点滅の設定を変更
    /// </summary>
    public void SetBlinkSettings(float speed, float min, float max)
    {
        blinkSpeed = Mathf.Max(0.1f, speed);
        minAlpha = Mathf.Clamp01(min);
        maxAlpha = Mathf.Clamp01(max);
    }*/
    
    /// <summary>
    /// UIの表示状態を切り替え
    /// </summary>
    public void ToggleUI()
    {
        if (isUIVisible)
        {
            HideUI();
        }
        else
        {
            ShowUI();
        }
    }
    
    /// <summary>
    /// 現在UIが表示されているかどうか
    /// </summary>
    public bool IsUIVisible()
    {
        return isUIVisible;
    }
}
