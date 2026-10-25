using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-9000)]
public sealed class DailyChallengeRuntime : MonoBehaviour
{
    static DailyChallengeSettings settings;
    static readonly HashSet<int> disabledArtifacts = new HashSet<int>();
    static int selectedDay;
    static int layoutSeed;
    static DailyChallenge selected;
    static DailyChallengeRuntime instance;
    float elapsed, nextDelivery, noticeUntil;
    string notice;
    TextMeshProUGUI label;
    int penalizedCycle = -1;
    public static DailyChallengeSettings Settings => settings != null ? settings :
        (settings = Resources.Load<DailyChallengeSettings>("DailyChallengeSettings") ?? ScriptableObject.CreateInstance<DailyChallengeSettings>());
    public static DailyChallenge Current => FestivalUpgradeRuntime.InArcade ? selected : DailyChallenge.None;
    public static float Elapsed => instance != null ? instance.elapsed : 0f;
    public static bool Is(DailyChallenge challenge) => Current == challenge;
    public static bool IsArtifactDisabled(int id) => Is(DailyChallenge.DisabledArtifacts) && disabledArtifacts.Contains(id);
    public static float Speed => Is(DailyChallenge.Turbo) ? Settings.turboMultiplier : 1f;
    public static float QteDifficulty => Is(DailyChallenge.HarderQte) ? Settings.qteDifficultyMultiplier : 1f;
    public static bool RecipeBlocked => Is(DailyChallenge.NoRecipes);
    public static float DeliveryWait => Is(DailyChallenge.DeliveryCooldown) && instance != null ? Mathf.Max(0, instance.nextDelivery - instance.elapsed) : 0f;
    public static int BanCategory => Is(DailyChallenge.RotatingWorkBan) ? Mathf.FloorToInt(Elapsed / Settings.workBanInterval) % 3 : -1;
    public static bool WorkBlocked(string tag) => BanCategory == (tag == "craft" ? 0 : tag == "wash" ? 1 : tag == "blacksmith" ? 2 : -2);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Initialize()
    {
        ResetRun(); settings = null;
        SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded;
    }
    public static void ResetRun() { selectedDay = 0; selected = DailyChallenge.None; disabledArtifacts.Clear(); }
    static void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneNames.Arcade) return;
        BeginDay(DayAdvanceButton.GetDayStatic());
        if (UnityEngine.Object.FindFirstObjectByType<DailyChallengeRuntime>() == null)
        {
            var host = new GameObject("DailyChallenge"); SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<DailyChallengeRuntime>();
        }
    }
    public static void BeginDay(int day)
    {
        if (selectedDay == day) return;
        var previous = selected; selectedDay = day; disabledArtifacts.Clear();
        if (day <= 1) { selected = DailyChallenge.None; return; }
        var owned = OwnedProgressManager.GetOwnedArtifactIds();
        var candidates = Settings.enabledChallenges.Where(x => x != DailyChallenge.None
            && (x != DailyChallenge.DisabledArtifacts || owned.Count > 0)).Distinct().ToList();
        if (candidates.Count > 1) candidates.Remove(previous);
        selected = candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : DailyChallenge.None;
        layoutSeed = UnityEngine.Random.Range(1, int.MaxValue);
        if (selected == DailyChallenge.DisabledArtifacts)
            while (owned.Count > 0 && disabledArtifacts.Count < Settings.disabledArtifactCount)
            { int i = UnityEngine.Random.Range(0, owned.Count); disabledArtifacts.Add(owned[i]); owned.RemoveAt(i); }
    }
    void Awake() { instance = this; }
    void OnDestroy() { if (instance == this) instance = null; }
    void Start()
    {
        if (Is(DailyChallenge.ShuffledStations)) DailyChallengeLayout.Apply(layoutSeed);
        var template = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(x => x.canvas != null && x.font != null);
        if (template == null) return;
        var panel = new GameObject("DailyChallengeBanner", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(template.canvas.rootCanvas.transform, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = new Vector2(.27f, .77f); rect.anchorMax = new Vector2(.73f, .88f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(.03f, .035f, .06f, .88f);
        panel.GetComponent<Image>().raycastTarget = false;
        var text = new GameObject("Text", typeof(RectTransform)); text.transform.SetParent(panel.transform, false);
        label = text.AddComponent<TextMeshProUGUI>(); label.font = template.font; label.fontSharedMaterial = template.fontSharedMaterial;
        label.fontSize = 26; label.color = new Color(1f,.9f,.62f); label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false; label.enableAutoSizing = true; label.fontSizeMin = 19; label.fontSizeMax = 26;
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(12,8); label.rectTransform.offsetMax = new Vector2(-12,-8);
    }
    void Update()
    {
        if (GameClockText.Instance != null && GameClockText.Instance.RoundEnded) return;
        elapsed += Time.deltaTime;
        if (label != null)
        {
            string message = selected == DailyChallenge.None ? "1日目：苦難なし" : "今日の苦難：" + Description();
            if (KouaArtifactRuntime.Has(KouaArtifactRuntime.MemoryBook)) message += "\nM：どこでもレシピ";
            if (noticeUntil > elapsed) message += "\n" + notice;
            label.text = message;
        }
    }
    public static void Notify(string text)
    { if (instance != null) { instance.notice = text; instance.noticeUntil = instance.elapsed + 3f; } }
    public static void Delivered() { if (instance != null) instance.nextDelivery = instance.elapsed + Settings.deliveryCooldown; }
    public static int Goal(int amount) => Is(DailyChallenge.HigherGoal) ? Mathf.CeilToInt(amount * Settings.goalMultiplier) : amount;
    public static float Duration(float amount) => Mathf.Max(15f, amount - (Is(DailyChallenge.ShorterDay) ? Settings.timeReduction : 0)
        - (KouaArtifactRuntime.Has(KouaArtifactRuntime.TimeIsMoney) ? 30f : 0));
    public static float Weight(RequestType type)
    {
        int category = FestivalUpgradeRules.WorkCategory(type);
        return (Is(DailyChallenge.FewerMixing) && category == 0) || (Is(DailyChallenge.FewerPurification) && category == 1)
            || (Is(DailyChallenge.FewerBlacksmith) && category == 2) ? Settings.requestWeightMultiplier : 1f;
    }
    public static int Reward(int amount, Request request)
    {
        var c = KouaArtifactRuntime.Catalog;
        var group = c == null ? null : Is(DailyChallenge.OrePenalty) ? c.ores : Is(DailyChallenge.PlantPenalty) ? c.plants
            : Is(DailyChallenge.MushroomPenalty) ? c.mushrooms : null;
        return group != null && KouaArtifactRuntime.UsesMaterial(request, group) ? Mathf.RoundToInt(amount * Settings.materialRewardMultiplier) : amount;
    }
    public static int Output(int normal, float roll) => Mathf.Max(0, normal - (Is(DailyChallenge.LostOutput) && roll < Settings.lostOutputChance ? 1 : 0));
    public static void ObserveMovement(Vector3 displacement)
    {
        if (instance == null || !Is(DailyChallenge.RedLightGreenLight) || Time.timeScale <= 0) return;
        float cycle = Mathf.Max(Settings.redLightDuration + 2f, Settings.redLightCycle);
        int round = Mathf.FloorToInt(Elapsed / cycle);
        bool stopped = Elapsed % cycle >= cycle - Settings.redLightDuration;
        displacement.y = 0;
        if (stopped && displacement.sqrMagnitude > .0001f && instance.penalizedCycle != round)
        {
            instance.penalizedCycle = round;
            GameClockText.Instance?.ReduceTime(Settings.redLightPenaltySeconds);
            Notify("動いた！ 残り時間 −" + Settings.redLightPenaltySeconds + "秒");
        }
    }
    public static string Description()
    {
        switch (Current)
        {
            case DailyChallenge.HigherGoal: return $"ノルマ {Settings.goalMultiplier:0.##}倍";
            case DailyChallenge.ShorterDay: return $"営業時間 −{Settings.timeReduction:0}秒";
            case DailyChallenge.FewerPurification: return "浄化依頼が来にくい";
            case DailyChallenge.FewerMixing: return "調合依頼が来にくい";
            case DailyChallenge.FewerBlacksmith: return "鍛冶依頼が来にくい";
            case DailyChallenge.OrePenalty: return "鉱石を使う依頼の報酬減少";
            case DailyChallenge.PlantPenalty: return "植物を使う依頼の報酬減少";
            case DailyChallenge.MushroomPenalty: return "きのこを使う依頼の報酬減少";
            case DailyChallenge.DisabledArtifacts: return "今日だけ無効\n" + string.Join("・", disabledArtifacts.Select(KouaArtifactRuntime.DisplayName));
            case DailyChallenge.HarderQte: return "作業QTEが難しくなる";
            case DailyChallenge.NoRecipes: return "レシピを見られない";
            case DailyChallenge.DeliveryCooldown: return $"納品の間隔 {Settings.deliveryCooldown:0}秒" + (DeliveryWait > 0 ? $"（あと{DeliveryWait:0.0}秒）" : "");
            case DailyChallenge.ExpiringRequests: return $"依頼は{Settings.requestLifetime:0}秒でキャンセル";
            case DailyChallenge.LostOutput: return $"{Settings.lostOutputChance * 100:0}%で完成数 −1";
            case DailyChallenge.SlipperyMovement: return "足元が滑る";
            case DailyChallenge.ShuffledStations: return "素材と作業台の配置が変わる";
            case DailyChallenge.NoRightTurn: return "カメラを右に回せない";
            case DailyChallenge.RedLightGreenLight:
                float cycle = Mathf.Max(Settings.redLightDuration + 2f, Settings.redLightCycle);
                float left = cycle - Elapsed % cycle;
                return left <= Settings.redLightDuration ? "だるまさんがころんだ！ 止まって！" : left <= Settings.redLightDuration + 2 ? "もうすぐ止まって！" : "だるまさんがころんだ：移動OK";
            case DailyChallenge.Turbo: return $"時間・作業・移動 {Settings.turboMultiplier:0.#}倍速";
            case DailyChallenge.RotatingWorkBan: return new[] { "調合", "浄化", "鍛冶" }[BanCategory] + $"禁止（あと{Settings.workBanInterval - Elapsed % Settings.workBanInterval:0}秒）";
            default: return "なし";
        }
    }
}
