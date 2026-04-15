using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// 라벨 회전을 카메라 투영면에 평행하게 유지.
    /// 카메라의 rotation 을 그대로 따라가기만 하면 라벨의 로컬 XY 평면 = 카메라 image plane.
    /// (카메라 방향을 향하는 LookAt billboard와 달리, 고정된 상대 자세 유지)
    /// </summary>
    [DisallowMultipleComponent]
    public class TMLabelBillboard : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        private void LateUpdate()
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;
            transform.rotation = cam.transform.rotation;
        }
    }
}
