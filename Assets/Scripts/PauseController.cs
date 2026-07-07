using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームを一時停止/再開するためのコンポーネント。
/// - インスペクタでトグルキー（KeyCode）を指定
/// - pauseUIObjects に登録した UI オブジェクトをポーズ中に表示する
/// - resumeButton を指定するとボタン押下で解除
/// - 時間を止めるために Time.timeScale を 0/1 に切り替える
/// </summary>
public class PauseController : MonoBehaviour
{
    public static PauseController Instance { get; private set; }

    [Header("入力")]
    [Tooltip("ポーズ/再開を切り替えるキー（インスペクタで指定）")]
    public KeyCode toggleKey = KeyCode.Escape;

    [Header("UI")]
    [Tooltip("ポーズ中に表示するUIオブジェクト（複数可）")]
    public List<GameObject> pauseUIObjects = new List<GameObject>();

    [Tooltip("ポーズUI内の再開ボタン（任意）。設定するとボタンで復帰できます）")]
    public Button resumeButton;

    [Tooltip("ポーズ中に自動でカーソルを表示・解放する（デフォルト: true）")]
    public bool showCursorOnPause = true;

    [Tooltip("ポーズ時に Timescale を変更するか（デフォルト: true）")]
    public bool controlTimeScale = true;

    // 内部状態
    private bool isPaused = false;
    // ポーズ時に無効化したBehaviourを記録して復帰時に再有効化する
    private System.Collections.Generic.List<UnityEngine.Behaviour> disabledBehaviours = new System.Collections.Generic.List<UnityEngine.Behaviour>();

    void Awake()
    {
        // シングルトン化して永続化。既に存在する場合は破棄
        if (Instance == null)
        {
            Instance = this;
            // 別シーンの親オブジェクトに依存しないようルート化してから永続化
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Debug.Log($"PauseController: Instance created and set DontDestroyOnLoad ({gameObject.name})");
        }
        else if (Instance != this)
        {
            Debug.Log($"PauseController: Duplicate instance destroyed ({gameObject.name})");
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        // シーン読み込み時にポーズUIを隠すために登録
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // 初期状態ではポーズUIを非表示にする
        // clear any previous scene-specific references
        pauseUIObjects.Clear();
        SetPauseUIActive(false);

        // シーン内に存在する"pause"系オブジェクトを自動検知して非表示にする（PauseControllerがシーン生成後に初期化された場合の保険）
        HidePauseUIByNameInScene(SceneManager.GetActiveScene());

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(Resume);
        }
    }

    void Update()
    {
        // 指定キーでトグル
        bool pressed = false;

        // 旧入力システム
        if (Input.GetKeyDown(toggleKey)) pressed = true;

        // 新しいInput Systemがある場合もチェック（KeyCode -> Key の名前が一致する場合）
        #if ENABLE_INPUT_SYSTEM
        try
        {
            if (!pressed && UnityEngine.InputSystem.Keyboard.current != null)
            {
                UnityEngine.InputSystem.Key keyEnum;
                if (System.Enum.TryParse<UnityEngine.InputSystem.Key>(toggleKey.ToString(), out keyEnum))
                {
                    if (UnityEngine.InputSystem.Keyboard.current[keyEnum].wasPressedThisFrame)
                    {
                        pressed = true;
                    }
                }
            }
        }
        catch { /* ignore parsing issues */ }
        #endif

        if (pressed)
        {
            Debug.Log($"PauseController: toggle key pressed ({toggleKey})");
            TogglePause();
        }
    }

    void OnDisable()
    {
        // シーン読み込み登録解除
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // コンポーネントが無効化されたときにTimeScaleが0のままにならないように復帰
        if (controlTimeScale && Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
        // 無効化したBehaviourが残らないように復帰
        RestoreDisabledBehaviours();
    }

    /// <summary>
    /// ポーズ/再開をトグル
    /// </summary>
    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    /// <summary>
    /// ポーズを開始
    /// </summary>
    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        Debug.Log("PauseController: Pausing");

        // UI表示
        SetPauseUIActive(true);

        // 視点移動などの入力コンポーネントを無効化（FirstPersonController を参考に）
        // 既にある DeliveryStation のカーソル制御と同等の振る舞いで入力を停止する
        TryDisablePlayerInput();

        // 時間停止
        if (controlTimeScale)
        {
            Time.timeScale = 0f;
        }

        // カーソルを表示/解放
        if (showCursorOnPause)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// ポーズを解除してゲームを再開
    /// </summary>
    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        Debug.Log("PauseController: Resuming");

        // UI非表示
        SetPauseUIActive(false);

        // 時間再開
        if (controlTimeScale)
        {
            Time.timeScale = 1f;
        }

        // カーソルの処理はゲーム側の設計に依る。
        // ゲームプレイ（FirstPersonController が存在）時のみカーソルを非表示にする。
        if (showCursorOnPause)
        {
            var fpcCheck = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
            if (fpcCheck != null)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                // メニュー/タイトル等ではカーソルを表示・解放しておく
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        // 無効化した入力コンポーネントを復帰
        RestoreDisabledBehaviours();
    }

    /// <summary>
    /// pauseUIObjects の表示状態をまとめて切り替え
    /// </summary>
    private void SetPauseUIActive(bool active)
    {
        foreach (var obj in pauseUIObjects)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }

    /// <summary>
    /// プレイヤー入力（視点移動など）を無効化して記録する
    /// </summary>
    private void TryDisablePlayerInput()
    {
        disabledBehaviours.Clear();

        // FirstPersonController を探して無効化
        var fpc = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
        if (fpc != null)
        {
            GameplayInputUtility.DisableBehaviour(fpc, disabledBehaviours);
        }

        // また一般的なインタラクション系 Behaviour も無効化しておく
        GameplayInputUtility.DisableStandardInput(fpc, null, disabledBehaviours);
    }

    /// <summary>
    /// 無効化した Behaviour を元に戻す
    /// </summary>
    private void RestoreDisabledBehaviours()
    {
        foreach (var b in disabledBehaviours)
        {
            if (b != null)
            {
                try { b.enabled = true; } catch { }
            }
        }
        disabledBehaviours.Clear();
    }


    void OnDestroy()
    {
        // シーン読み込み登録解除（保険）
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(Resume);
        }

        // 保険としてTimeScaleを戻す
        if (controlTimeScale && Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }

        // 保険として無効化したBehaviourを復帰
        RestoreDisabledBehaviours();
    }

