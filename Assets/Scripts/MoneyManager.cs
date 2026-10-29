using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 所持金の増減とUI反映を管理する。
/// `RequestManager` の報酬付与や `ScoreDisplay` の最終集計で参照される。
/// </summary>
public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [SerializeField] public static int currentMoney = 0;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI gainMoneyText;
    private int displayedMoney;

    private void OnEnable()
    {
        // Only active balance managers configure their labels; legacy disabled components
        // also remain on the goal/day objects in older scenes.
        bool gameplay = gameObject.scene.name == SceneNames.Arcade ||
            gameObject.scene.name.StartsWith("tutorial", System.StringComparison.OrdinalIgnoreCase);
        if (gameplay)
        {
            NumericTextLayout.Hud(moneyText, 1);
            NumericTextLayout.Hud(gainMoneyText, 2);
        }
        else NumericTextLayout.Fit(moneyText, 42f);
        UpdateUI();
    }

    private void LateUpdate()
    {
        // Multiple panels have their own MoneyManager but share one balance.
        // Refresh each visible label even when another instance changes the balance.
        if (displayedMoney != currentMoney) UpdateUI();
    }

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
        if (gainMoneyText != null)
        {
            gainMoneyText.gameObject.SetActive(false);
        }
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateUI();
        StartCoroutine(ShowGainMoney(amount));
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
            displayedMoney = currentMoney;
        }
    }

    private IEnumerator ShowGainMoney(int amount)
    {
        if (gainMoneyText == null)
        {
            yield break;
        }

        gainMoneyText.text = $"+{amount:N0}G";
        gainMoneyText.gameObject.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        gainMoneyText.gameObject.SetActive(false);
    }

    public int GetMoney() => currentMoney;

    public void ResetMoney()
    {
        currentMoney = 0;
        UpdateUI();
    }
}

