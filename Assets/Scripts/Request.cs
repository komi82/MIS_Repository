using UnityEngine;

[CreateAssetMenu(menuName = "Game/Request")]
public class Request : ScriptableObject
{
    public string requestName;
    public ItemData requiredItem;
    public int rewardAmount;
    public bool isCompleted;
}