    /// <summary>
    /// シーン読み込み時のハンドラ（シーン入場時にポーズUIを確実に非表示にする）
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 新シーン入場時はグローバルにポーズ状態を解除（UIが残る問題を防ぐ）
        ForceUnpauseAll();

        // clear any scene-specific references
        pauseUIObjects.Clear();

        // さらにシーン内のオブジェクトで名前に "pause" を含むUIを検索して非表示にする
        HidePauseUIByNameInScene(scene);
    }

    /// <summary>
    /// シーン内で名前に "pause" を含むオブジェクトを探して非表示にし、pauseUIObjects に追加しておく
    /// </summary>
    private void HidePauseUIByNameInScene(Scene scene)
    {
        try
        {
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    if (t == null || t.gameObject == null) continue;
                    string name = t.gameObject.name.ToLowerInvariant();
                    if (name.Contains("pause") || name.Contains("pauseui") || name.Contains("pause_panel") || name.Contains("pausepanel"))
                    {
                        t.gameObject.SetActive(false);
                        if (!pauseUIObjects.Contains(t.gameObject))
                        {
                            pauseUIObjects.Add(t.gameObject);
                        }
                        Debug.Log($"PauseController: Hid scene pause UI '{t.gameObject.name}'");
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"PauseController: HidePauseUIByNameInScene failed: {ex.Message}");
        }
    }

    /// <summary>
    /// インスタンスのポーズ関連状態をリセットしてUIを非表示にする
    /// </summary>
    public void ResetPauseState()
    {
        SetPauseUIActive(false);
        isPaused = false;

        if (controlTimeScale && Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }

        RestoreDisabledBehaviours();

        if (showCursorOnPause)
        {
            var fpcCheck = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
            if (fpcCheck != null)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }

    /// <summary>
    /// 全ての PauseController インスタンスを強制的にアンパーズする（静的ユーティリティ）
    /// </summary>
    public static void ForceUnpauseAll()
    {
        // TimeScale をデフォルト状態に
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
        // カーソル表示はゲームプレイかメニューかで切り替える（FirstPersonController の存在で判定）
        var fpcCheck = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
        if (fpcCheck != null)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // 全インスタンスに対してResetPauseStateを呼ぶ
        var pcs = UnityEngine.Object.FindObjectsOfType<PauseController>();
        if (pcs != null)
        {
            foreach (var pc in pcs)
            {
                if (pc != null)
                {
                    pc.ResetPauseState();
                }
            }
        }

        // 保険: 代表的な入力系コンポーネントが無効化されたままになっていないかチェックして強制的に有効化
        var fpc = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
        if (fpc != null) fpc.enabled = true;

        var itemPickups = UnityEngine.Object.FindObjectsOfType<ItemPickup>();
        foreach (var ip in itemPickups) { if (ip != null) ip.enabled = true; }

        var slotSelectors = UnityEngine.Object.FindObjectsOfType<SlotSelector>();
        foreach (var ss in slotSelectors) { if (ss != null) ss.enabled = true; }

        var putItems = UnityEngine.Object.FindObjectsOfType<PutItem>();
        foreach (var pi in putItems) { if (pi != null) pi.enabled = true; }

        var recipeStations = UnityEngine.Object.FindObjectsOfType<RecipeStation>();
        foreach (var rs in recipeStations) { if (rs != null) rs.enabled = true; }

        var deliveryStations = UnityEngine.Object.FindObjectsOfType<DeliveryStation>();
        foreach (var ds in deliveryStations) { if (ds != null) ds.CursorActive = false; }
    }
}

