using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲームプレイ中の入力コンポーネント無効化を共通化するユーティリティ。
/// </summary>
public static class GameplayInputUtility
{
    public static void DisableBehaviour(Behaviour behaviour, List<Behaviour> trackList = null)
    {
        if (behaviour == null || !behaviour.enabled)
        {
            return;
        }

        behaviour.enabled = false;
        trackList?.Add(behaviour);
    }

    public static void DisableStandardInput(
        FirstPersonController playerController = null,
        DeliveryStation deliveryStation = null,
        List<Behaviour> trackList = null)
    {
        DisableBehaviour(playerController, trackList);
        DisableBehaviour(Object.FindFirstObjectByType<ItemPickup>(), trackList);
        DisableBehaviour(Object.FindFirstObjectByType<SlotSelector>(), trackList);
        DisableBehaviour(Object.FindFirstObjectByType<PutItem>(), trackList);
        DisableBehaviour(deliveryStation, trackList);
        DisableBehaviour(Object.FindFirstObjectByType<RecipeStation>(), trackList);
    }
}
