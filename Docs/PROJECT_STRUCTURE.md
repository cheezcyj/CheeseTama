# 프로젝트 구조 안내

이 저장소는 `Assets`, `Packages`, `ProjectSettings`를 포함하며 Unity에서 게임을 실행하고 WebGL로 빌드할 수 있도록 정리한 프로젝트입니다. 개발용 회귀 테스트, 예제 JSON, 비어 있는 제작용 폴더는 포함하지 않습니다. 파일을 찾거나 이동할 때는 폴더 이름뿐 아니라 어셈블리·GUID·리소스 경로를 함께 확인하세요.

## 실행 코드와 씬 구성

```text
Assets/_Project/
├─ Scenes/                         Boot, Milkroom, Collection, Debug
├─ Shaders/                        성장 캐릭터 공용 팔레트 셰이더
├─ Scripts/
│  ├─ Core/
│  │  ├─ GameManager.cs             상태 소유·초기화·기존 게임 API
│  │  ├─ GameplayFlow/
│  │  │  └─ GameManager.MiniGames.cs 미니게임 완료·참여·보상 처리
│  │  ├─ StarterSceneBuilder.cs      공개 씬 진입점·공통 상태·에디터 미리보기
│  │  └─ SceneBuilding/              책임별 씬 구성 partial 구현
│  ├─ Gameplay/                     기능별 게임 규칙과 presenter
│  ├─ UI/                           화면 controller·배율·입력·접근성
│  ├─ Save/                         저장 데이터·복구·이전
│  ├─ Platform/                     플랫폼 연동·계정·저장 전송
│  ├─ Environment/                  방·조명·소품·테마
│  └─ Editor/
│     ├─ Build/                     빌드 실행·검증·빌드용 최적화
│     ├─ Assets/                    모델·프리팹·오디오 제작 도구
│     └─ Scenes/                    씬 생성 메뉴·에디터 미리보기 동기화
└─ Resources/                      실제 콘텐츠·모델 집합·오디오·UI·폰트
```

### 씬 구성 코드를 찾는 기준

모든 `StarterSceneBuilder.*.cs`는 같은 `CheeseTama.Core.StarterSceneBuilder` 타입의 일부입니다. 파일별로 별도 컴포넌트나 관리 객체를 생성하지 않습니다.

| 부분 파일 | 담당 내용 |
| --- | --- |
| `Account` | 프로필, 로컬 계정 화면, 로그인 상태 재바인딩 |
| `CarePanels` | 우유·요리·간식·도감·꾸미기 기본 패널 |
| `Hud` | 상태 게이지, 피드백, 보조 버튼과 상단 HUD 배치 |
| `Onboarding` | 이름, 첫 만남, 새 게임 선택, 저장 복구 안내 |
| `RecordsAndCareEvents` | 생활 기록, 귀환 요약, 돌봄 이벤트 |
| `MiniGames` | 우유방울·점프·공놀이·청소 화면 |
| `Progression` | 성장 여정, 기억 일지, 배달, 숨은 레시피 |
| `StoryPanels` | 연구, 미스터리, 꿈, 방 조사, NPC 이야기 |
| `DecorationAndLife` | 장식 배치, 자율 행동, 분위기와 계절 |
| `Settings` | 설정, 클라우드 저장 화면, 입력 바인딩 |
| `Blending` | 우유 블렌딩, 조리 선택, 직접 휴식 연결 |
| `World` | 카메라, 조명, 방 구성, 소품 판정, 캐릭터 배치 |
| `UiWidgets` | 공통 Canvas·패널·스크롤·입력·버튼 생성 |
| `UiVisuals` | 버튼 동작 연결, 아이콘, 글자·도형 스타일 |

공통 필드·정적 초기화·타입 정의는 원래 `StarterSceneBuilder.cs`에 유지합니다. `GameManager`도 직렬화 필드·이벤트·초기화·저장 상태를 원래 파일에서 소유합니다. 이 분리는 탐색과 책임 구획을 개선하는 구조 정리이며, 같은 타입 내부의 결합을 별도 서비스로 모두 분해한 설계는 아닙니다.

