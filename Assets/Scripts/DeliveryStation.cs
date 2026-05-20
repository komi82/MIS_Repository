using UnityEngine;

/// <summary>
/// デリバー端末とのインタラクトを管理する。
/// `DeliveryUIList` の更新やカーソル状態切替を通じて、納品UIの開閉を制御する。
/// </summary>
public class DeliveryStation : MonoBehaviour
{
    [SerializeField] private GameObject deliveryUI; // デリバーUIパネル
    [SerializeField] private float detectRange = 7f; // 検知範囲
    [SerializeField] private Camera mainCamera;      // プレイヤーのカメラ
    [SerializeField] private DeliveryUIList deliveryUiList;
    public bool CursorActive = false;


    void Start()
    {
        deliveryUI.SetActive(false);
    }

    void Update()
    {
        // EscキーでUIを強制的に閉じる
        if (Input.GetKeyDown(KeyCode.Escape) && deliveryUI.activeSelf)
        {
            deliveryUI.SetActive(false);
            CursorActive = false;
            Cursor.lockState = CursorLockMode.Locked;   // 画面中央に固定＆非表示
            Cursor.visible = false; // カーソルを明示的に非表示
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
                 // 基本的な効果音再生
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.soundData.buttonClickSound);
                }
                // UIが非表示の場合は必ず表示にする
                if (!deliveryUI.activeSelf)
                {
                    
                    deliveryUI.SetActive(true);
                    deliveryUiList.RefreshList();
                    CursorActive = true;
                    Cursor.lockState = CursorLockMode.Confined; // ゲームウィンドウ内に制限
                    Cursor.visible = true; // カーソルを明示的に表示
                }
                else
                {
                    // UIが表示されている場合は非表示にする
                    deliveryUI.SetActive(false);
                    CursorActive = false;
                    Cursor.lockState = CursorLockMode.Locked;   // 画面中央に固定＆非表示
                    Cursor.visible = false; // カーソルを明示的に非表示
                }
                return;
            }
        }

        // タグがない場合
  //      CursorActive = false;
  //      Cursor.lockState = CursorLockMode.Locked;   // 画面中央に固定＆非表示


        deliveryUI.SetActive(false);
    }
}

