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

    // arcade の実行時状態スナップショット（RequestCompleted は保存しない）
    private bool hasSavedArcadeState = false;
    private int savedMoney = 0;
    private bool savedRequestBoardPlaySound = true;
    private readonly List<SavedComponentState> savedComponentStates = new List<SavedComponentState>();
    
    void Awake()
    {
        QualitySettings.vSyncCount = 1; // VSyncを無効にすることでtargetFrameRateが有効になる
        DontDestroyOnLoad(gameObject); // シーンをまたいでも破棄されないようにする

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(SoundManager.Instance.soundData.gameplayBGM);
        }

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        // arcade を離れるタイミングで、RequestCompleted 以外の状態をスナップショット化
        if (previousScene.name == arcadeSceneName)
        {
            SaveArcadeState(previousScene);
        }

        if (nextScene.name != arcadeSceneName) return;

        bool shouldReset = resetArcadeFromScenes != null && resetArcadeFromScenes.Contains(previousScene.name);
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

        // 静的状態: RequestCompleted は仕様により保存しない
        savedMoney = MoneyManager.currentMoney;
        savedRequestBoardPlaySound = RequestBoard.playRequestSound;

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

        if (debugArcadeState) Debug.Log("ArcadeState: 保存済み状態を復元しました");
    }

    private void ResetArcadeRuntimeState()
    {
        MoneyManager.currentMoney = 0;
        RequestManager.RequestCompleted = 0;
        RequestBoard.playRequestSound = true;
    }

    private void ClearSavedArcadeState()
    {
        hasSavedArcadeState = false;
        savedMoney = 0;
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
