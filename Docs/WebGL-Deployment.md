# CheeseTama 개발자용 WebGL 릴리스 운영 안내

CheeseTama 웹게임의 GitHub Pages 주소는 [https://cheezcyj.github.io/CheeseTama/play/](https://cheezcyj.github.io/CheeseTama/play/)다. 사이트 내부 경로는 `/play/`이며, GitHub 프로젝트 페이지의 `/CheeseTama/` 접두사가 앞에 붙는다. 주소가 바뀌면 브라우저 저장 영역이 달라질 수 있으므로 출시 후에는 이 주소를 유지한다.

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

정적 호스트의 게시 루트를 `site/`에 맞춘다. 수동 workflow는 빌드와 배포 계약 검증이 성공한 경우에만 이 사이트를 GitHub Pages로 게시한다. 별도의 빌드 파일 커밋이나 배포 브랜치는 만들지 않는다.

검증기의 `-PublicBasePath /play/`는 아티팩트 내부 경로다. 이 값을 `/CheeseTama/play/`로 바꾸지 않는다. 시작 페이지·Build·TemplateData 주소는 상대경로를 사용하므로 프로젝트 페이지 접두사를 유지한다.

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

다음은 `_headers`를 지원하는 호스트에 요청하는 정책이다. GitHub Pages는 이 파일을 사용자 응답 헤더 설정으로 적용하지 않으므로 실제 `Cache-Control` 응답을 별도로 확인해야 한다.

- `/`, `/play/`, `/play/index.html`: `no-cache, no-store, must-revalidate`
- `/play/Build/*`: 배포가 바뀔 때 같은 파일명이 갱신되므로 매번 재검증
- `/play/TemplateData/*`: 1시간 후 재검증
- `/play/ThirdPartyNotices/*`: 1일 후 재검증

위 정책이 적용되는 호스트에서는 HTML과 Build 파일이 최신 배포를 재검증한다. GitHub Pages에서는 게시 직후 브라우저·CDN 캐시 때문에 갱신이 늦게 보일 수 있다. 게임 데이터 자체의 재다운로드 절감은 Unity의 IndexedDB 데이터 캐시를 사용하며, 추후 파일명 해시를 도입한 경우에만 Build 파일을 `immutable`로 전환한다.

## CI 사용

`.github/workflows/webgl-release.yml`은 저장소 비밀을 준비한 관리자가 `main`에서 수동 실행할 때만 다음 작업을 수행한다. 일반 push만으로 자동 배포되지는 않는다.

1. Unity WebGL 릴리스 클린 빌드
2. `/play/` 구조로 정적 사이트 조립
3. 배포 계약·민감정보·게시 금지 파일 검사
4. `cheesetama-webgl-site` 아티팩트 업로드
5. Pages 전용 아티팩트 업로드와 `github-pages` 환경 게시

워크플로 실행 전 GitHub Actions 저장소 비밀에 `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`를 등록한다. 문서나 워크플로 파일에는 실제 값을 기록하지 않는다.

저장소 Settings → Pages에서 Source를 **GitHub Actions**로 설정한다. Actions에서 **WebGL 빌드 및 GitHub Pages 배포** → **Run workflow** → `main`을 선택한다. 빌드 작업에는 읽기 권한만 부여하고, 배포 작업에만 `pages: write`와 `id-token: write`를 부여한다. 다른 브랜치에서는 빌드·배포 작업을 건너뛴다.

전체 workflow가 성공한 뒤 위 공개 주소의 게임 진입과 저장 복원을 확인한다. 아티팩트 업로드 성공만으로 실제 Pages 게시 성공을 판단하지 않는다.

로컬 반복 검증은 Unity 메뉴의 `CheeseTama > 검증 > WebGL 릴리스 빌드`를 사용한다. 이 메뉴는 이전 IL2CPP·플레이어 캐시를 재사용하는 증분 릴리스다. 게시 직전 독립 검증은 `WebGL 릴리스 클린 빌드 (최종 검증)`을 사용하며, CI도 같은 클린 빌드 진입점을 호출한다.

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
