using UnityEngine;

public class FogOfWarNoiseManager : MonoBehaviour
{
    [Header("Fog Overlay Noise Settings")]
    [Tooltip("FogOverlayPlane에 적용된 Renderer 컴포넌트")]
    public Renderer fogOverlayRenderer;

    [Tooltip("KJ_Work/Images/NoiseSample.png 텍스처를 할당하세요")]
    public Texture2D noiseTexture;

    [Tooltip("노이즈 패턴이 흘러가는 스크롤 속도 (X, Y)")]
    public Vector2 scrollSpeed = new Vector2(0.05f, 0.05f);

    [Tooltip("노이즈의 확대/축소 스케일 (값이 크면 무늬가 촘촘해짐)")]
    public Vector2 noiseScale = new Vector2(3f, 3f);

    [Tooltip("노이즈가 안개 그래픽에 미치는 영향력 (0~1)")]
    [Range(0f, 1f)]
    public float noiseIntensity = 0.4f;

    private Vector2 _currentOffset;
    private Material _instanceMat;

    private void Start()
    {
        if (fogOverlayRenderer != null)
        {
            // Material의 복사본(Instance)을 생성하여 수정 시 원본 에셋이 변형되지 않도록 보호
            _instanceMat = fogOverlayRenderer.material;
        }
    }

    private void Update()
    {
        if (_instanceMat == null) return;

        // 텍스처 및 설정 상태 반영
        if (noiseTexture != null)
        {
            _instanceMat.SetTexture("_NoiseTex", noiseTexture);
        }
        
        _instanceMat.SetVector("_NoiseScale", new Vector4(noiseScale.x, noiseScale.y, 0, 0));
        _instanceMat.SetFloat("_NoiseIntensity", noiseIntensity);

        // 매 프레임 스크롤 속도에 시간(DeltaTime)을 곱하여 오프셋 누적 (UV 스크롤링)
        _currentOffset += scrollSpeed * Time.deltaTime;
        
        // 셰이더로 스크롤링 위치 넘기기
        _instanceMat.SetVector("_NoiseOffset", new Vector4(_currentOffset.x, _currentOffset.y, 0, 0));
    }
}
