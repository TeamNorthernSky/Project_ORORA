using UnityEngine;
using UnityEngine.Rendering;

public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance { get; private set; }

    [Header("Map Settings")]
    [Tooltip("지도의 중심 좌표 설정")]
    public Vector2 mapCenter = Vector2.zero;
    [Tooltip("지도의 실제 크기 (World X, Z)")]
    public Vector2 mapSize = new Vector2(100f, 100f);
    [Tooltip("시야 렌더 타겟의 해상도")]
    public int resolution = 1024;
    
    [Header("Dependencies")]
    [Tooltip("원형 시야를 그릴 마스크 Material (FogOfWarMask.shader)")]
    public Material visionMaskMaterial; 
    [Tooltip("누적 메모리 블렌딩을 시킬 Material (FogOfWarAccumulate.shader)")]
    public Material accumulateMaterial; 
    
    private RenderTexture _fogCurrentRT;
    private RenderTexture _fogVisitedRT;
    private CommandBuffer _cb;
    private Mesh _quadMesh;

    private void Awake()
    {
        Instance = this;
        InitializeFog();
    }

    private void InitializeFog()
    {
        // 1. 실시간 시야와 누적 시야 RT 생성
        _fogCurrentRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.R8);
        _fogCurrentRT.name = "_FogCurrentRT";
        
        _fogVisitedRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.R8);
        _fogVisitedRT.name = "_FogVisitedRT";
        
        // 2. 누적 기억 RT 검은색(0, 안보임)으로 초기화 
        RenderTexture.active = _fogVisitedRT;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = null;

        _cb = new CommandBuffer { name = "FogOfWar_DrawVision" };
        _quadMesh = CreateQuad();
    }

    private void LateUpdate()
    {
        if (_cb == null) return;
        
        _cb.Clear();
        
        // 1. 매 프레임 Current 시야 RT 클리어 (현재 영역 초기화)
        _cb.SetRenderTarget(_fogCurrentRT);
        _cb.ClearRenderTarget(false, true, Color.black);
        
        // 2. 위에서 바닥을 내려다보는 카메라 직교 매트릭스 설정
        Vector3 camPos = new Vector3(mapCenter.x, 100f, mapCenter.y);
        Matrix4x4 view = Matrix4x4.Inverse(Matrix4x4.TRS(camPos, Quaternion.LookRotation(Vector3.down, Vector3.up), Vector3.one));
        Matrix4x4 proj = Matrix4x4.Ortho(-mapSize.x * 0.5f, mapSize.x * 0.5f, -mapSize.y * 0.5f, mapSize.y * 0.5f, 0.1f, 200f);
        _cb.SetViewProjectionMatrices(view, proj);
        
        // 3. 등록된 모든 시야 보유 유닛에 대해 Quad(마스크) 그리기
        foreach(var unit in FogOfWarUnit.AllUnits)
        {
            float r = unit.visionRadius;
            Vector3 pos = unit.transform.position;
            // Quad 메시는 Z=0, XY 평면 기준이므로 XZ 평면상으로 그리기위해 회전 적용
            Matrix4x4 trs = Matrix4x4.TRS(
                new Vector3(pos.x, 0f, pos.z), 
                Quaternion.Euler(90f, 0f, 0f), 
                new Vector3(r * 2f, r * 2f, 1f)
            );
            _cb.DrawMesh(_quadMesh, trs, visionMaskMaterial);
        }
        
        // 4. Current 시야를 Visited 누적 RT에 Max Blend 기법으로 덮어씌움
        _cb.Blit(_fogCurrentRT, _fogVisitedRT, accumulateMaterial);
        
        // 커맨드 버퍼 실행 시점
        Graphics.ExecuteCommandBuffer(_cb);
        
        // 5. 후처리 셰이더용 Global 변수 전달
        Shader.SetGlobalTexture("_FogCurrentRT", _fogCurrentRT);
        Shader.SetGlobalTexture("_FogVisitedRT", _fogVisitedRT);
        
        Vector4 mapBounds = new Vector4(mapCenter.x - mapSize.x * 0.5f, mapCenter.y - mapSize.y * 0.5f, mapSize.x, mapSize.y);
        Shader.SetGlobalVector("_FogMapBounds", mapBounds);
    }

    private Mesh CreateQuad()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3( 0.5f, -0.5f, 0),
            new Vector3(-0.5f,  0.5f, 0),
            new Vector3( 0.5f,  0.5f, 0)
        };
        mesh.uv = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        return mesh;
    }

    private void OnDestroy()
    {
        if (_fogCurrentRT) _fogCurrentRT.Release();
        if (_fogVisitedRT) _fogVisitedRT.Release();
        if (_cb != null) _cb.Release();
    }
}
