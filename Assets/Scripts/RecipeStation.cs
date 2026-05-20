using UnityEngine;

public class RecipeStation : MonoBehaviour
{
    [SerializeField] private GameObject recipeUI; // レシピUIパネル
    [SerializeField] private float detectRange = 7f; // 検知範囲
    [SerializeField] private Camera mainCamera; // プレイヤーのカメラ
    [SerializeField] private FirstPersonController playerController; // プレイヤー制御用
    [SerializeField] private DeliveryStation deliveryStation; // DeliveryStationとCursorActiveを共有
    public bool CursorActive = false;

    void Start()
    {
        recipeUI.SetActive(false);
    }

    void Update()
    {
        // EscキーでUIを強制的に閉じる
        if (Input.GetKeyDown(KeyCode.Escape) && recipeUI.activeSelf)
        {
            recipeUI.SetActive(false);
            CursorActive = false;
            // DeliveryStationと共有する場合
            if (deliveryStation != null)
            {
                deliveryStation.CursorActive = false;
            }
            Cursor.lockState = CursorLockMode.Locked; // 画面中央に固定＆非表示
            Cursor.visible = false; // カーソルを明示的に非表示
            // プレイヤー制御を再有効化
            if (playerController != null) playerController.enabled = true;
            return;
        }

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
                // UIが非表示の場合は必ず表示にする
                if (!recipeUI.activeSelf)
                {
                    recipeUI.SetActive(true);
                    CursorActive = true;
                    // DeliveryStationと共有する場合
                    if (deliveryStation != null)
                    {
                        deliveryStation.CursorActive = true;
                    }
                    Cursor.lockState = CursorLockMode.Confined; // ゲームウィンドウ内に制限
                    Cursor.visible = true; // カーソルを明示的に表示
                    // プレイヤー制御を無効化
                    if (playerController != null) playerController.enabled = false;
                }
                else
                {
                    // UIが表示されている場合は非表示にする
                    recipeUI.SetActive(false);
                    CursorActive = false;
                    // DeliveryStationと共有する場合
                    if (deliveryStation != null)
                    {
                        deliveryStation.CursorActive = false;
                    }
                    Cursor.lockState = CursorLockMode.Locked; // 画面中央に固定＆非表示
                    Cursor.visible = false; // カーソルを明示的に非表示
                    // プレイヤー制御を再有効化
                    if (playerController != null) playerController.enabled = true;
                }
                return;
            }
        }

        // レイキャストが当たらない場合はUIを非表示にしない
        // （ユーザーがFキーを押したときにのみ切り替え）
    }
}
