using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの一人称視点操作（視点回転・移動・重力・移動アニメ）を制御する。
/// `DeliveryStation.CursorActive` を参照して、UI操作中は視点入力を止める。
/// </summary>
public class FirstPersonController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    float mo = 0.0f;
    public float sensitivity = 2.0f;
    public Transform playerBody; // プレイヤーのオブジェクト（親）をセット

    private CharacterController characterController;
    [SerializeField] private DeliveryStation deliveryStation;

    float xRotation = 0f;
    float yRotation = 0f;
    public float gravity = -9.81f;
    private float verticalVelocity = 0f;

    private Animator Controller = null;

    void Start()
    {
        mo = moveSpeed;
        // DeliveryStationがない場合、またはカーソルがアクティブでない場合のみロック
        if (deliveryStation == null || !deliveryStation.CursorActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        characterController = GetComponent<CharacterController>(); // プレイヤーの CharacterController を取得

        Controller = GetComponent<Animator>();
    }



    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
          moveSpeed = mo * 2.0f;
}
        else
        {
          moveSpeed = mo;
        }

        if (deliveryStation.CursorActive == false)
        {
            // マウス入力（視点の回転）
            float mouseX = Mouse.current.delta.x.ReadValue() * sensitivity;
            float mouseY = Mouse.current.delta.y.ReadValue() * sensitivity;

            // カメラの回転（上下のみ）
            xRotation -= mouseY * 0.5f;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f); // 上下の可動域を制限（-90度 = 真下、90度 = 真上）
            Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            // プレイヤーの回転（左右のみ）
            yRotation += mouseX * 0.5f;
            // yRotation = Mathf.Clamp(yRotation, -180f, 180f); // 左右の可動域を制限したい場合はこの行のコメントを外す
            playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);
        
        // WASD移動
        float horizontal = Keyboard.current.aKey.isPressed ? -1f : Keyboard.current.dKey.isPressed ? 1f : 0f;
        float vertical = Keyboard.current.wKey.isPressed ? 1f : Keyboard.current.sKey.isPressed ? -1f : 0f;

        Vector3 moveDirection = playerBody.forward * vertical + playerBody.right * horizontal;
        // 重力の適用
        if (characterController.isGrounded)
        {
            verticalVelocity = 0f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        moveDirection.y = verticalVelocity;

        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        if (Input.GetKey(KeyCode.W))
        {
            Controller.SetBool("movement_forward", true);
        }
        else
        {
            Controller.SetBool("movement_forward", false);
        }

        if (Input.GetKey(KeyCode.A))
        {
            Controller.SetBool("movement_left", true);
        }
        else
        {
            Controller.SetBool("movement_left", false);
        }

        if (Input.GetKey(KeyCode.D))
        {
            Controller.SetBool("movement_right", true);
        }
        else
        {
            Controller.SetBool("movement_right", false);
        }

        if (Input.GetKey(KeyCode.S))
        {
            Controller.SetBool("movement_back", true);
        }
        else
        {
            Controller.SetBool("movement_back", false);
        }
    }
    }
}

