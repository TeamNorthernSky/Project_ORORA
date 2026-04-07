using UnityEngine;

public class FogOfWarManager : MonoBehaviour
{
    [Header("Map References")]
    public Transform player;
    public Camera mainCamera;
    
    [Header("Fog Map Configuration")]
    public int textureSize = 256;
    public float mapSizeX = 100f;
    public float mapSizeZ = 100f;
    public Vector2 mapCenter = Vector2.zero;

    [Header("Visibility Settings")]
    public float revealRadius = 10f;
    public float updateInterval = 0.1f;

    private Texture2D fogTexture;
    
    // 부드러운 보간을 위한 맵 데이터 배열들
    private float[] exploredMap;  // 한 번이라도 탐험했던 지역을 기억 (최대 0.75)
    private float[] targetMap;    // 이번 프레임의 목표 시야 (현재 시야 1.0 + 탐험 0.75)
    private float[] currentMap;   // 실제 텍스처에 적용되는 현재 렌더링 시야 (보간됨)
    private Color[] colorBuffer;

    [Header("Interpolation")]
    public float fadeSpeed = 5f;  // 안개가 걷히고 끼는 속도
    public float vignetteMoveSpeed = 10f; // 비네트 마스크가 플레이어를 따라가는 속도

    private Vector2 currentWorldPos; // 현재 비네트가 위치한 월드 좌표 (Lerp용)

    void Start()
    {
        if(mainCamera == null)
            mainCamera = Camera.main;

        if (player != null)
        {
            currentWorldPos = new Vector2(player.position.x, player.position.z);
        }

        fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.RFloat, false);
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        fogTexture.filterMode = FilterMode.Bilinear;
        
        int totalPixels = textureSize * textureSize;
        exploredMap = new float[totalPixels];
        targetMap = new float[totalPixels];
        currentMap = new float[totalPixels];
        colorBuffer = new Color[totalPixels];

        for (int i = 0; i < totalPixels; i++)
        {
            exploredMap[i] = 0f;
            targetMap[i] = 0f;
            currentMap[i] = 0f;
            colorBuffer[i] = new Color(0, 0, 0, 0);
        }

        fogTexture.SetPixels(colorBuffer);
        fogTexture.Apply();

        // Push map size globals
        float minX = mapCenter.x - mapSizeX * 0.5f;
        float minZ = mapCenter.y - mapSizeZ * 0.5f;
        Shader.SetGlobalVector("_MapBounds", new Vector4(minX, minZ, mapSizeX, mapSizeZ));
        Shader.SetGlobalTexture("_FogTex", fogTexture);

        // 첫 프레임 즉시 업데이트 (보간 없이)
        UpdateTargetMap();
        System.Array.Copy(targetMap, currentMap, totalPixels);
        for(int i = 0; i < totalPixels; i++) colorBuffer[i].r = currentMap[i];
        fogTexture.SetPixels(colorBuffer);
        fogTexture.Apply();
    }

    void Update()
    {
        // 1. 매 프레임 플레이어 목표 시야맵 계산
        UpdateTargetMap();

        // 2. 현재 시야맵을 목표 시야맵으로 서서히 보간(Lerp)하여 텍스처 적용
        LerpMap();
        
        // 3. 비네트(Vignette) 마스크가 플레이어의 월드 좌표에 맞춰 부드럽게 따라오도록 보간 적용
        if (player != null)
        {
            Vector2 targetWorldPos = new Vector2(player.position.x, player.position.z);
            
            // 월드 좌표 보간 (부드러운 카메라 효과)
            currentWorldPos = Vector2.Lerp(currentWorldPos, targetWorldPos, Time.deltaTime * vignetteMoveSpeed);

            Vector4 currentPosVec = Shader.GetGlobalVector("_PlayerWorldPos");
            currentPosVec.x = currentWorldPos.x;
            currentPosVec.y = currentWorldPos.y;
            Shader.SetGlobalVector("_PlayerWorldPos", currentPosVec);
        }
    }

    void UpdateTargetMap()
    {
        // 최적화: 매 프레임 targetMap을 초기화하기 보다 exploredMap을 그대로 복사해옵니다. (지나간 자리는 0.75 유지)
        System.Array.Copy(exploredMap, targetMap, exploredMap.Length);

        if (player == null) return;

        float minX = mapCenter.x - mapSizeX * 0.5f;
        float minZ = mapCenter.y - mapSizeZ * 0.5f;

        float normalizedPlayerX = (player.position.x - minX) / mapSizeX;
        float normalizedPlayerZ = (player.position.z - minZ) / mapSizeZ;

        int playerPixelX = Mathf.RoundToInt(normalizedPlayerX * textureSize);
        int playerPixelZ = Mathf.RoundToInt(normalizedPlayerZ * textureSize);

        int radiusPixels = Mathf.RoundToInt((revealRadius / mapSizeX) * textureSize);
        float fadeEdgePixels = radiusPixels * 0.3f; // 원의 테두리를 부드럽게 깎아낼 픽셀 수

        for (int y = -radiusPixels; y <= radiusPixels; y++)
        {
            for (int x = -radiusPixels; x <= radiusPixels; x++)
            {
                int px = playerPixelX + x;
                int py = playerPixelZ + y;

                if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                {
                    float distance = Mathf.Sqrt(x * x + y * y);
                    if (distance <= radiusPixels)
                    {
                        // 딱딱한 원형 시야 대신 테두리 보간을 넣어 자연스럽게 맵에 그려지도록 합니다.
                        float edgeDist = Mathf.Max(0, distance - (radiusPixels - fadeEdgePixels));
                        float visibility = 1.0f - Mathf.Clamp01(edgeDist / fadeEdgePixels);
                        
                        int idx = py * textureSize + px;
                        
                        // 현재 플레이어의 시야는 1.0 (최대 밝음) 까지 오를 수 있습니다.
                        targetMap[idx] = Mathf.Max(targetMap[idx], visibility);
                        
                        // 한 번이라도 밝혀진 곳은 최대 0.75까지만 영구 기록으로 남깁니다.
                        float exploredVisibility = Mathf.Min(visibility, 0.75f);
                        exploredMap[idx] = Mathf.Max(exploredMap[idx], exploredVisibility);
                    }
                }
            }
        }
    }

    void LerpMap()
    {
        bool isUpdated = false;
        float dt = Time.deltaTime * fadeSpeed;

        for (int i = 0; i < currentMap.Length; i++)
        {
            // 목표치와 차이가 날 때만 보간 (매우 미세한 차이는 생략하여 성능 확보)
            if (Mathf.Abs(currentMap[i] - targetMap[i]) > 0.005f)
            {
                currentMap[i] = Mathf.Lerp(currentMap[i], targetMap[i], dt);
                colorBuffer[i].r = currentMap[i];
                isUpdated = true;
            }
        }

        if (isUpdated)
        {
            fogTexture.SetPixels(colorBuffer);
            fogTexture.Apply();
        }
    }
}
