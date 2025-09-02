using UnityEngine;
using UnityEngine.UI;

public class ItemPickup : MonoBehaviour
{
    [Header("�A�C�e���擾�ݒ�")]
    [SerializeField] private float pickupRange = 10f;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Camera mainCamera;


    [Header("UI�ݒ�")]
    [SerializeField] private GameObject pickupPromptUI; // �\���pUI�i��FText�t����Panel�j
    [SerializeField] private Text pickupPromptText;     // �A�C�e�����\���pText

    private ItemBehaviour currentTargetItem;

    void Update()
    {
        DetectItemInView(); // ���t���[�����C�L���X�g

        if (Input.GetKeyDown(KeyCode.F))
        {
            TryPickupItem();
        }
    }

    void DetectItemInView()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            ItemBehaviour item = hit.collider.GetComponent<ItemBehaviour>();
            if (item != null)
            {
                currentTargetItem = item;

                // UI�\���ƃe�L�X�g�X�V
                pickupPromptUI.SetActive(true);
                pickupPromptText.text = $"[F] �E���F{item.ItemData.itemName}";
                return;
            }
        }

        // �Ώۂ��Ȃ��ꍇ�͔�\��
        currentTargetItem = null;
        pickupPromptUI.SetActive(false);
    }

    void TryPickupItem()
    {
        if (currentTargetItem == null) return;

        bool success = inventoryManager.AddItem(currentTargetItem.ItemData);

        if (success)
        {
            Debug.Log($"�A�C�e�� '{currentTargetItem.ItemData.itemName}' ���擾���܂���");
            Destroy(currentTargetItem.gameObject);
            pickupPromptUI.SetActive(false); // UI����\����

        }
        else
        {
            Debug.LogWarning("�C���x���g�������t�ł��B�A�C�e���͎c��܂�");
        }

        currentTargetItem = null;
    }



}





/*
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemPickup : MonoBehaviour
{
    [Header("�A�C�e���擾�ݒ�")]
    [SerializeField] private float pickupRange = 10f;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Camera mainCamera;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryPickupItem();
        }
    }

    void TryPickupItem()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            ItemBehaviour item = hit.collider.GetComponent<ItemBehaviour>();
            if (item != null)
            {
                bool success = inventoryManager.AddItem(item.ItemData);

                if (success)
                {
                    Debug.Log($"�A�C�e�� '{item.ItemData.itemName}' ���擾���܂���");
                    Destroy(item.gameObject); // �������̂ݍ폜
                }
                else
                {
                    Debug.LogWarning("�C���x���g�������t�ł��B�A�C�e���͎c��܂�");
                    // �����ł͉��������A�A�C�e���͂��̂܂܎c��
                }
            }
        }
    }
}
*/
