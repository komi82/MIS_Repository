using UnityEngine;
using UnityEngine.InputSystem;


public class ItemPickup : MonoBehaviour
{
    [SerializeField] private float pickupRange = 3f;
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
                inventoryManager.AddItem(item.ItemData);
                Destroy(item.gameObject); // èEÇ¡ÇΩÇÁè¡Ç∑
            }
        }
    }
}