# CheeseTama: The Milkroom

`CheeseTama: The Milkroom`(치즈타마: 밀크룸)은 작은 치즈 생명체를 돌보고 성장시키는 PC 우선 감성 캐주얼 육성 게임입니다.

포근한 3D 밀크룸에서 상태를 살피고, 우유와 간식을 주고, 함께 놀거나 방을 청소하며 일상의 기록을 쌓아 갑니다. 이 저장소에는 Unity에서 실행할 수 있는 개발 중인 프로토타입이 공개되어 있습니다.

## 현재 구현

- 우유주기, 요리하기, 간식 먹이기, 놀아주기, 청소하기, 휴식하기로 구성된 돌봄 루프
- 포만감, 기분, 청결, 졸림, 건강, 애정과 성장 상태 관리
- 성장 단계별 3D 캐릭터와 행동·상태 반응
- 우유 선택, 요리, 간식가방과 보관 수량 관리
- 발견 기록을 확인하고 보상을 받는 도감 화면
- 아침, 오후, 밤, 비 분위기의 밀크룸 테마
- 볼륨, 음소거, 전체화면, UI 크기와 프레임 제한 설정
- 로컬 저장, 불러오기와 진행 초기화
- 여러 화면 비율을 고려한 카메라 프레이밍

## 요구 환경

- Unity Hub
- Unity `6000.0.78f1`
- 패키지 설치를 위한 인터넷 연결

처음 프로젝트를 열면 Unity Package Manager가 필요한 패키지를 내려받고 에셋을 임포트합니다. 환경에 따라 몇 분 정도 걸릴 수 있습니다.

## 실행 방법

1. 저장소를 복제하거나 ZIP 파일로 내려받습니다.
2. Unity Hub에서 저장소의 프로젝트 폴더를 엽니다.
3. 패키지 설치와 에셋 임포트가 끝날 때까지 기다립니다.
4. Unity 메뉴에서 `CheeseTama > 시작 씬 빌드`를 실행합니다.
5. `Assets/_Project/Scenes/Milkroom.unity`를 엽니다.
6. Unity Editor의 Play 버튼을 누릅니다.

## 조작과 플레이 흐름

마우스로 상단 메뉴와 하단 행동 버튼을 선택합니다. 숫자 키 `1`부터 `6`까지는 여섯 가지 돌봄 행동의 단축키입니다.

1. 밀크룸에서 캐릭터의 상태를 확인합니다.
2. 필요한 돌봄 행동을 선택합니다.
3. 상태 변화와 캐릭터 반응을 확인합니다.
4. 새로 발견한 기록은 도감에서 살펴봅니다.
5. 꾸미기와 설정에서 방 분위기와 플레이 환경을 조정합니다.

## 프로젝트 구조

```text
Assets/_Project/Scripts       핵심 게임 로직과 UI 코드
Assets/_Project/Scenes        Boot, Milkroom, Collection, Debug 씬
Assets/_Project/Resources     런타임 데이터와 UI 리소스
Assets/Characters             캐릭터 모델과 성장 에셋
Assets/Environments           밀크룸 환경과 소품
Packages                      Unity 패키지 구성
ProjectSettings               Unity 프로젝트 설정
```

## 기술 스택

- Unity 6 `6000.0.78f1`
- Universal Render Pipeline `17.0.4`
- uGUI `2.0.0`
- Input System `1.19.0`
- glTFast `6.19.0`
- Unity Test Framework `1.6.0`

## 개발 상태

현재 프로젝트는 완성된 배포판이 아닌 개발 중인 프로토타입입니다. 핵심 플레이 흐름은 Unity Editor에서 확인할 수 있으며, 게임 밸런스와 UI, 아트 및 연출은 변경될 수 있습니다.

## 라이선스

라이선스 조건은 [LICENSE](LICENSE)를 확인해 주세요.
