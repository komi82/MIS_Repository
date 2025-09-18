using UnityEngine;
using UnityEngine.UI;

public class MoneyManager : MonoBehaviour
{
    [SerializeField] public static int currentMoney;
    [SerializeField] private Text moneyText;

    private void Start()
    {
        currentMoney = 0;
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

        Debug.LogWarning("ŠŽ‹à‚ª‘«‚è‚Ü‚¹‚ñ");
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