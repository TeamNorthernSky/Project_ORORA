using UnityEngine;

/// <summary>
/// 카메라를 대상(플레이어 등) 기준으로 쿼터뷰 오프셋만큼 유지하며 계속 따라가는 컴포넌트.
/// 카메라 GameObject에 붙여 사용합니다.
/// </summary>
[DisallowMultipleComponent]
public class QuarterViewCameraFollower : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private bool followEnabled = true;
    [SerializeField] private float followDelay = 0.5f;

    [Header("Offsets")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 12f, -8.6f);
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Smoothing")]
    [SerializeField] private float rotationLerp = 10f;

    [Header("Fixed Rotation")]
    [SerializeField] private Vector3 fixedEulerAngles = new Vector3(55f, 0f, 0f);

    [Header("Zoom")]
    [SerializeField] private float minZoomY = 5f;
    [SerializeField] private float maxZoomY = 30f;
    [SerializeField] private float zoomSpeed = 200f;

    [Header("Edge Scrolling")]
    [SerializeField] private float edgeThreshold = 40f;
    [SerializeField] private float edgeScrollSpeed = 30f;
    [SerializeField] private float edgeLimitRange = 50f;

    [Header("Reset")]
    [SerializeField] private KeyCode resetKey = KeyCode.Y;

    private Vector3 followVelocity;
    private Vector3 panOffset;
    private float defaultZoomY;

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    public void SetFollowEnabled(bool enabled)
    {
        followEnabled = enabled;
    }

    public void SetQuarterView(Vector3 newPositionOffset, Vector3 newLookAtOffset)
    {
        positionOffset = new Vector3(0f, newPositionOffset.y, newPositionOffset.z);
        lookAtOffset = newLookAtOffset;
        defaultZoomY = positionOffset.y;
    }

    private void Awake()
    {
        positionOffset.x = 0f;
        defaultZoomY = positionOffset.y;
    }

    private void Update()
    {
        HandleZoomInput();
        HandleEdgeScrolling();
        HandleResetInput();
    }

    private void LateUpdate()
    {
        if (!followEnabled || followTarget == null)
            return;

        Vector3 followAnchor = followTarget.position + new Vector3(lookAtOffset.x, 0f, lookAtOffset.z);
        Vector3 desiredPos = followAnchor + panOffset + positionOffset;
        desiredPos.x = followAnchor.x + panOffset.x;
        desiredPos.y = positionOffset.y;
        desiredPos.z = followAnchor.z + panOffset.z + positionOffset.z;

        // 스무딩 (프레임 독립 느낌)
        float smoothTime = Mathf.Max(0.01f, followDelay);
        float tRot = 1f - Mathf.Exp(-rotationLerp * Time.deltaTime);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref followVelocity, smoothTime);

        Quaternion desiredFixedRot = Quaternion.Euler(fixedEulerAngles);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredFixedRot, tRot);
    }

    private void HandleZoomInput()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scroll, 0f))
            return;

        positionOffset.y = Mathf.Clamp(positionOffset.y - scroll * zoomSpeed * Time.deltaTime, minZoomY, maxZoomY);
    }

    private void HandleEdgeScrolling()
    {
        if (!IsMouseInsideScreen())
            return;

        Vector3 move = Vector3.zero;
        Vector3 mousePosition = Input.mousePosition;

        if (mousePosition.x <= edgeThreshold)
            move.x -= 1f;
        else if (mousePosition.x >= Screen.width - edgeThreshold)
            move.x += 1f;

        if (mousePosition.y <= edgeThreshold)
            move.z -= 1f;
        else if (mousePosition.y >= Screen.height - edgeThreshold)
            move.z += 1f;

        if (move.sqrMagnitude <= 0f)
            return;

        move.Normalize();
        panOffset += move * (edgeScrollSpeed * Time.deltaTime);
        panOffset.x = Mathf.Clamp(panOffset.x, -edgeLimitRange, edgeLimitRange);
        panOffset.z = Mathf.Clamp(panOffset.z, -edgeLimitRange, edgeLimitRange);
        panOffset.y = 0f;
    }

    private void HandleResetInput()
    {
        if (!Input.GetKeyDown(resetKey))
            return;

        panOffset = Vector3.zero;
        positionOffset = new Vector3(0f, defaultZoomY, positionOffset.z);
    }

    private static bool IsMouseInsideScreen()
    {
        Vector3 mousePosition = Input.mousePosition;
        return mousePosition.x >= 0f
            && mousePosition.x <= Screen.width
            && mousePosition.y >= 0f
            && mousePosition.y <= Screen.height;
    }
}

