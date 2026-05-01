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
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive)
        {
            mouseX = 0f;
            mouseY = 0f;
            x = 0f;
            z = 0f;
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

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

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // --- YENÝ: CS2 TARZI HASAR AFALLAMASI (AIM PUNCH) ---
    public void ApplyDamageFlinch()
    {
        // Ekraný aniden 35 ile 55 derece arasý yukarý fýrlat
        float verticalKick = Random.Range(35f, 55f);

        // Fareyi rastgele saða veya sola -20 ile +20 derece savur
        float horizontalKick = Random.Range(-20f, 20f);

        // Yukarý bakmasý için xRotation'dan çýkarýyoruz
        xRotation -= verticalKick;

        // Boynu kýrýlmasýn diye sýnýrla
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Deðiþikliði anýnda kameraya uygula (Sarsýntý hissi)
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Karakterin gövdesini saða sola savur
        transform.Rotate(Vector3.up * horizontalKick);
    }
}