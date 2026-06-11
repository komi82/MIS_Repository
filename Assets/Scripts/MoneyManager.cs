using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 所持金の増減とUI反映を管理する。
/// `RequestManager` の報酬付与や `ScoreDisplay` の最終集計で参照される。
/// </summary>
public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [SerializeField] public static int currentMoney = 10000;
    [SerializeField] private TextMeshProUGUI moneyText;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        // シーン遷移で初期化しない仕様
        UpdateUI();
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateUI();
    }

    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            UpdateUI();
            return true;
        }

        Debug.LogWarning("お金が足りません");
        return false;
    }

    private void UpdateUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"{currentMoney:N0}G";
        }
    }

    public int GetMoney() => currentMoney;
}


