using UnityEngine;

public class DeliveryStation : MonoBehaviour
{
    [SerializeField] private GameObject deliveryUI; // 納品UIパネル
    [SerializeField] private float detectRange = 3f; // 検知距離
    [SerializeField] private Camera mainCamera;      // プレイヤーのカメラ
    [SerializeField] private DeliveryUIList deliveryUiList;


    void Start()
    {
        deliveryUI.SetActive(false);
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.F))
        {
            DetectStationInView();
        }
    }

    void DetectStationInView()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, detectRange))
        {
            deliveryUI.SetActive(!deliveryUI.activeSelf);
            deliveryUiList.RefreshList();
            return;
        }

        // ヒットしなかった場合
        deliveryUI.SetActive(false);
    }
}