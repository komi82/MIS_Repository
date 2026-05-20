using UnityEngine;

[CreateAssetMenu(menuName = "Game/Request")]
public class Request : ScriptableObject
{
    public string requestName;
    public RequestType requestType;
    public ItemData requiredItem;
    public ItemData providedItem;
    public int rewardAmount;
    public bool isCompleted;
}