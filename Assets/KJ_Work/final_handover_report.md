# Project ORORA - KJ 안개/그리드 통합 인수인계

업데이트: 2026-04-16

## 작업 규칙
- 컨텍스트를 파악하려면 반드시 이 파일을 먼저 읽는다.
- 매 작업 시, 다른 컨텍스트에서도 이어서 사용할 수 있도록 이 파일에 작업 내역을 기록하고 갱신한다.
- `KJ_Work` 바깥의 Script 파일은 수정하지 않는다.

## 목표
- `DHScene`의 그리드 기반 이동 및 좌표계를 유지한다.
- 그 위에 `JC`의 전장의 안개 렌더링 파이프라인을 올려서 사용한다.
- 구현 변경은 `Assets/KJ_Work` 내부로만 제한한다.

## 현재 통합 방향
- `DH`가 담당하는 부분:
  - `GridManager`
  - `PartyGridMover`
  - `PartyRegistry`
  - 경로 미리보기, 클릭 선택, UI 흐름
- `KJ` 브리지에서 담당하는 부분:
  - 씬 참조 수집
  - 파티 이동 정보를 안개 갱신으로 전달하는 연결 흐름
  - `DH` 그리드와 호환되는 fog manager 래퍼
- `JC` 스타일 fog가 담당하는 부분:
  - visibility 렌더 텍스처 관리
  - explored visibility 렌더 텍스처 관리
  - URP fog 렌더 피처 및 셰이더 샘플링

## KJ_Work에 구현된 내용

### 통합 스크립트
- [KJ_DHCompatibleFogManager.cs](/d:/Project_ORORA/Assets/KJ_Work/Scripts/Integration/KJ_DHCompatibleFogManager.cs)
  - `KJ_Work` 안에서 최소한의 `JC PlayFogManager` 동작을 다시 구성한다.
  - `JC`의 `GameManager`에 의존하지 않고 `DH`의 `GridManager`로 world/grid 변환을 처리한다.
  - `_VisibilityCurrentTex`, `_VisibilityExploredTex`, `_GridWorldSize` 셰이더 전역값을 관리한다.
  - `UpdatePlayerVisibility(Vector2Int, int)`를 통해 그리드 기반 시야 갱신을 받는다.
- [DHToJCFogBridge.cs](/d:/Project_ORORA/Assets/KJ_Work/Scripts/Integration/DHToJCFogBridge.cs)
  - `PartyGridMover`의 위치를 추적해서 `KJ_DHCompatibleFogManager`로 전달한다.
  - 포커스된 파티 하나만이 아니라 `PartyRegistry` / 씬 참조에 수집된 모든 `PartyGridMover`를 기준으로 fog를 갱신한다.
  - `DH`의 `TurnManager`를 수정하지 않고, `KJ_Work`에서 `TurnManager.DayAdvanced`를 감지해 턴/일자 변경 시 파티 시야를 다시 갱신한다.
- [FogSceneReferences.cs](/d:/Project_ORORA/Assets/KJ_Work/Scripts/Integration/FogSceneReferences.cs)
  - 브리지 흐름에서 사용하는 씬 참조를 수집한다.
- [FogGridBootstrap.cs](/d:/Project_ORORA/Assets/KJ_Work/Scripts/Integration/FogGridBootstrap.cs)
  - 참조를 자동으로 해석하고, 씬 디버깅을 위해 통합 상태를 로그로 남긴다.

### 매니저 보강 내용
- `KJ_DHCompatibleFogManager`는 필요한 `JC` 셰이더가 없으면 안전하게 초기화 실패하도록 수정되었다.
- 머티리얼이나 텍스처 생성에 실패했을 때 더 이상 초기화 완료 상태로 남지 않는다.
- `KJ_DHCompatibleFogManager`는 더 이상 `Time.deltaTime`으로 fog 복원을 진행하지 않는다.
  - 기존 delay 관련 필드는 호환성을 위해 유지하고 있다.
  - 현재는 완전한 턴 기반 refog 로직으로 전환된 상태다.
