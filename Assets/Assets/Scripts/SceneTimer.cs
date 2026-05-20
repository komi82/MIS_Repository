using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneTimer : MonoBehaviour
{
    public static SceneTimer Instance { get; private set; }

    private float startTime;

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
        if (Instance != null)
        {
            float elapsed = Instance.GetElapsedTime();
        }
    }


    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        startTime = Time.time;
        Debug.Log($"Scene '{scene.name}' loaded. Timer started.");
        
        // シーン名に応じてカーソル状態を設定
        SetCursorStateForScene(scene.name);
    }
    
    /// <summary>
    /// シーン名に応じてカーソル状態を設定
    /// </summary>
    private void SetCursorStateForScene(string sceneName)
    {
        if (sceneName == "result" || sceneName == "title" || sceneName == "Shop")
        {
            // resultシーンとtitleシーンとShopシーンではカーソルを表示
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
        return Time.time - startTime;
    }


}
