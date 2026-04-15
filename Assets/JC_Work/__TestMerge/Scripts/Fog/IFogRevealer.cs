using System;
using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// 시야를 여는 주체. 파티·광산·스카우트 등 다양한 객체가 구현 가능.
    /// Registry에 등록되면 TMFogRenderManager가 순회하며 해당 셀 주변을 Visible로 설정.
    /// </summary>
    public interface IFogRevealer
    {
        Vector2Int GridPosition { get; }
        int Radius { get; }
        bool IsActive { get; }

        /// <summary>revealer의 위치·반경 변경 시 발화. Registry가 재수집 트리거에 사용 가능.</summary>
        event Action<IFogRevealer> RevealerChanged;
    }
}
