using UnityEngine;
using System.Collections.Generic;

public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance { get; private set; }

    [Header("Map & Grid Settings")]
    [Tooltip("지도의 중심 좌표 설정")]
    public Vector2 mapCenter = Vector2.zero;
    
    [Tooltip("지도의 실제 크기 (World X, Z)")]
    public Vector2 mapSize = new Vector2(128f, 128f);
    
    [Tooltip("그리드의 해상도 (타일 개수, 128x128 맵이면 128)")]
    public int gridResolution = 128;

    [Header("Fog Settings")]
    [Tooltip("시야가 넓어지는 칸 수 (1 = 자신 기준 상하좌우를 포함한 3x3 정사각형 형태)")]
    public int visionRange = 1;

    private Texture2D _currentTex;
    private Texture2D _visitedTex;

    private Color32[] _currentPixels;
    private Color32[] _visitedPixels;

    private readonly Color32 _clearColor = new Color32(0, 0, 0, 0);       // 안 보임
    private readonly Color32 _fogColor = new Color32(255, 0, 0, 0);       // 보임 (셰이더가 r채널을 사용하므로 R=255)

    private void Awake()
    {
        Instance = this;
        InitializeFog();
    }

    private void InitializeFog()
    {
        // 실시간 시야를 담을 텍스처
        _currentTex = new Texture2D(gridResolution, gridResolution, TextureFormat.RGBA32, false);
        _currentTex.filterMode = FilterMode.Bilinear; // 타일 경계를 부드럽게. 완전히 깎아지른 네모를 원하면 Point로 변경하세요.
        _currentTex.wrapMode = TextureWrapMode.Clamp;

        // 방문한 시야를 누적할 텍스처 (시간이 지나도 사라지지 않음)
        _visitedTex = new Texture2D(gridResolution, gridResolution, TextureFormat.RGBA32, false);
        _visitedTex.filterMode = FilterMode.Bilinear;
        _visitedTex.wrapMode = TextureWrapMode.Clamp;

        int totalPixels = gridResolution * gridResolution;
        _currentPixels = new Color32[totalPixels];
        _visitedPixels = new Color32[totalPixels];

        // 초기 방문 배열을 완전 어두운 상태로 초기화
        for (int i = 0; i < totalPixels; i++)
        {
            _currentPixels[i] = _clearColor;
            _visitedPixels[i] = _clearColor;
        }

        _visitedTex.SetPixels32(_visitedPixels);
        _visitedTex.Apply();
    }

    private void LateUpdate()
    {
        // 1. 현재 프레임의 시야(Current) 배열만 매번 어둡게 지우기
        for (int i = 0; i < _currentPixels.Length; i++)
        {
            _currentPixels[i] = _clearColor;
        }

        // 월드 좌하단 시작점 (맵 중심축 기준)
        float mapMinX = mapCenter.x - mapSize.x * 0.5f;
        float mapMinZ = mapCenter.y - mapSize.y * 0.5f;
        
        // 2. 유닛(플레이어) 위치 기반으로 그리드 칠하기
        foreach (var unit in FogOfWarUnit.AllUnits)
        {
            Vector3 pos = unit.transform.position;

            // 월드 좌표를 그리드의 2D 인덱스로 변환
            int gridX = Mathf.FloorToInt((pos.x - mapMinX) / mapSize.x * gridResolution);
            int gridY = Mathf.FloorToInt((pos.z - mapMinZ) / mapSize.y * gridResolution);

            // 정사각형 영역 마스킹 (내 타일을 중심으로 visionRange 만큼의 칸수)
            for (int y = -visionRange; y <= visionRange; y++)
            {
                for (int x = -visionRange; x <= visionRange; x++)
                {
                    int targetX = gridX + x;
                    int targetY = gridY + y;

                    // 마스킹 타일이 맵 테두리를 벗어나지 않았는지 안전 검사
                    if (targetX >= 0 && targetX < gridResolution && targetY >= 0 && targetY < gridResolution)
                    {
                        // 1차원 배열 인덱스로 변환
                        int index = targetY * gridResolution + targetX;
                        
                        _currentPixels[index] = _fogColor;
                        _visitedPixels[index] = _fogColor; // 한 번이라도 밝혀지면 영구히 밝게 유지
                    }
                }
            }
        }

        // 3. 텍스처에 픽셀 적용
        _currentTex.SetPixels32(_currentPixels);
        _currentTex.Apply();

        _visitedTex.SetPixels32(_visitedPixels);
        _visitedTex.Apply();

        // 4. 전역 셰이더 변수로 전달 (기존 FogOfWarOverlay 및 Procedural 셰이더가 이 텍스처를 읽음)
        Shader.SetGlobalTexture("_FogCurrentRT", _currentTex);
        Shader.SetGlobalTexture("_FogVisitedRT", _visitedTex);

        // 맵 바운더리 전달
        Vector4 mapBounds = new Vector4(mapMinX, mapMinZ, mapSize.x, mapSize.y);
        Shader.SetGlobalVector("_FogMapBounds", mapBounds);
    }
}
