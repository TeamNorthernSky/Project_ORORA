# KJ FOW 파이프라인 핸드오프 (2026-04-21)

> **대상**: KJ 및 KJ가 사용하는 AI 에이전트.
> **목적**: 2026-04-21 JC가 수행한 "KJ 안개 렌더 파이프라인의 JC_Work 의존성 분리 + 매니저 개편" 작업의 현재 상태와 이후 작업 방향 안내.
>
> 이 파일을 먼저 읽고, 세부 히스토리가 필요하면 `C:\dev\_MD\KJ_FOW_분리_핸드오프_260421.md`(JC 관점 작업일지)를 참고하세요.

---

## 1. TL;DR

- `KJ_GameManager` → **`KJ_FOWManager`**로 리네이밍됨 (클래스·파일·프리팹)
- `KJ_AutoBootstrap` → **`KJ_FOWBootstrap`**로 리네이밍됨
- `KJ_SuppressLegacyBootstrap`은 **제거**됨 (JC `[GameManager]`와 공존하는 구조로 변경)
- KJ 전용 Renderer Data(**`URP-JC-KJ-Renderer.asset`**)를 `Assets/KJ_Work/RenderData/`에 만들고, 공용 Pipeline Asset 3종(URP-Balanced/Performant/HighFidelity)의 Renderer List에 **index 2**로 등록
- KJ 안개 씬(`FogGrid_Test.unity`)은 이제 이 KJ Renderer를 쓰며, **JC_Work 폴더에 대한 의존성 0**
- 결론: **"폴더 독립성"** 달성 — JC_Work를 통째로 삭제해도 KJ FOW 파이프라인은 동일하게 동작

---

## 2. 현재 매니저 구조

프로젝트는 **두 개의 DDOL 싱글턴 매니저**가 공존합니다.

### 2.1 `[GameManager]` (JC 소유)
- 경로: `Assets/JC_Work/Prefab_jc/[GameManager].prefab`
- 담당: 재화(CurrencyManager) / 씬 전환(SceneLoader) / 디버그 패널(DebugManager) / EventSystem(EventSystemGuard)
- **더 이상 Grid/FogOfWar를 노출하지 않음** (2026-04-21 제거)
- 부트스트랩: `Assets/JC_Work/Scripts_jc/Manager/AutoBootstrap.cs`

### 2.2 `[KJ_FOWManager]` (KJ 소유)
- 경로: `Assets/KJ_Work/Prefabs/[KJ_FOWManager].prefab`
- 담당: 전장의 안개 (Grid + FogOfWar)
- 루트 한 GameObject에 컴포넌트 4개 flat:
  - `KJ_FOWManager` — 싱글턴 + `Instance.Grid` / `Instance.FogOfWar` 프로퍼티
  - `KJ_PlayGridManager` — 그리드 데이터
  - `KJ_PlayFogManager` — RT 기반 안개 렌더
  - `KJ_JCStyleFogBridge` — 파티/턴/Revealer 연동
- 부트스트랩: `Assets/KJ_Work/Scripts/Integration/KJ_FOWBootstrap.cs` (BeforeSceneLoad)
- `DontDestroyOnLoad` 유지 — 탐사↔전투 전환 시 fog 상태 보존 의도

### 2.3 두 매니저의 관계
- **완전 독립**. 서로의 `Instance`를 참조하지 않음
- 부트스트랩 순서 의존 없음 (같은 `BeforeSceneLoad` 시점이지만 결과는 순서 무관)
- 모든 씬에서 둘 다 떠 있음. 충돌 없음 (이름도 타입도 다름)

---

## 3. 렌더 파이프라인 구조

공용 Pipeline Asset **3종 모두** 동일한 Renderer List를 가집니다:

| Index | Renderer Data | 위치 | 용도 |
|---|---|---|---|
| 0 | 기본 URP Renderer | `Assets/Settings/URP-*-Renderer.asset` | 안개 없는 일반 씬 |
| 1 | `URP-JC-Renderer` | `Assets/JC_Work/RenderData_jc/URP-JC-Renderer.asset` | **레거시**. 현재 실사용 없음 |
| 2 | `URP-JC-KJ-Renderer` | `Assets/KJ_Work/RenderData/URP-JC-KJ-Renderer.asset` | **현 활성 KJ 안개 렌더** |

### 3.1 `URP-JC-KJ-Renderer.asset`의 Feature 구성

