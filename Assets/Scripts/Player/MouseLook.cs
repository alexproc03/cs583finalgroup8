using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Sensitivity")]
    public float sensitivity = 2.5f;

    [Header("Vertical Clamp")]
    public float verticalClamp = 88f;

    [SerializeField] private Transform cameraHolder;

    private float _xRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivity;

        // Vertical look on camera holder (clamped)
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -verticalClamp, verticalClamp);
        cameraHolder.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        // Horizontal look rotates the whole player body
        transform.Rotate(Vector3.up * mouseX);
    }
}
