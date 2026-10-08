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

    private void OnEnable()
    {
        RequestManager.RequestComp += CloseIfNoRequests;
    }

    private void OnDisable()
    {
        RequestManager.RequestComp -= CloseIfNoRequests;
    }

    void Start()
    {
        deliveryUI.SetActive(false);
    }

    private bool HasPendingRequests()
    {
        if (deliveryUiList == null || deliveryUiList.requestManager == null) return false;
        return deliveryUiList.requestManager.GetActiveRequests()
            .Exists(request => request != null && !request.isCompleted);
    }

    private void CloseIfNoRequests()
    {
        // 最後の依頼を消す前に、納品UIの操作ロックも解除する。
        // 他の画面も CursorActive を使うため、納品UIが開いている時だけ閉じる。
        if (deliveryUI != null && deliveryUI.activeSelf && !HasPendingRequests())
            ForceCloseUI();
    }

    public void ForceCloseUI()
    {
        if (deliveryUI != null)
        {
            deliveryUI.SetActive(false);
        }

        CursorActive = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // EscキーでUIを強制的に閉じる
        if (Input.GetKeyDown(KeyCode.Escape) && deliveryUI.activeSelf)
        {
            ForceCloseUI();
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
                if (!deliveryUI.activeSelf)
                {
                    // 次の依頼が生成されるまでの間に、空のUIで操作を止めない。
                    if (!HasPendingRequests()) return;

                    deliveryUI.SetActive(true);
                    deliveryUiList.RefreshList();
                    CursorActive = true;
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = true;
                }
                else
                {
                    ForceCloseUI();
                }
                return;
            }
        }

        // 端末から視線を外して閉じる場合も、表示と操作ロックを一緒に戻す。
        if (deliveryUI != null && deliveryUI.activeSelf)
        {
            ForceCloseUI();
        }
    }
}
