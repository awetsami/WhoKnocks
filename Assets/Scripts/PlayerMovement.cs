using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public float baseSpeed = 5f;
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

    private InteractionSystem interactionSystem;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Application.targetFrameRate = 144;

        interactionSystem = GetComponent<InteractionSystem>();
    }

    void Update()
    {
        // 1. ÖNCE OYUNCU GÝRDÝLERÝNÝ OKU
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // 2. DÝYALOG KONTROLÜ: EÐER DÝYALOG AÇIKSA GÝRDÝLERÝ SIFIRLA!
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive)
        {
            mouseX = 0f;
            mouseY = 0f;
            x = 0f;
            z = 0f;
        }

        // --- KAMERA BAKIÞI ---
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

        Vector3 move = transform.right * x + transform.forward * z;

        float currentMultiplier = 1f;
        if (interactionSystem != null)
        {
            currentMultiplier = interactionSystem.GetPlayerSpeedMultiplier();
        }

        float currentSpeed = baseSpeed * currentMultiplier;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // --- YERÇEKÝMÝ ---
        // Girdiler 0 olsa bile bu kýsým çalýþmaya devam edecek, böylece aþaðý düþmeyeceksin.
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}