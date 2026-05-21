using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// レシピ閲覧ステーションの開閉を担当するインタラクト制御。
/// `DeliveryStation` とカーソル状態を共有し、UI表示中は `FirstPersonController` を停止する。
/// </summary>
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
            CloseRecipeUI();
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
                    CloseRecipeUI();
                }
                return;
            }
        }

        // レイキャストが当たらない場合はUIを非表示にしない
        // （ユーザーがFキーを押したときにのみ切り替え）
    }

    void CloseRecipeUI()
    {
        recipeUI.SetActive(false);
        CursorActive = false;
        if (deliveryStation != null)
        {
            deliveryStation.CursorActive = false;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerController != null) playerController.enabled = true;

        if (SceneManager.GetActiveScene().name == "tutorial7")
        {
            ConditionalSceneTransition.TriggerTransitionStatic();
        }
    }
}
