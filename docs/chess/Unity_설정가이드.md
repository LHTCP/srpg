# 🚀 Unity 체스 게임 빠른 설정 가이드

프로젝트 전체 문서 인덱스: [docs/README.md](../README.md)

## ⚡ 3분 만에 체스 게임 실행하기

### 1️⃣ 기본 오브젝트 생성

**1. 체스보드 오브젝트 만들기**
```
Hierarchy 창에서 우클릭
→ "Create Empty" 선택
→ 이름을 "ChessBoard"로 변경
→ Inspector에서 "Add Component" 클릭
→ "ChessBoard" 검색해서 추가
```

**2. 게임매니저 오브젝트 만들기**
```
Hierarchy 창에서 우클릭  
→ "Create Empty" 선택
→ 이름을 "ChessGameManager"로 변경
→ Inspector에서 "Add Component" 클릭
→ "ChessGameManager" 검색해서 추가
```

### 2️⃣ 컴포넌트 연결

**ChessGameManager 설정:**
```
1. Hierarchy에서 "ChessGameManager" 선택
2. Inspector에서 "Chess Board" 필드 확인
3. Hierarchy의 "ChessBoard" 오브젝트를 
   "Chess Board" 필드에 드래그해서 연결
```

### 3️⃣ 카메라 위치 조정

**Main Camera 설정:**
```
1. Hierarchy에서 "Main Camera" 선택
2. Inspector의 Transform에서:
   - Position: X=0, Y=10, Z=-5
   - Rotation: X=45, Y=0, Z=0
```

### 4️⃣ 게임 실행

```
✅ Play 버튼 클릭!
✅ 자동으로 8×8 체스판 생성됨
✅ 모든 체스 말들이 정확한 위치에 배치됨
```

---

## 🎮 조작 방법

### 기본 플레이
1. **🖱️ 클릭**: 현재 턴의 말 선택
2. **🟢 초록색 확인**: 이동 가능한 위치 표시
3. **🖱️ 클릭**: 원하는 위치로 이동
4. **🔄 자동**: 턴 변경

### 시각적 표시
- 🟡 **노란색**: 선택된 말
- 🟢 **초록색**: 이동 가능한 위치
- ⚪ **흰색/갈색**: 체스판 패턴

---

## 🔧 문제 해결

### 스크립트를 추가할 수 없어요
**해결책:**
```
1. Unity Console 열기 (Window → General → Console)
2. 빨간색 에러 메시지 확인
3. Assets → Refresh (Ctrl+R)
4. 필요시 Unity 재시작
```

### 말과 타일 위치가 안 맞아요
**해결책:**
```
✅ 이미 수정완료! 
   → transform.position 기반으로 정확한 위치 계산
```

### 말이 클릭되지 않아요
**해결책:**
```
1. 말 오브젝트에 Collider 확인
2. 카메라 Raycast 확인
3. CreatePrimitive는 자동으로 Collider 생성됨
```

---

## 📋 체크리스트

설정 완료 여부를 확인하세요:

- [ ] ChessBoard 오브젝트 생성 완료
- [ ] ChessGameManager 오브젝트 생성 완료  
- [ ] ChessBoard와 GameManager 연결 완료
- [ ] Main Camera 위치 조정 완료
- [ ] Console에 에러 없음
- [ ] Play 버튼으로 게임 실행 성공

---

## 🚀 다음 단계

게임이 실행되면:

1. **🎯 플레이 테스트**: 각 말들의 움직임 확인
2. **📖 완전 가이드**: [체스게임_완전가이드.md](체스게임_완전가이드.md) 읽어보기
3. **🎨 커스터마이징**: 색상, 크기 조정
4. **🔧 확장 기능**: UI, 사운드, 애니메이션 추가

---

**🎉 축하합니다! 체스 게임이 완성되었습니다!**

이제 친구와 함께 체스를 즐겨보세요! 🏆 
