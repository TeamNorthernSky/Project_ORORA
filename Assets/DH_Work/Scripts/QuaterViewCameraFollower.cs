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

    [Header("Offsets")]
    [SerializeField] private Vector3 positionOffset = new Vector3(10f, 10f, -10f);
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Smoothing")]
    [SerializeField] private float positionLerp = 10f;
    [SerializeField] private float rotationLerp = 10f;

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
        positionOffset = newPositionOffset;
        lookAtOffset = newLookAtOffset;
    }

    private void LateUpdate()
    {
        if (!followEnabled || followTarget == null)
            return;

        Vector3 desiredPos = followTarget.position + positionOffset;
        Vector3 desiredLookAt = followTarget.position + lookAtOffset;

        // 스무딩 (프레임 독립 느낌)
        float tPos = 1f - Mathf.Exp(-positionLerp * Time.deltaTime);
        float tRot = 1f - Mathf.Exp(-rotationLerp * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, desiredPos, tPos);

        Vector3 dir = desiredLookAt - transform.position;
        if (dir.sqrMagnitude > 0.000001f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, tRot);
        }
    }
}