| 순서 | Feature | 참조 에셋 | 위치 |
|---|---|---|---|
| 1 | SSAO | URP 내장 | 공용 |
| 2 | `KJ_FogOfWarFeatureJC` | `fogMaterial` = `KJ_FogOfWarMaterialJC.mat` | `Assets/KJ_Work/Materials/` |
| 3 | `KJ_FogStencilPrepassFeature` | `stencilShader` = `KJ_FogStencilPrepass.shader` | `Assets/KJ_Work/Shaders/` |
| 4 | `KJ_FogHidableClipPassFeature` | (참조 없음, passEvent=300) | — |

### 3.2 KJ 셰이더·머티리얼 전체 (KJ_Work 내부, JC_Work 의존 0)

- `Assets/KJ_Work/Shaders/KJ_FogMaskJC.shader` — `Custom/KJ/FogMaskJC`
- `Assets/KJ_Work/Shaders/KJ_FogDecayJC.shader` — `Custom/KJ/FogDecayJC`
- `Assets/KJ_Work/Shaders/KJ_FogOfWarJC.shader` — `Custom/KJ/FogOfWarJC`
- `Assets/KJ_Work/Shaders/KJ_FogStencilPrepass.shader` — `Custom/KJ/FogStencilPrepass`
- `Assets/KJ_Work/Materials/FogMaskJC.mat`, `FogDecayJC.mat` — KJ_PlayFogManager가 런타임 `Shader.Find`로 쓰는 내부용
- `Assets/KJ_Work/Materials/KJ_FogOfWarMaterialJC.mat` — Inspector 튜닝 대상, Feature에 꽂혀있음

### 3.3 KJ 안개 씬 리스트
- `Assets/KJ_Work/Scenes/FogGrid_Test.unity` — Main Camera `m_RendererIndex: 2` 설정됨. **안개 정상 렌더**
- `Assets/KJ_Work/Scenes/FogofWar_Test.unity`, `TestScene.unity` — Main Camera `m_RendererIndex: -1`(기본). 안개 렌더 비활성 상태. 필요 시 인덱스 2로 바꾸면 됨

---

## 4. 고립 에셋 — `URP-JC-KJ.asset` (Pipeline Asset)

- 경로: `Assets/KJ_Work/RenderData/URP-JC-KJ.asset`
- **현재 Quality Settings의 어떤 Quality Level에도 할당되지 않음** → 렌더링 파이프라인에 아무 영향 없음
- **삭제하지 않고 보존**: 장차 KJ 전용 Quality Level 분리 전략을 쓸 여지 차원

### 활성화하려면
1. `Edit > Project Settings > Quality`
2. 특정 Quality Level의 "Render Pipeline Asset" 슬롯에 이 파일 할당

(주의: 그렇게 하면 그 Quality Level의 모든 씬이 이 Pipeline Asset을 사용하게 됨. 지금 구조에서는 불필요.)

---

## 5. KJ 측 이중 트랙 병존 — **중요 결정 필요**

KJ_Work 내부에는 현재 안개 구현 방식이 **두 트랙** 병존합니다:

### 5.1 **JCStyle 트랙** — 이번 분리 작업의 본체
- 위치: `Assets/KJ_Work/Scripts/JCStyle/`, `Assets/KJ_Work/Prefabs/[KJ_FOWManager].prefab`
- 특징: JC 원본 코드를 `KJ_` 접두어로 복제한 구조. KJ_PlayGridManager가 자체 그리드 관리
- 현재 실사용 중 (FogGrid_Test.unity가 이 경로 사용)

### 5.2 **Integration 트랙** — DH 호환 실험
- 위치: `Assets/KJ_Work/Scripts/Integration/`
- 주요 파일:
  - `KJ_DHCompatibleFogManager.cs` — DH의 `GridManager`, `LevelLoader`, `LevelData`를 직접 참조
  - `FogGridBootstrap.cs`, `FogSceneReferences.cs` — 씬 단위 레퍼런스 수집
  - `DHToJCFogBridge.cs` — DH 그리드 ↔ JC 스타일 안개 브리지
- 현재 어느 씬에서도 활성 실사용 없음 (확인 필요)

### 5.3 고려 사항
- 프로토타입 머지 단계에서 DH의 그리드 시스템이 사실상의 기준이 됨
- JCStyle 트랙의 `KJ_PlayGridManager`는 DH 그리드와 중복 — 통합 시 이 매니저는 폐기될 가능성
- Integration 트랙이 미래 수렴 지점일 수도 있음
- **결정은 KJ**. JC는 이 결정에 관여하지 않음

---

## 6. 향후 작업 체크리스트 (추천 순서)

