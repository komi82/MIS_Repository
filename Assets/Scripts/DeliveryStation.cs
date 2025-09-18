using UnityEngine;

public class DeliveryStation : MonoBehaviour
{
    [SerializeField] private GameObject deliveryUI; // 納品UIパネル
    [SerializeField] private float detectRange = 7f; // 検知距離
    [SerializeField] private Camera mainCamera;      // プレイヤーのカメラ
    [SerializeField] private DeliveryUIList deliveryUiList;
    public bool CursorActive = false;


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
            if (hit.collider.gameObject == this.gameObject)
            {
                deliveryUI.SetActive(!deliveryUI.activeSelf);
                deliveryUiList.RefreshList();
                if (CursorActive)
                {
                    CursorActive = false;
                    Cursor.lockState = CursorLockMode.Locked;   // 画面中央に固定＆非表示

                }
                else
                {
                    CursorActive = true;
                    Cursor.lockState = CursorLockMode.Confined; // ゲームウィンドウ内に制限

                }
                return;
            }
        }

        // ヒットしなかった場合
  //      CursorActive = false;
  //      Cursor.lockState = CursorLockMode.Locked;   // 画面中央に固定＆非表示


        deliveryUI.SetActive(false);
    }
}