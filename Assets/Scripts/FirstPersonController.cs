using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float sensitivity = 2.0f;
    public Transform playerBody; // プレイヤーのオブジェクト（親）をセット

    private CharacterController characterController;

    float xRotation = 0f;
    float yRotation = 0f;
    public float gravity = -9.81f;
    private float verticalVelocity = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        characterController = GetComponent<CharacterController>(); // プレイヤーの CharacterController を取得
    }



    void Update()
    {
        // マウス操作（視点の回転）
        float mouseX = Mouse.current.delta.x.ReadValue() * sensitivity;
        float mouseY = Mouse.current.delta.y.ReadValue() * sensitivity;

        // カメラの回転（上下視点のみ）
        xRotation -= mouseY * 0.5f;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        // プレイヤーの回転（左右視点のみ）
        yRotation += mouseX * 0.5f;
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
    }
}