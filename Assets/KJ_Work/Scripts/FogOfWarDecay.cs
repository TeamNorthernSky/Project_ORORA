using UnityEngine;

public class FogOfWarDecay : MonoBehaviour
{
    [Tooltip("초당 시야가 달아지는/잊혀지는 속도 (예: 0.1 이면 약 10초 뒤에 완전히 어두워짐)")]
    public float decaySpeed = 0.1f;
    
    [Tooltip("시야가 사라지기 전까지 유지되는 시간 (초 단위)")]
    public float maskRemainTime = 5.0f;
    
    private Material _decayMaterial;

    private void Awake()
    {
        Shader decayShader = Shader.Find("Hidden/FogOfWarDecay");
        if (decayShader != null)
        {
            _decayMaterial = new Material(decayShader);
        }
        else
        {
            Debug.LogError("FogOfWarDecay 셰이더를 찾을 수 없습니다.");
        }
    }

    private void Update()
    {
        if (FogOfWarManager.Instance == null || _decayMaterial == null) return;

        // Mask 유지 보너스를 계산하여 글로벌 셰이더 변수로 전달
        float remainBonus = maskRemainTime * decaySpeed;
        Shader.SetGlobalFloat("_FogRemainBonus", remainBonus);

        RenderTexture visitedRT = FogOfWarManager.Instance.FogVisitedRT;
        if (visitedRT == null) return;

        // 초당 감쇠량을 구하여 셰이더 프로퍼티로 전달
        float decayAmount = decaySpeed * Time.deltaTime;
        _decayMaterial.SetFloat("_DecayAmount", decayAmount);

        // Blit을 위해 동일한 스펙의 임시 텍스처를 빌림
        RenderTexture tempRT = RenderTexture.GetTemporary(visitedRT.descriptor);
        
        // VisitedRT -> tempRT 로 지정된 양만큼 감소시킨 값을 그리기
        Graphics.Blit(visitedRT, tempRT, _decayMaterial);
        
        // 다시 tempRT -> VisitedRT 로 덮어쓰기
        Graphics.Blit(tempRT, visitedRT);
        
        // 임시 텍스처 메모리 반환
        RenderTexture.ReleaseTemporary(tempRT);
    }
    
    private void OnDestroy()
    {
        if (_decayMaterial != null)
        {
            Destroy(_decayMaterial);
        }
    }
}
