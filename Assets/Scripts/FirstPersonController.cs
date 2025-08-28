using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float sensitivity = 2.0f;
    public Transform playerBody; // �v���C���[�̃I�u�W�F�N�g�i�e�j���Z�b�g

    private CharacterController characterController;

    float xRotation = 0f;
    float yRotation = 0f;
    public float gravity = -9.81f;
    private float verticalVelocity = 0f;

    private Animator Controller = null;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        characterController = GetComponent<CharacterController>(); // �v���C���[�� CharacterController ���擾

        Controller = GetComponent<Animator>();
    }



    void Update()
    {
        // �}�E�X����i���_�̉�]�j
        float mouseX = Mouse.current.delta.x.ReadValue() * sensitivity;
        float mouseY = Mouse.current.delta.y.ReadValue() * sensitivity;

        // �J�����̉�]�i�㉺���_�̂݁j
        xRotation -= mouseY * 0.5f;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        // �v���C���[�̉�]�i���E���_�̂݁j
        yRotation += mouseX * 0.5f;
        playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // WASD�ړ�
        float horizontal = Keyboard.current.aKey.isPressed ? -1f : Keyboard.current.dKey.isPressed ? 1f : 0f;
        float vertical = Keyboard.current.wKey.isPressed ? 1f : Keyboard.current.sKey.isPressed ? -1f : 0f;

        Vector3 moveDirection = playerBody.forward * vertical + playerBody.right * horizontal;
        // �d�͂̓K�p
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
        }else{
            Controller.SetBool("movement_forward", false);
        }

        if (Input.GetKey(KeyCode.A))
        {
            Controller.SetBool("movement_left", true);
        }else{
            Controller.SetBool("movement_left", false);
        }

        if (Input.GetKey(KeyCode.D))
        {
            Controller.SetBool("movement_right", true);
        }else{
            Controller.SetBool("movement_right", false);
        }

        if (Input.GetKey(KeyCode.S))
        {
            Controller.SetBool("movement_back", true);
        }else{
            Controller.SetBool("movement_back", false);
        }

    }
}