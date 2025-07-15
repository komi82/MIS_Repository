using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5.0f; // 移動速度
    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        // WASDキーの入力取得
        float horizontal = Keyboard.current.aKey.isPressed ? -1f : Keyboard.current.dKey.isPressed ? 1f : 0f;
        float vertical = Keyboard.current.wKey.isPressed ? 1f : Keyboard.current.sKey.isPressed ? -1f : 0f;

        // 移動ベクトルを作成
        Vector3 move = (transform.forward * vertical + transform.right * horizontal) * moveSpeed * Time.deltaTime;

        // キャラクターを移動
        characterController.Move(move);
    }
}