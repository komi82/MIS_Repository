using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneTimer : MonoBehaviour
{
    public static SceneTimer Instance { get; private set; }
    [SerializeField] private Text timerText;

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
            timerText.text = $"{elapsed:F2}";
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
    }

    public float GetElapsedTime()
    {
        return Time.time - startTime;
    }


}
