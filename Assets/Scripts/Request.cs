using UnityEngine;

/// <summary>
/// 依頼1件分の状態を保持するScriptableObject。
/// `RequestManager` が生成・更新し、UIや納品処理が参照する。
/// </summary>
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