# CheeseTama: The Milkroom

작은 치즈타마 알을 돌보고, 우유와 간식을 챙겨주며, 말랑한 친구로 부화시켜 가는 감성 캐주얼 육성 게임 프로토타입입니다.

현재 저장소는 최종 게임이 아니라 Unity에서 핵심 루프를 빠르게 확인하기 위한 초기 개발 버전입니다.

## 현재 상태

- Unity 6 기반 프로토타입
- PC 우선 감성 육성 게임
- 저장/불러오기/초기화 가능한 로컬 세이브
- Milkroom, Collection, Debug 씬 자동 생성
- MIT License

## 현재 구현된 기능

### Milkroom

- 치즈타마 상태 표시
  - 레벨
  - 형태
  - 컨디션
  - 포만감
  - 기분
  - 청결도
  - 졸림
  - 건강
  - 애정도
  - 부화 진행도
  - 우유 성장도
- 케어 버튼
  - `Feed Milk`
  - `Star Milk`
  - `Snack`
  - `Play`
  - `Clean`
  - `Rest`
  - `Wait 1h`
  - `Save`
  - `Reload`
  - `Reset`
- `Basic Milk` 성장도 기록
- `Star Milk` 해금 및 성장도 기록
- 치즈 간식 케어 액션
- 시간 경과에 따른 상태 변화
- 케어 후 랜덤 이벤트 발생

### 치즈타마 비주얼

- 알 상태 표시
- 부화 후 `Soft CheeseTama` 형태 표시
- 케어 반응
  - 살짝 튀어오름
  - 색 반응
  - 부화 축하 반응
- 컨디션별 색상 변화
- 부화 후 컨디션별 표정 변화
  - cheerful
  - sleepy
  - hungry
  - messy
  - unwell
- 이벤트 발생 시 별도 색 반응과 작은 이벤트 표시

### Collection

- 우유 기록
- 성장 기록
- 이벤트 기록
- 진화 기록
- 숨겨진 도감 기록
- 누락된 우유 성장 기록 자동 보정

### Debug

빠른 테스트를 위한 개발용 씬입니다.

- 배고픔 상태 강제 적용
- 졸림 상태 강제 적용
- 지저분함 상태 강제 적용
- 아픔 상태 강제 적용
- 기분 좋은 상태 강제 적용
- 즉시 부화
- Star Milk 해금
- 이벤트 강제 발생
- 저장 데이터 초기화

## Unity에서 실행하기

1. Unity Hub에서 프로젝트 폴더를 엽니다.
2. Unity 상단 메뉴에서 `Assets > Refresh`를 누릅니다.
3. 컴파일이 끝나고 Console에 빨간 오류가 없는지 확인합니다.
4. Unity 상단 메뉴에서 `CheeseTama > Build Starter Scenes`를 누릅니다.
5. `Assets/_Project/Scenes/Milkroom.unity`를 엽니다.
6. Play 버튼을 누릅니다.

## 빠른 확인 루트

### 기본 케어 확인

1. `Milkroom` 씬에서 Play
2. `Feed Milk`, `Snack`, `Play`, `Clean`, `Rest` 버튼 클릭
3. 왼쪽 상태 패널의 수치 변화 확인
4. 치즈타마의 움직임과 색 반응 확인

### 부화 확인

1. `Debug` 씬으로 이동
2. `Hatch` 버튼 클릭
3. `Soft CheeseTama` 형태와 얼굴 표시 확인
4. `Hungry`, `Sleepy`, `Messy`, `Unwell`, `Cheerful` 버튼으로 표정 확인

### 도감 확인

1. `Milkroom` 또는 `Debug`에서 케어 행동 진행
2. `Collection` 씬으로 이동
3. Milk Records, Event Records, Evolution Records, Hidden Records 확인

## 개발용 빌드 검사

Unity가 `.csproj`를 생성한 뒤에는 터미널에서 C# 컴파일 검사를 실행할 수 있습니다.

```powershell
dotnet restore CheeseTama.csproj
dotnet build CheeseTama.csproj --no-restore
```

현재 개발 환경에서는 .NET SDK 10으로 빌드 검사를 사용합니다.

## 프로젝트 구조

```text
Assets/_Project/
  Art/                  아트 리소스 자리
  Audio/                오디오 리소스 자리
  Data/                 샘플 데이터와 ScriptableObject 자리
  Prefabs/              프리팹 자리
  Scenes/               Boot, Milkroom, Collection, Debug
  Scripts/
    Collections/        도감 저장 및 숨겨진 도감 로직
    Core/               GameManager, 씬 빌더, 런타임 부트스트랩
    Data/               데이터 정의
    Gameplay/           케어, 성장, 이벤트, 상태, 해금 로직
    Save/               로컬 저장 로직
    UI/                 Milkroom, Collection, Debug UI와 비주얼 컨트롤러
```

## 아직 개발 중인 것

- 최종 아트 에셋
- 사운드와 음악
- 정식 밸런싱
- 밀크룸 꾸미기
- 실제 아이템 인벤토리
- 정식 튜토리얼
- 배포용 빌드 설정

## 라이선스

이 프로젝트는 MIT License를 사용합니다. 자세한 내용은 `LICENSE` 파일을 확인하세요.
