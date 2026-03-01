using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public float baseSpeed = 5f; // Deðiþken adýný "baseSpeed" yaptýk ki ana hýzýmýz hep sabit kalsýn
    public float gravity = -9.81f;
    public float jumpHeight = 1f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    Vector3 velocity;
    bool isGrounded;

    public Transform playerCamera;
    public float mouseSensitivity = 100f;
    float xRotation = 0f;

    private InteractionSystem interactionSystem; // Etkileþim sistemini baðlayacaðýmýz referans

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Application.targetFrameRate = 144;

        // Karakterin üzerindeki InteractionSystem'i otomatik olarak bulur
        interactionSystem = GetComponent<InteractionSystem>();
    }

    void Update()
    {
        // --- KAMERA BAKIÞI ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // --- HAREKET ---
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        // YENÝ: Etkileþim sisteminden araba hýz çarpanýný alýyoruz
        float currentMultiplier = 1f;
        if (interactionSystem != null)
        {
            currentMultiplier = interactionSystem.GetPlayerSpeedMultiplier();
        }

        // Ana hýzý (baseSpeed), arabanýn aðýrlýk çarpanýyla çarpýp uyguluyoruz
        float currentSpeed = baseSpeed * currentMultiplier;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // --- YERÇEKÝMÝ ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}