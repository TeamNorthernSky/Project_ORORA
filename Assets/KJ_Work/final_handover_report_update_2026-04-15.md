# Project ORORA - KJ Fog/Grid 통합 업데이트 메모

원본 파일 `final_handover_report.md`는 현재 잠금 상태로 직접 갱신이 불가능하여,
본 파일에 2026-04-15 작업 내역을 임시 기록한다.
원본 파일 잠금 해제 후 아래 내용을 본문에 반영할 것.

## 1. 작업 목적
- `DHScene`의 `GridManager`, `PartyGridMover`, `PartyRegistry` 기반 좌표/이동 시스템은 유지한다.
- `JC_Work`의 fog 렌더링 방식(RT_Current / RT_Explored, FogMask / FogDecay 셰이더 기반)은 유지한다.
- 다른 작업 폴더 스크립트는 수정하지 않고, `KJ_Work` 내부 브릿지 스크립트만 추가/수정한다.

## 2. 이번 작업 내역

### 2.1 추가된 스크립트
- [KJ_DHCompatibleFogManager.cs](/d:/Project_ORORA/Assets/KJ_Work/Scripts/Integration/KJ_DHCompatibleFogManager.cs)
  - `JC PlayFogManager`의 역할을 `KJ_Work` 안에서 DH 좌표계 기준으로 다시 구성한 매니저
  - `GridManager`, `LevelLoader`, `LevelData`를 직접 참조
  - `_VisibilityCurrentTex`, `_VisibilityExploredTex`, `_GridWorldSize` 글로벌 셰이더 값을 설정
  - 외부 브릿지에서 `UpdatePlayerVisibility(Vector2Int, int)` 호출로 시야 반영 가능

### 2.2 수정된 스크립트
- [DHToJCFogBridge.cs](/d:/Project_ORORA/Assets/KJ_Work/Scripts/Integration/DHToJCFogBridge.cs)
  - 기존 `PlayFogManager` 참조를 `KJ_DHCompatibleFogManager` 참조로 교체
- [FogSceneReferences.cs](/d:/Project_ORORA/Assets/KJ_Work/Scripts/Integration/FogSceneReferences.cs)
  - fog 매니저 자동 수집 대상을 `KJ_DHCompatibleFogManager`로 교체
- [FogGridBootstrap.cs](/d:/Project_ORORA/Assets/KJ_Work/Scripts/Integration/FogGridBootstrap.cs)
  - 로그 출력 항목을 새 fog 매니저 기준으로 갱신

## 3. 현재 구조 요약
- DH가 담당:
  - 그리드 좌표 계산
  - 파티 이동
  - 파티 선택/경로 갱신 이벤트
- KJ 브릿지가 담당:
  - DH 파티의 현재 grid 좌표 감지
  - fog 매니저에 시야 반영 요청 전달
- JC fog 방식이 담당:
  - RT_Current / RT_Explored 누적
  - FogMask / FogDecay 셰이더 기반 시야 마스크 렌더
  - URP fog 렌더 피처가 샘플링하는 글로벌 텍스처 출력

## 4. 다음 확인 필요 항목
- `FogSystem` 오브젝트에 `KJ_DHCompatibleFogManager`를 실제로 부착했는지 확인 필요
- `FogSystem`에서 더 이상 쓰지 않는 fog 관련 중복 컴포넌트 정리 필요 가능
- DH 좌표계(셀 중심이 정수)와 JC fog 셰이더 UV 계산 간 반 셀 오프셋 보정 필요 여부 실기 확인 필요
- 광산 시야까지 JC fog로 넘길지 여부 추가 결정 필요
