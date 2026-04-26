[English](../../CONTRIBUTING.md) | [Tiếng Việt](../vi/CONTRIBUTING.md) | [中文](../zh/CONTRIBUTING.md) | [日本語](../ja/CONTRIBUTING.md) | [한국어](../ko/CONTRIBUTING.md)

# 🎥 Director Simulator: Scene Builder 기여하기

Unity Sandbox Director 커뮤니티에 오신 것을 환영합니다! 개발 환경 설정, 테스트 실행 및 PR 제출 방법은 다음과 같습니다.

---

## 🛠️ 설정
1. **Unity** – 버전 `2021.3` 이상 (URP 권장).
2. **Git** – 저장소를 복제(clone)하고 `main` 브랜치를 체크아웃합니다.
3. Unity에서 프로젝트를 엽니다. Unity가 필요한 패키지를 자동으로 가져옵니다.
4. 데모 씬 실행: `File → Open Scene → Assets/Scenes/Demo.unity`를 열고 Play를 누릅니다.

---

## 📁 저장소 구조
```
Assets/
  Scripts/MiniTimeline/        # 핵심 타임라인 시스템
  Scripts/Placement/           # 배치 에디터 프레임워크
  Scenes/                      # 데모 및 테스트 씬
  Resources/                   # ScriptableObjects, 프리팹
  Editor/                      # 에디터 확장 (프리팹 생성기, UI)
docs/                          # 기술 문서, GDD, 디자인 노트
.github/                       # Issue 및 PR 템플릿
```

---

## 🧪 테스트 및 검증
- 저장/불러오기를 확인하려면 Unity Editor에서 **Mini Timeline → Test Serialization**을 실행합니다.
- 배치 규칙을 확인하려면 **Placement System → Validate Rules**를 실행합니다.
- 커밋하기 전에 빌드 오류가 없는지 확인합니다.

---

## 📝 코딩 표준
- C#은 Unity 규칙을 따르며, 파일 이름은 PascalCase, 네임스페이스는 디렉토리 구조를 따릅니다.
- 주석은 간결하게 유지하고 공용 API에는 XML 문서를 권장합니다.
- 커밋 메시지 형식: `[System] 짧은 설명` 예: `[Timeline] 이벤트 스크러빙 지연 수정`.

---

## 🚀 Pull Request
1. `main`에서 새 브랜치를 만듭니다: `feature/your-feature-name` 또는 `fix/bug-description`.
2. 테스트를 작성하고 (가능한 경우) Unity 빌드가 성공적인지 확인합니다.
3. 템플릿(`.github/PULL_REQUEST_TEMPLATE.md`)을 사용하여 PR을 엽니다.
4. 리뷰어는 48시간 내에 답변할 것입니다. 답변이 없으면 `@maintainer`를 멘션하세요.

---

## 🎬 초보자용 이슈 (Good First Issues)
- `good-first-issue` 라벨은 초보자를 위해 우선 할당됩니다.
- 중복을 피하기 위해 시작하기 전에 "I'm working on this"라고 댓글을 남겨주세요.

---

Director Simulator를 더욱 개선하는 데 기여해 주셔서 감사합니다! 🌟