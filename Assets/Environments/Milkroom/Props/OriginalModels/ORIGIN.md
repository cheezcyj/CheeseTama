# 밀크룸 직접 제작 소품

2026-09-06 제작한 원본 소품 7종의 Unity 사용본이다.

| 모델 | 기능 프리팹 | 삼각형 |
| --- | --- | ---: |
| CozyChair | CozyChair | 7,606 |
| Fridge | Fridge | 9,968 |
| Window | Window | 23,780 |
| MilkShelf | MilkShelf | 19,884 |
| MilkCabinet | DresserTable | 15,380 |
| Chalkboard | Chalkboard | 7,552 |
| Rug | Rug | 6,240 |

메시는 Blender 5.2.1에서 프로젝트용 파라메트릭 소스로 직접 제작했다. 기존 소품의 메시·텍스처나 외부 다운로드 모델을 입력으로 사용하지 않았다. BaseColor와 Roughness는 직접 작성한 재질 팔레트와 수학적 패턴으로 생성했다. 실제 Blender exporter 정보는 원본 FBX에 유지한다.

- `<모델>.fbx`: 제작 원본에서 내보낸 파일. 임포터의 실제 회전·단위 변환을 갖는다.
- `<모델>_Mesh.asset`: 위 FBX의 실제 임포트 행렬을 적용한 미터 단위 Y-up 게임 메시. 정면은 +Z, 최저점 Y=0이다.
- `BaseColor.png`: sRGB 색상 atlas.
- `Roughness.png`: 선형 거칠기 atlas.
- `MetallicSmoothness.png`: Unity용 파생 텍스처. RGB=0, alpha=1−Roughness.R이며 원본 PNG는 변경하지 않았다.
- `<모델>.mat`: 활성 렌더 파이프라인용 재질. 현재 Built-in에서는 Standard를 사용한다. Window는 기존 프로젝트의 비조명 Source Color shader 정책을 유지한다.

각 프리팹은 단일 메시·단일 외부 재질을 사용한다. 의자에는 수건이나 별도 보정용 쿠션이 없고, 치즈 베개가 새 모델 안에 포함된다. MilkCabinet의 기존 기능 이름인 DresserTable은 요리 메뉴 연결을 위해 보존한다. Rug는 v002로 지름 2.20m·전체 두께 0.06m이며, 중앙 교차 실선과 고주파 직조 텍스처 패턴을 제거했다. 캐릭터가 놓이는 중앙 면 높이는 바닥에서 약 0.05451m다.

`OriginalMilkroomPropAssets`가 소스 변환·재질 갱신을 담당한다. 메시와 material의 새 GUID는 첫 생성 뒤 재실행 시 유지하며, 기능 프리팹 GUID와 장면의 renderer 식별자도 보존한다. 원본 UV는 색 역할별 공유 atlas라 고유 lightmap UV가 아니다. 문구 변경·문 열기 애니메이션·투명 창문 효과는 이 정적 모델 교체에 포함하지 않는다.

이 기록은 실제 제작 과정과 입력을 설명한다. 모든 기존 디자인과의 비유사성이나 법적 독점 권리를 보증하거나 별도의 공개 배포 승인을 의미하지 않는다.