### 6.1 단기
- [ ] 현 상태 검토 후 커밋 (2026-04-22 JC와 미팅 예정)
- [ ] KJ 씬(`FogofWar_Test.unity`, `TestScene.unity`)도 안개가 필요하면 Main Camera `m_RendererIndex`를 2로 변경
- [ ] `URP-JC-KJ.asset`(고립 에셋)의 장기 처분 결정 — 삭제 vs 유지

### 6.2 중기 — 트랙 통합 방향 결정
- [ ] JCStyle vs Integration 중 어느 쪽으로 수렴할지 판단
- [ ] 수렴 방향이 정해지면 반대쪽 트랙의 파일 정리
- [ ] DH 그리드와의 통합 시점·인터페이스 합의

### 6.3 장기 — 프로토타입 머지
- [ ] KJ_FOWManager의 Grid 의존성을 DH 그리드로 전환
- [ ] fog 상태(RT_Current/RT_Explored)의 수명·저장 위치 재검토 (현재는 DDOL 보존, 향후 ScriptableObject나 `[GameManager]` 산하 상태 모듈로 이전 가능)
- [ ] KJ_FOWManager를 씬 오브젝트로 격하할지 재평가

---

## 7. 수정 시 주의사항

### 7.1 건드리면 안 되는 것
- **공용 Pipeline Asset 3종의 Renderer List 순서** — index 1(JC)을 지우면 리스트가 재정렬되어 모든 씬의 `m_RendererIndex: 2` 설정이 깨집니다. JC_Work를 나중에 삭제하더라도 Pipeline Asset의 엔트리는 유지하거나 null 상태로 두어야 함
- **`KJ_FOWBootstrap.cs`의 프리팹 경로 상수** — 프리팹 리네이밍·이동 시 이 경로도 같이 수정
- **JC `[GameManager]`에 fog/grid 관련 컴포넌트 재추가 금지** — 폴더 독립성 위반. 필요하면 `[KJ_FOWManager]` 산하로

### 7.2 Unity URP RendererFeature 변경 시
- Inspector에서 Add Renderer Feature로 추가/제거하는 것이 가장 안전
- 직접 YAML 편집 시 `m_RendererFeatureMap` 해시가 깨질 수 있음 (과거 이슈 있었음)
- 스크립트로 조작이 필요하면 `script-execute`로 `AssetDatabase.AddObjectToAsset` + `ValidateRendererFeatures` 리플렉션 호출 필요

### 7.3 셰이더 파일
- **파일명**에는 `KJ_` 접두어와 `JC` 접미어 병용(JC 원본 복제임을 명시)
- **내부 `Shader "..."` 선언**은 `Custom/KJ/` namespace 사용 (JC와 충돌 방지)
- 복제 신규 셰이더 추가 시 두 규칙 모두 지킬 것

---

## 8. 검증 방법

변경 후 다음을 확인:

1. **컴파일**: Unity 에디터 콘솔에 빨간 에러 없음
2. **부트스트랩**: 빈 씬에서 Play → Hierarchy에 `[GameManager]`와 `[KJ_FOWManager]` 둘 다 DDOL 영역에 뜸
3. **JC 디버그 패널**: 백쿼트키(`)로 열림·닫힘 정상
4. **안개**: `FogGrid_Test.unity` 재생 → 플레이어 주변 시야 원·이동 시 탐험 영역 확장·시간 경과 후 감쇠 정상
5. **폴더 독립성**: `JC_Work` 폴더 이름을 임시로 `JC_Work_disabled` 등으로 변경 후 FogGrid_Test 재생 → 안개 여전히 정상. 검증 후 원복

---

## 9. 연락처·참고

- JC 측 작업 히스토리: `C:\dev\_MD\KJ_FOW_분리_핸드오프_260421.md`
- JC 구현 리마인더: `C:\dev\_MD\ORORA_구현_리마인더_JC.md` (섹션 2·3 갱신됨)
- JC 안개 파이프라인 원본 가이드: `Assets/JC_Work/_Docs/유니티_전장의안개_JC파이프라인_통합가이드_260414.md` (일부 경로 stale — 이번 작업으로 인해 `FogStencilPrepass.shader` → `FogStencilPrepassJC.shader` 등, 그 외 JC 측 매니저 제거 사실 미반영)
- 팀 전체 아키텍처: `Assets/JC_Work/__TestMerge/` (보존된 참고용 구축물)

이 문서 자체는 KJ가 자유롭게 수정·확장해도 됩니다. JC 본인에게 남기는 부분만 상단 "TL;DR"과 "매니저 구조"의 JC 측 설명은 건드리기 전 확인 요망.
