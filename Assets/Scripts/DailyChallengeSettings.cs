using UnityEngine;

public enum DailyChallenge
{
    None, HigherGoal, ShorterDay, FewerPurification, FewerMixing, FewerBlacksmith,
    OrePenalty, PlantPenalty, MushroomPenalty, DisabledArtifacts, HarderQte,
    NoRecipes, DeliveryCooldown, ExpiringRequests, LostOutput, SlipperyMovement,
    ShuffledStations, NoRightTurn, RedLightGreenLight, Turbo, RotatingWorkBan
}

[CreateAssetMenu(menuName = "Game/Daily Challenge Settings")]
public sealed class DailyChallengeSettings : ScriptableObject
{
    [Min(1f)] public float goalMultiplier = 1.25f;
    [Min(0f)] public float timeReduction = 20f;
    [Range(.05f, 1f)] public float requestWeightMultiplier = .35f;
    [Range(0f, 1f)] public float materialRewardMultiplier = .7f;
    [Min(0)] public int disabledArtifactCount = 3;
    [Min(1f)] public float qteDifficultyMultiplier = 1.5f;
    [Min(.1f)] public float deliveryCooldown = 5f;
    [Min(5f)] public float requestLifetime = 35f;
    [Range(0f, 1f)] public float lostOutputChance = .1f;
    [Min(.1f)] public float slipperyAcceleration = 2.5f;
    [Min(3f)] public float redLightCycle = 12f;
    [Min(1f)] public float redLightDuration = 2f;
    [Min(0f)] public float redLightPenaltySeconds = 5f;
    [Min(1f)] public float turboMultiplier = 2f;
    [Min(5f)] public float workBanInterval = 20f;
    public DailyChallenge[] enabledChallenges = new[] {
        DailyChallenge.HigherGoal, DailyChallenge.ShorterDay, DailyChallenge.FewerPurification,
        DailyChallenge.FewerMixing, DailyChallenge.FewerBlacksmith, DailyChallenge.OrePenalty,
        DailyChallenge.PlantPenalty, DailyChallenge.MushroomPenalty, DailyChallenge.DisabledArtifacts,
        DailyChallenge.HarderQte, DailyChallenge.NoRecipes, DailyChallenge.DeliveryCooldown,
        DailyChallenge.ExpiringRequests, DailyChallenge.LostOutput, DailyChallenge.SlipperyMovement,
        DailyChallenge.ShuffledStations, DailyChallenge.NoRightTurn, DailyChallenge.RedLightGreenLight, DailyChallenge.Turbo,
        DailyChallenge.RotatingWorkBan
    };
}