- `KJ_DHCompatibleFogManager`는 이제 `KJ_Work` 안에서 공개된 그리드를 턴 기준으로 추적한다.
  - `TurnManager.DayAdvanced`를 턴/일자 진행 기준으로 사용한다.
  - `currentTurn - lastRevealedTurn < revealedTurnLifetime` 동안 공개 상태를 유지한다.
  - 현재 기본값 `revealedTurnLifetime = 3` 기준으로, `currentTurn - lastRevealedTurn >= 3`이 되면 다시 fogged 상태로 돌아간다.
  - explored 텍스처 내용은 `KJ`가 관리하는 턴 상태를 기준으로 다시 그린다.
- 현재 visibility 사용은 사실상 explored visibility에 연결되도록 바뀌었다.
  - 셰이더 호환성을 위해 `_VisibilityCurrentTex`는 계속 채워주지만, 실제로는 `rtExplored`를 가리킨다.
  - 브리지 흐름은 유지하면서, 실제 게임 플레이 결과는 별도 current 텍스처가 아니라 턴 기반 재방문 기록을 기준으로 보이게 된다.
- 여러 파티의 방문/시야 기록을 합친 상태를 지원한다.
  - `DHToJCFogBridge.RefreshAllPartyVision()`이 등록된 모든 파티 위치를 순서대로 적용한다.
  - 각 파티의 현재 위치는 턴이 바뀔 때마다 다시 방문 처리되어, 가만히 있어도 현재 위치 주변이 유지된다.
- refogged 셀은 더 이상 한 번에 최종값으로 떨어지지 않는다.
  - `KJ_DHCompatibleFogManager.refoggedVisibility` 기본값은 `0.5`이다.
  - 단계형 완화를 위해 `refoggedVisibilityStage1 = 0.8`, `refoggedVisibilityStage2 = 0.65`가 추가되었다.
  - 기본 흐름은 `1.0 -> 0.8 -> 0.65 -> 0.5` 순서로 내려간다.
- [KJ_FogOfWar.shader](/d:/Project_ORORA/Assets/KJ_Work/Shaders/KJ_FogOfWar.shader)에 디버그용 이진 시야 컷 옵션이 추가되었다.
  - `_DebugBinaryLayerCut`을 켜면 `visLow`가 임계값 이상인 셀은 `cellFog = 0`으로 강제된다.
  - `_DebugVisibleThreshold` 기본값은 `0.99`이며, 시야 경계를 얼마나 엄격하게 0/1로 자를지 조절한다.
- 시간 기반 fog 복원 잔재는 `KJ_DHCompatibleFogManager`에서 정리되었다.
  - 사용하지 않는 `FogDecay` 기반 필드 및 머티리얼 처리 코드를 제거했다.
  - 현재는 턴 기반 fog 흐름에 필요한 상태 기반 텍스처 재구성 경로만 유지한다.

## 씬 상태
- 씬: [FogGrid_Test.unity](/d:/Project_ORORA/Assets/KJ_Work/Scenes/FogGrid_Test.unity)
- 현재 `FogSystem`에 포함된 구성:
  - 기존 `DH` fog 관련 컴포넌트
  - `KJ_DHCompatibleFogManager`
  - `FogSceneReferences`
  - `FogGridBootstrap`
  - `DHToJCFogBridge`

## 중요 메모
- `FogGridManager`는 여전히 `FogSystem`에 남아 있으며, 현재는 `DH` 측 visibility 로직 호환성을 위해 씬에 유지하고 있다.
- 새로운 `KJ` 브리지 경로는 `DH_Work`나 `JC_Work` 스크립트를 수정하지 않고도 `JC` 스타일 fog 렌더링을 구동하는 것을 목표로 한다.
- 현재 단계는 브리지 구조와 씬 연결 정리에 초점을 맞춘 상태이며, 최종 게임플레이 검증까지 완료된 상태는 아니다.

## 다음 확인 사항
- `Party` 또는 `Party2`가 움직일 때 `FogSystem`이 fog를 올바르게 갱신하는지 확인
- `FogGrid_Test`에서 사용하는 렌더러에 `FogOfWar` 렌더 피처가 활성화되어 있는지 확인
- 광산 reveal도 `JC` 스타일 fog 경로로 브리지할지 여부 확인
- `DH` fog 렌더링이 더 이상 필요 없다면, 중복되는 `DH` fog 렌더 컴포넌트를 계속 활성화해 둘지 검토
