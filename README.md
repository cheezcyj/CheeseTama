# CheeseTama: The Milkroom

CheeseTama는 우유로 자라는 작은 치즈 생명체를 돌보는 PC 우선 감성 육성 게임 프로토타입입니다. 플레이어는 Milkroom에 머물며 치즈타마의 상태를 살피고, 우유와 간식, 놀이, 청소, 휴식으로 성장과 도감 기록을 천천히 쌓아 갑니다.

현재 저장소는 최종 게임이 아니라 Unity에서 핵심 루프를 빠르게 검증하기 위한 개발 버전입니다.

## 현재 상태

- Unity 6 `6000.0.78f1` 기반 프로젝트
- MIT License
- Boot, Milkroom, Collection, Debug 씬 구성
- 로컬 JSON 저장/불러오기/초기화
- Milkroom 체류 시간, 일일 루틴, 초기 재화, 도감 기록 프로토타입
- v1.9/v2.1 기준 풀 3D 스타일라이즈드 툰 렌더링 밀크룸 디오라마와 활성 렌더 파이프라인에 맞춰 깨지지 않는 toon 머티리얼 기반 치즈타마 비주얼
- Unity 프로젝트 설정, 패키지 잠금 파일, Git 설정 파일 추적 반영

## 구현된 주요 기능

### Milkroom

- 치즈타마 상태 표시: 레벨, 형태, 배고픔, 기분, 청결, 졸림, 건강, 애정, 성숙도, 부화 진행도
- 상단 메뉴: `도감`, `꾸미기`, `설정`
- 하단 케어 버튼: `우유`, `조합`, `간식`, `놀이`, `청소`, `수면`
- 숫자키 `1~6`으로 하단 케어 버튼 실행
- 상태 메시지 전용 Message Bar
- Basic Milk 성장 기록
- Basic Milk Lv. 2 이후 Star Milk 해금 및 성장 기록
- 치즈 간식 케어 액션
- 케어 행동 후 자동 저장
- 오프라인/체류 시간 진행에 따른 상태 변화
- 5/10/20/30분 체류 보상
- Milk Coins, Milk Drops, Collection Fragments 초기 재화
- `Catch Drops` 우유방울 획득 프로토타입
- 케어 행동 기반 랜덤 이벤트와 이벤트 반응

### CheeseTama 비주얼

- Lv.1 알 상태와 부화 후 CheeseTama 형태 표시
- 노란 치즈 푸딩 몸체, 치즈 구멍, 볼터치, 큰 눈, 작은 팔다리, 성장 단계별 실루엣
- Lv.33 최종형 왕관 디테일
- 상태별 표정 변화: 기본, 행복, 배고픔, 졸림, 지저분함, 아픔, 놀람
- 케어 행동 시 부드러운 튀어오름, 스쿼시/스트레치, 색상 반응

### Collection

- 발견된 기록만 표시하는 도감 화면
- 우유 기록, 성장 기록, 이벤트 기록, 진화 기록
- 숨겨진 도감은 실제 해금 전까지 슬롯/이름/희귀도/조건/카테고리/카운트 노출 금지
- Basic Milk와 Star Milk 성장 기록 분리 및 누락 보정

### Settings

- Settings는 상단 `설정` 메뉴에서 열림
- Data Management:
  - `Save`
  - `Load`
  - `Reset`
- Reset은 `RESET`을 정확히 입력해야 버튼이 활성화됨
- Save/Load/Reset 버튼은 본문 텍스트와 겹치지 않도록 배치

### Debug / 개발자 패널

- Debug 씬에서 빠른 상태 조작과 이벤트 테스트 가능
- Milkroom에서는 `F12`로 개발자 패널 열기
- 개발자 패널 기능:
  - `Wait +1h`
  - `Debug Scene`
- 개발자 패널은 상태 수치 영역을 가리지 않도록 배치

## Unity에서 실행하기

1. Unity Hub에서 `C:\Users\user\Desktop\CheeseTama` 폴더를 엽니다.
2. Unity 상단 메뉴에서 `Assets > Refresh`를 누릅니다.
3. 컴파일이 끝나고 Console에 빨간 컴파일 에러가 없는지 확인합니다.
4. Unity 상단 메뉴에서 `CheeseTama > Build Starter Scenes`를 누릅니다.
5. `Assets/_Project/Scenes/Milkroom.unity`를 엽니다.
6. Play 버튼을 누릅니다.

## 빠른 확인 루트

### 기본 케어 루프

1. `Milkroom` 씬에서 Play
2. `우유`, `조합`, `간식`, `놀이`, `청소`, `수면` 클릭
3. 상태 수치, Message Bar, 치즈타마 반응 확인
4. 상단 `도감`을 열어 기록 반영 확인

### 설정/저장 확인

1. 상단 `설정` 클릭
2. `Save`, `Load` 버튼 클릭 후 메시지 확인
3. `Reset` 클릭
4. 입력칸에 `RESET` 입력 전에는 Reset 버튼이 잠겨 있는지 확인
5. `RESET` 입력 후 초기화 동작 확인

### 개발자 테스트

1. Milkroom에서 `F12` 입력
2. 개발자 패널이 상태 수치를 가리지 않는지 확인
3. `Wait +1h`로 시간 경과 테스트
4. `Debug Scene`으로 이동해 부화, 상태 프리셋, 이벤트 테스트

## 개발 빌드 검사

Unity가 `.csproj`를 생성한 뒤에는 터미널에서 C# 컴파일 검사를 실행할 수 있습니다.

```powershell
dotnet restore CheeseTama.csproj
dotnet build CheeseTama.csproj --no-restore
```

현재 개발 환경에서는 .NET SDK 10 계열로 검사를 진행했습니다.

## 프로젝트 구조

```text
Assets/_Project/
  Art/                  아트 방향 문서와 임시 리소스
  Audio/                오디오 리소스 자리
  Data/                 샘플 JSON, ScriptableObject 자리
  Docs/                 Codex 작업 지시/참고 문서
  Prefabs/              프리팹 자리
  Scenes/               Boot, Milkroom, Collection, Debug
  Scripts/
    Collections/        도감 저장/해금 로직
    Core/               GameManager, 런타임 부트스트랩, 씬 빌더
    Data/               데이터 정의
    Gameplay/           케어, 성장, 이벤트, 상태 진행
    Save/               로컬 저장/불러오기
    UI/                 Milkroom, Collection, Debug, Settings UI
```

## 작업 폴더 안내

- Unity에서 확인하는 백업/작업 폴더: `C:\Users\user\Desktop\CheeseTama`
- GitHub 추적 저장소 폴더: `C:\Users\user\Documents\GitHub\CheeseTama`
- 의미 있는 변경 후에는 작업 폴더 내용을 추적 저장소에 반영하고 한국어 커밋 메시지로 푸시합니다.

## 아직 개발 중인 것

- 최종 캐릭터 아트/모델 에셋
- 정식 Blend 패널과 레시피 UX
- 케어 버튼 및 상단 메뉴 아이콘
- Collection 카드형 UI 개선
- Sound, Display, Controls 설정 탭
- 첫 세션 튜토리얼
- 사운드/VFX/연출 폴리싱
- 배포용 빌드 설정

## License

This project uses the MIT License. See `LICENSE` for details.
