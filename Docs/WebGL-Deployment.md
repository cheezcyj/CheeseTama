# CheeseTama WebGL 배포 계약

CheeseTama 웹게임의 공개 주소는 `/play/`로 고정한다. 주소가 바뀌면 브라우저가 서로 다른 저장 영역으로 인식할 수 있으므로 기존 출시 후에는 이 경로를 변경하지 않는다.

## 산출물 구조

CI가 생성하는 `cheesetama-webgl-site` 아티팩트는 다음 구조를 가진다.

```text
site/
├─ index.html              # /play/로 이동
├─ _headers                # 지원하는 정적 호스트용 헤더 규칙
├─ _redirects              # /play를 /play/로 정규화
└─ play/
   ├─ index.html
   ├─ Build/
   ├─ TemplateData/
   └─ ThirdPartyNotices/
```

정적 호스트의 게시 루트를 `site/`에 맞춘다. 실제 배포는 CI에 포함하지 않으며, 검증된 아티팩트를 선택한 호스트에 별도로 게시한다.

## MIME과 압축 계약

릴리스는 Gzip 압축과 브라우저 측 압축 해제 대체 경로를 함께 사용한다. `_headers`를 지원하는 호스트에서는 다음 응답 헤더를 적용한다.

| 파일 | Content-Type | Content-Encoding |
| --- | --- | --- |
| `*.framework.js.unityweb` | `application/javascript` | `gzip` |
| `*.wasm.unityweb` | `application/wasm` | `gzip` |
| `*.data.unityweb` | `application/octet-stream` | `gzip` |
| `*.loader.js` | `application/javascript; charset=utf-8` | 없음 |

호스트가 `_headers`를 해석하지 않더라도 압축 해제 대체 경로로 실행할 수 있다. 다만 공개 전에는 개발자 도구의 Network 패널에서 모든 Build 응답이 200이고, 압축 파일이 HTML 오류 페이지로 대체되지 않았는지 확인한다.

## 캐시 정책

- `/`, `/play/`, `/play/index.html`: `no-cache, no-store, must-revalidate`
- `/play/Build/*`: 배포가 바뀔 때 같은 파일명이 갱신되므로 매번 재검증
- `/play/TemplateData/*`: 1시간 후 재검증
- `/play/ThirdPartyNotices/*`: 1일 후 재검증

HTML과 Build 파일은 항상 최신 배포를 재검증한다. 게임 데이터 자체의 재다운로드 절감은 Unity의 IndexedDB 데이터 캐시를 사용하며, 추후 파일명 해시를 도입한 경우에만 Build 파일을 `immutable`로 전환한다.

## CI 사용

`.github/workflows/webgl-release.yml`은 저장소 비밀을 준비한 관리자가 수동 실행할 때만 다음 작업을 수행한다.

1. Unity WebGL 릴리스 빌드
2. `/play/` 구조로 정적 사이트 조립
3. 배포 계약·민감정보·게시 금지 파일 검사
4. `cheesetama-webgl-site` 아티팩트 업로드

워크플로 실행 전 GitHub Actions 저장소 비밀에 `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`를 등록한다. 문서나 워크플로 파일에는 실제 값을 기록하지 않는다.

## 공개 호스트 RC 체크

- 최신 데스크톱 Chrome, Edge, Firefox, Safari에서 첫 클릭 시작과 오디오 재생 확인
- `/play/` 재방문 후 진행도와 UI 설정 복원 확인
- 전체 화면 진입과 해제 확인
- 좁은 창, 고 DPI, 모바일 세로·가로 방향에서 화면 잘림 확인
- 로딩 실패 상황에서 한국어 오류 화면과 다시 시도 버튼 확인
- 30분 이상 실행 후 비정상적인 메모리 증가와 프레임 저하 확인
- 배포 전 아티팩트 검증 스크립트 재실행

```powershell
pwsh -NoProfile -File Tools/WebGL/Validate-WebGlRelease.ps1 -SiteRoot Artifacts/WebGlSite -PublicBasePath /play/
```
