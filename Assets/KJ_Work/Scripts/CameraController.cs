using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float fastMoveMultiplier = 2.5f; // Left Shift 키를 누르고 이동할 때 배속
    public bool freezeYAxis = false;        // true로 켜면 wasd 이동 시 위/아래 높이(Y축)가 고정됨 (RTS 방식)

    [Header("Rotation Settings")]
    public float lookSensitivity = 2f;
    public bool invertY = false;

    private float pitch;
    private float yaw;

    private void Start()
    {
        // 시작할 때 카메라의 현재 회전값을 가져와서 초기화
        Vector3 angles = transform.eulerAngles;
        pitch = angles.x;
        yaw = angles.y;
    }

    private void Update()
    {
        HandleRotation();
        HandleMovement();
    }

    private void HandleRotation()
    {
        // 1번 버튼 = 마우스 우클릭
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * lookSensitivity;
            pitch += mouseY * lookSensitivity * (invertY ? 1f : -1f);

            // 카메라가 360도 뒤집히지 않도록 피치(상하 회전각)를 89도까지만 제한
            pitch = Mathf.Clamp(pitch, -89f, 89f);

            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); // A, D, Left, Right
        float vertical = Input.GetAxisRaw("Vertical");     // W, S, Up, Down

        if (horizontal != 0 || vertical != 0)
        {
            // Left Shift 키를 누르면 더 빠르게 이동하도록
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * fastMoveMultiplier : moveSpeed;

            // 카메라가 쳐다보고 있는 방향(forward, right)을 기준으로 이동 벡터 계산
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            if (freezeYAxis)
            {
                // Y축 이동을 무시하고 평면상에서만 이동 (전략게임 시점)
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
            }

            Vector3 moveDirection = (forward * vertical) + (right * horizontal);

            // Time.deltaTime을 곱해 프레임과 상관없이 일정한 속도로 이동
            transform.position += moveDirection.normalized * (currentSpeed * Time.deltaTime);
        }
    }
}