## 어셈블리와 이동 규칙

- 런타임 partial은 모두 `CheeseTama.asmdef` 범위에 둡니다. 같은 이름이어도 별도 Editor 어셈블리에 두면 하나의 타입으로 합쳐지지 않습니다.
- Editor 폴더의 `CheeseTama.Editor.asmdef`는 Editor 루트에 유지합니다. 하위 `Build`, `Assets`, `Scenes`에 새 어셈블리를 만들지 않습니다.
- 기존 클래스명·namespace·메뉴 경로·빌드 진입 메서드는 폴더 이름에 맞춰 임의로 바꾸지 않습니다.
- Unity 에셋은 Editor의 이동 기능으로 `.meta`와 함께 옮겨 GUID를 보존합니다. `Resources` 경로, 코드의 문자열 경로, 테스트·빌드 도구의 참조를 먼저 확인합니다.
- 기존 `GameManager.cs`의 경로와 `.meta`는 씬이 사용하는 MonoScript 식별자이므로 유지합니다.
- 생성된 `.csproj`와 `.slnx`는 수동 수정하지 않습니다. `asmdef`와 실제 소스를 수정한 후 Unity에서 IDE 프로젝트 파일을 재생성합니다.

## 루트 폴더의 역할과 보존 범위

| 경로 | 역할·취급 |
| --- | --- |
| `Assets` | 런타임 소스·에셋·필수 메타·빌드 및 에디터 도구 |
| `Packages`, `ProjectSettings` | 패키지 잠금과 Unity 설정. 캐시가 아님 |
| `Tools`, `Deploy` | 검증 도구와 배포 설정. 경로를 참조하는 호출부 확인 |
| `Docs` | 공개 사용·개발 안내와 README 시연 미디어 |
| `Builds` | 빌드할 때 생성되는 출력. 저장소에 포함하지 않음 |
| `Library` | Unity 임포트 캐시. 재생성에는 시간과 환경이 필요함 |
| `Temp` | 실행 임시 자료와 씬 복구 데이터가 있을 수 있음 |
| `Logs` | 로그 외에 테스트 결과·측정 증거가 있을 수 있음 |
| `UserSettings` | 사용자별 에디터 상태. 프로젝트 소스와 별개이나 무조건 재현 가능하지 않음 |

## 간소화된 배포 소스의 범위

- `Tests`의 EditMode·PlayMode 회귀 모음은 개발용 원본에서 관리하며 이 저장소에는 포함하지 않습니다. 이 복제본만으로 해당 회귀 테스트를 실행할 수는 없습니다.
- 공개 workflow가 사용하는 Editor 빌드 진입점과 콘텐츠·결과물 검증 도구는 유지합니다.
- 예제 JSON이 있던 `Assets/_Project/Data`와 실제 런타임 코드인 `Assets/_Project/Scripts/Data`는 다릅니다. 후자는 실행에 필요하므로 유지합니다.
- 실제 프리팹과 동적 로드 콘텐츠는 `Resources` 등에 있습니다. 이름이 비슷한 빈 제작용 폴더와 혼동하지 마세요.
- `Debug` 씬은 게임의 개발자 메뉴에서 참조하므로 유지합니다. 공개 릴리스 빌드의 씬 구성에서는 제외됩니다.
- `Library`, `Logs`, `UserSettings` 같은 로컬 생성 폴더는 버전 관리 대상이 아니며 Unity를 열면 다시 만들어질 수 있습니다.
- 원본 제작 파일, 내부 문서, 사용자 저장 및 복구 자료는 공개 소스에 포함하지 않습니다.

에셋을 정리할 때는 필수 `.meta`와 GUID, 문자열 기반 Resources 경로를 보존해야 합니다. 패키지 잠금 파일이나 Editor 폴더 전체를 단순 캐시로 간주해 삭제하지 마세요.
