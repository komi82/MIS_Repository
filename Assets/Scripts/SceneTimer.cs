using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// シーン読み込み時点を基準に経過時間を提供するタイマー。
/// 主に `RequestManager` が依頼生成間隔の判定に利用する。
/// </summary>
public class SceneTimer : MonoBehaviour
{
    public static SceneTimer Instance { get; private set; }

    private float elapsed;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        if (!DayStartSequence.IsBlocking) elapsed += Time.deltaTime;
    }


    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        elapsed = 0f;
        Debug.Log($"Scene '{scene.name}' loaded. Timer started.");
        
        // シーン名に応じてカーソル状態を設定
        SetCursorStateForScene(scene.name);
    }
    
    /// <summary>
    /// シーン名に応じてカーソル状態を設定
    /// </summary>
    private void SetCursorStateForScene(string sceneName)
    {
        string lower = sceneName.ToLowerInvariant();
        if (lower == SceneNames.Result || lower == SceneNames.Title || lower == SceneNames.Shop.ToLowerInvariant())
        {

            // resultシーンとtitleシーンではカーソルを表示
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        else
        {
            // その他のシーン（arcade等）ではカーソルをロック
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public float GetElapsedTime()
    {
        return elapsed;
    }


}
