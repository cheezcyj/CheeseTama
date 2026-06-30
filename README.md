# CheeseTama: The Milkroom

CheeseTama는 우유로 자라나는 작은 치즈 생명체를 돌보는 PC 우선 코지 육성 게임 프로토타입입니다. 플레이어는 밀크룸에 머물며 치즈타마의 상태를 확인하고, 우유/조합/간식/놀이/청소/수면 행동으로 성장과 도감 기록을 쌓습니다.

현재 저장소는 최종 게임이 아니라 Unity에서 핵심 루프와 비주얼 방향을 빠르게 검증하기 위한 개발 버전입니다.

## 현재 상태

- Unity 6 `6000.0.78f1` 기반 프로젝트
- MIT License
- Boot, Milkroom, Collection, Debug 씬 구성
- 로컬 JSON 저장/불러오기/초기화
- Milkroom 체류 시간, 일일 루틴, 초기 재화, 도감 기록 프로토타입
- Basic Milk / Star Milk 성장 및 기록 분리
- 3D 카툰 렌더링 방향의 절차적 CheeseTama 캐릭터
- 따뜻한 목재 방 콘셉트의 절차적 Milkroom 디오라마
- Settings 데이터 관리, Collection/Decorate 오버레이, F12 개발자 패널

## 최근 비주얼 방향

- 치즈타마 몸체는 기본 Sphere가 아니라 부드러운 커스텀 블롭 메시로 생성됩니다.
- 큰 반짝 눈, 볼터치, 작은 팔/발, 치즈 구멍, 광택 하이라이트, 부드러운 그림자를 사용합니다.
- 밀크룸은 아치형 창문, 커튼 주름, 러그 질감, 냉장고, 선반, 우유병, 식물, 의자, 조명, 소품을 절차적으로 배치합니다.
- 최종 아트 에셋 전까지도 화면이 단순 도형처럼 보이지 않는 것을 기준으로 작업합니다.

## Unity에서 실행하기

1. Unity Hub에서 `C:\Users\user\Desktop\CheeseTama` 폴더를 엽니다.
2. Unity 상단 메뉴에서 `Assets > Refresh`를 실행합니다.
3. Console에 빨간 컴파일 오류가 없는지 확인합니다.
4. Unity 상단 메뉴에서 `CheeseTama > Build Starter Scenes`를 실행합니다.
5. `Assets/_Project/Scenes/Milkroom.unity`를 엽니다.
6. Play 버튼을 누릅니다.

## 빠른 확인 루트

1. Milkroom 씬에서 Play
2. 하단 `우유`, `조합`, `간식`, `놀이`, `청소`, `수면` 버튼 클릭
3. 상태 수치, Message Bar, 치즈타마 반응 확인
4. 상단 `도감`, `꾸미기`, `설정` 메뉴 확인
5. `F12`로 개발자 패널을 열고 `Wait +1h` 확인

## 개발 빌드 검증

Unity가 `.csproj`를 생성한 뒤 로컬에서 C# 컴파일 검증을 실행할 수 있습니다.

```powershell
dotnet restore CheeseTama.csproj
dotnet build CheeseTama.csproj --no-restore
```

## 작업 폴더

- Unity에서 테스트하는 백업/작업 폴더: `C:\Users\user\Desktop\CheeseTama`
- GitHub 추적 저장소 폴더: `C:\Users\user\Documents\GitHub\CheeseTama`

Git 커밋 메시지는 한국어로 작성합니다.
