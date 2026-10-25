using UnityEngine;
using UnityEngine.InputSystem;
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
    public bool IsUIOpen => recipeUI != null && recipeUI.activeSelf;

    void Start()
    {
        // Early tutorial scenes deliberately have no recipe panel yet.
        if (recipeUI == null) { enabled = false; return; }
        if (mainCamera == null) mainCamera = Camera.main;
        InteractionControlTrial.ConfigureCloseHint(recipeUI);
        recipeUI.SetActive(false);
    }

    void Update()
    {
        if (recipeUI == null) return;
        // マウス試作中も表示どおりFで閉じられる。Escも共通で使用可能。
        if (recipeUI.activeSelf && InteractionControlTrial.ClosePanelPressed)
        {
            CloseRecipeUI();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame && KouaArtifactRuntime.Has(KouaArtifactRuntime.MemoryBook))
        {
            if (recipeUI.activeSelf) CloseRecipeUI();
            else if (!InteractionControlTrial.WorldInputBlocked) OpenRecipeUI();
            return;
        }
        if (InteractionControlTrial.InteractPressed)
        {
            DetectStationInView();
        }
    }

    void DetectStationInView()
    {
        if (recipeUI == null || mainCamera == null) return;
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, detectRange))
        {
            if (hit.collider.gameObject == this.gameObject)
            {
                InteractionControlTrial.Consume();
                // UIが非表示の場合は必ず表示にする
                if (!recipeUI.activeSelf)
                {
                    OpenRecipeUI();
                }
                else
                {
                    CloseRecipeUI();
                }
                return;
            }
        }

        // レイキャストが当たらない場合はUIを非表示にしない
        // （インタラクト入力で開き、Escまたは閉じるボタンで閉じる）
    }

    void OpenRecipeUI()
    {
        if (DailyChallengeRuntime.RecipeBlocked) { DailyChallengeRuntime.Notify("今日の苦難でレシピを開けません"); return; }
        if (recipeUI == null) return;
        InteractionControlTrial.Consume();
        recipeUI.SetActive(true); CursorActive = true;
        if (deliveryStation != null) deliveryStation.CursorActive = true;
        Cursor.lockState = CursorLockMode.Confined; Cursor.visible = true;
        if (playerController != null) playerController.enabled = false;
    }

    void CloseRecipeUI()
    {
        if (recipeUI == null) return;
        InteractionControlTrial.BlockCurrentFrame();
        recipeUI.SetActive(false);
        CursorActive = false;
        if (deliveryStation != null)
        {
            deliveryStation.CursorActive = false;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerController != null) playerController.enabled = true;

        if (SceneManager.GetActiveScene().name == SceneNames.Tutorial7)
        {
            ConditionalSceneTransition.TriggerTransitionStatic();
        }
    }
}
