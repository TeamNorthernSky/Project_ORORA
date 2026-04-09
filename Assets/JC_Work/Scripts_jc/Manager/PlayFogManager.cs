using UnityEngine;

/// <summary>
/// 전장의 안개 매니저. GameManager 하위에 배치.
/// PlayGridManager의 visibility 데이터를 Texture2D로 변환하여 셰이더에 전달.
/// </summary>
public class PlayFogManager : MonoBehaviour
{
    private Texture2D visibilityTexture;
    private PlayGridManager grid;
    private Color[] pixelBuffer;
    private bool isDirty = true;

    private static readonly int VisibilityTexId = Shader.PropertyToID("_VisibilityTex");

    public void Initialize()
    {
        grid = GameManager.Instance?.Grid;
        if (grid == null)
        {
            Debug.LogError("[PlayFogManager] PlayGridManager를 찾을 수 없습니다");
            return;
        }

        // Visibility Texture 생성 (64x64, Point 필터 → 셀 경계가 선명)
        visibilityTexture = new Texture2D(
            PlayGridManager.GridWidth,
            PlayGridManager.GridHeight,
            TextureFormat.R8, false
        );
        visibilityTexture.filterMode = FilterMode.Bilinear;
        visibilityTexture.wrapMode = TextureWrapMode.Clamp;
        visibilityTexture.name = "FogVisibilityTex";

        pixelBuffer = new Color[PlayGridManager.GridWidth * PlayGridManager.GridHeight];

        // 초기 상태: 전부 안개
        for (int i = 0; i < pixelBuffer.Length; i++)
            pixelBuffer[i] = Color.black;

        visibilityTexture.SetPixels(pixelBuffer);
        visibilityTexture.Apply();

        // 글로벌 셰이더 변수로 설정 (모든 셰이더에서 접근 가능)
        Shader.SetGlobalTexture(VisibilityTexId, visibilityTexture);

        Debug.Log("[PlayFogManager] 초기화 완료");
    }

    private void OnDestroy()
    {
        if (visibilityTexture != null)
            Destroy(visibilityTexture);
    }

    /// <summary>
    /// 텍스처 갱신이 필요함을 표시.
    /// 플레이어 이동, 턴 종료 등에서 호출.
    /// </summary>
    public void MarkDirty()
    {
        isDirty = true;
    }

    private void LateUpdate()
    {
        if (!isDirty || grid == null) return;
        isDirty = false;
        UpdateTexture();
    }

    private void UpdateTexture()
    {
        var cells = grid.GetAllCells();
        if (cells == null) return;

        for (int i = 0; i < cells.Length; i++)
        {
            float value;
            switch (cells[i].visibility)
            {
                case GridCell.VisibilityState.Visible:
                    value = 1f;
                    break;
                case GridCell.VisibilityState.Fogged:
                    value = 0.5f;
                    break;
                default: // Unexplored
                    value = 0f;
                    break;
            }
            pixelBuffer[i] = new Color(value, 0, 0, 1);
        }

        visibilityTexture.SetPixels(pixelBuffer);
        visibilityTexture.Apply();
        Shader.SetGlobalTexture(VisibilityTexId, visibilityTexture);
    }

    /// <summary>
    /// 플레이어 위치 기반 시야 갱신.
    /// PlayerControllerJC에서 이동 완료 시 호출.
    /// </summary>
    public void UpdatePlayerVisibility(Vector2Int playerPos, int sightRadius)
    {
        grid.RevealArea(playerPos, sightRadius);
        MarkDirty();
    }
}
