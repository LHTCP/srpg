# 스킬 메이커 개선 플랜

> 상태: **설계 초안** | 대상 파일: `SrpSkillData.cs`, `SrpSkillMakerController.cs`, `SrpSkills.cs`, `SrpDefaultSkills.cs`

---

## 0. 현황 요약

### 현재 데이터 구조

```
SrpSkillData
├── id, displayName, description
├── skillType      (Active / Passive)
├── trigger        (OnActivate / OnTurnStart / OnAttackHit / OnTakeDamage)
├── targetType     (None / Self / SingleEnemy / SingleAlly / AreaEnemy / AreaAlly)
├── range, areaSize, cooldown, endsActivation
└── effects: SrpSkillEffect[]
       ├── type      (Damage / Heal / BuffStat / DebuffStat / FrozenHeart / Cleave)
       ├── stat      ("self" / "hp" / "ap" / "attackPower" / "moveRange" / "attackRange" / "posture")
       ├── value     (int, 음수 허용)
       └── duration  (int, 현재 미사용)
```

### 스킬 메이커 UI에서 효과 한 줄의 현재 배치

```
[효과 유형  ▼]
[대상 스탯  ▼]
[값         ___]
[지속(턴)   ___]
```

---

## 1. 효과 행 가독성 개선

### 1-1. 문제

- 현재 읽는 순서: **값 → 효과 유형 → 대상 스탯 → 대상 유형(스킬 레벨)**. 네 군데를 왔다갔다해야 "이 효과가 무엇을 하는지" 파악된다.
- `stat = "self"`가 "**누구의** 스탯"인지 바로 보이지 않는다. `SrpSkills.ApplyEffects`에서 `FrozenHeart`는 `stat == "self"`이면 **시전자**에게 적용하지만, `BuffStat`/`DebuffStat`는 **대상(target)**에게 적용한다 — 같은 `"self"` 문자열이 완전히 다른 의미.

### 1-2. 개선안

**A) 효과 행 배치 순서 변경** — "한 줄 요약"을 자연어 어순으로 배열.

```
[대상 ▼] 의  [스탯 ▼] 에  [효과 유형 ▼]  [값 ___]  (지속 [___] 턴)
```

예시 렌더링: `대상에게 | HP | 피해 | 27 | (0턴)`

**B) 효과별 대상(EffectTarget) 필드를 신설**

`SrpSkillEffect`에 `effectTarget` enum을 추가해, 각 효과가 **스킬 대상(Target)**에게 적용되는지 **시전자(Self)**에게 적용되는지 명시한다. 기존 `stat == "self"` 해킹을 제거한다.

```csharp
public enum SrpEffectTarget { Target, Self }

public class SrpSkillEffect
{
    public SrpEffectTarget effectTarget;   // 신규
    public SrpEffectType type;
    public string stat;                    // "self" 값은 더 이상 사용하지 않음
    public int value;
    public int duration;
}
```

**C) 효과 요약 라벨 자동 생성** — 효과 행 상단에 한 줄 읽기 전용 텍스트를 표시한다.

```
"시전자의 FH +5"  또는  "대상에게 27 피해"
```

구현: `SrpSkillMakerController.CreateEffectRow()` 안에서 드롭다운/필드 값이 바뀔 때마다 라벨을 갱신하는 콜백을 건다.

### 1-3. 마이그레이션

기존 `stat == "self"`인 효과는 로드 시 `effectTarget = Self`로 변환, `stat`을 실제 대상 스탯(예: `"frozenHeart"`)으로 재매핑. `SrpDataIO` 로드 경로에 한 번만 실행되는 업그레이드 코드를 넣거나, `SrpSkillEffect`의 역직렬화 후처리에서 처리.

---

## 2. 음수 값 정책

### 2-1. 문제

`value` 필드에 음수를 넣으면 `Damage`가 회복이 되거나, `Heal`이 피해가 되는 등 **효과 유형의 의미가 뒤집힌다**. 이미 `BuffStat` / `DebuffStat`이 양/음 방향을 나누고 있으므로 값의 부호까지 자유로우면 이중 부정이 발생한다.

### 2-2. 추천: 값은 항상 양수(≥ 0), 방향은 효과 유형이 결정

| 효과 유형 | 방향 | 비고 |
|-----------|------|------|
| Damage | 음(피해) | value > 0이어야 의미 있음 |
| Heal | 양(회복) | value > 0 |
| BuffStat | 양(증가) | value > 0 |
| DebuffStat | 음(감소) | value > 0 — 내부에서 `-value` 적용 (이미 현재 코드가 이렇게 동작) |
| FrozenHeart | 양(증가) | 감소가 필요하면 별도 enum 또는 DebuffStat으로 |
| Cleave | 양(추가피해) | attackPower + value |

**구현 방법**:
- 스킬 메이커 `ApplyEffectsFromUi()`에서 `Mathf.Abs(value)` 강제.
- 입력 필드를 `IntegerNumber`에서 바꾸지 않되, 값 적용 시 `Mathf.Max(0, value)`로 클램프.
- 저장 시 음수면 0으로 보정하고 상태 바에 경고 표시.

### 2-3. 대안 (필요 시)

"감소형 FrozenHeart" 같은 게임 디자인이 나오면 `SrpEffectType.ReduceFH`를 추가하는 편이 음수 허용보다 안전하다.

---

## 3. 효과 트리거 타이밍 (개별 효과별)

### 3-1. 배경

현재 트리거(`SrpSkillTrigger`)는 **스킬 단위**. 한 스킬의 모든 효과가 동시에 발동한다. 요청: 개별 효과마다 "언제 발동하는지"를 설정할 수 있으면 좋겠다.

### 3-2. 설계

`SrpSkillEffect`에 **선택적** `effectTrigger` 필드를 추가한다.

```csharp
public class SrpSkillEffect
{
    public SrpEffectTarget effectTarget;
    public SrpEffectType type;
    public string stat;
    public int value;
    public int duration;
    public SrpSkillTrigger? effectTrigger;  // null이면 스킬의 trigger를 따름
}
```

- **`null` (기본)**: 스킬의 `trigger`와 동일하게 발동.
- **값이 있으면**: 해당 효과만 별도 타이밍에 발동.

`SrpSkills.ApplyEffects()`에서 효과별 트리거를 체크하도록 분기를 추가한다.

### 3-3. 메이커 UI

효과 행에 "트리거 오버라이드" 드롭다운을 추가한다. 기본값은 "스킬 트리거 따름"이고, 변경하면 해당 효과만 별도 타이밍으로 전환된다.

> **주의**: 아래 4절의 "가져오기한 효과"는 트리거를 오버라이드할 수 없다 (원본 스킬의 트리거를 따른다).

---

## 4. 스킬 조합 (기존 스킬을 추가 효과로 가져오기)

### 4-1. 컨셉

- 기존 스킬을 다른 스킬의 **추가 효과**로 참조할 수 있다.
- 참조 가능한 스킬의 조건: **효과 목록이 정확히 1개**이고, **"조합 가능" 플래그**가 켜져 있다.
- 가져온 효과의 값은 **오버라이드** 가능하며, 기본값은 원본 스킬의 값이다.

### 4-2. 용어 정의 (이름 후보)

| 후보 | 설명 |
|------|------|
| **부품 스킬 (Component Skill)** | 다른 스킬의 효과로 끼워 넣을 수 있는 단위 |
| **효과 모듈 (Effect Module)** | "모듈"이라 재사용 가능한 느낌 |
| **서브 효과 (Sub-Effect)** | 직관적이나 "효과의 효과"로 혼동 가능 |

→ 추천: **"부품 스킬"** — "이 스킬은 부품으로 사용 가능"이라는 토글 라벨이 자연스러움.

### 4-3. 데이터 구조 변경

```csharp
[Serializable]
public class SrpSkillData
{
    // ... 기존 필드 ...
    public bool isComponent;               // "부품 스킬로 사용 가능" 토글
    // effects 배열은 기존과 동일
}

[Serializable]
public class SrpSkillEffect
{
    // ... 기존 필드 ...
    public string linkedSkillId;           // null이면 직접 정의 효과, 값이 있으면 부품 스킬 참조
    public int? valueOverride;             // null이면 원본 값 사용
}
```

### 4-4. 규칙

| 규칙 | 상세 |
|------|------|
| 부품 등록 조건 | `effects.Length == 1`일 때만 `isComponent = true` 설정 가능 |
| 부품 해제 조건 | 다른 스킬이 이 스킬을 `linkedSkillId`로 참조 중이면 `isComponent`를 끌 수 없음 |
| 가져오기 | 효과 추가 시 "부품 스킬 가져오기" 버튼 → 부품 스킬 목록에서 선택 → 효과 행이 추가되고, `linkedSkillId`가 설정됨 |
| 오버라이드 | 가져온 효과 행에서 `value` 필드를 수정하면 `valueOverride`에 저장. 원본 변경 시 `valueOverride == null`인 효과만 따라감 |
| 트리거 | 가져온 효과는 **원본 부품 스킬의 트리거**를 따름. 개별 트리거 오버라이드 불가 |
| 순환 참조 방지 | 부품 스킬 자체는 `linkedSkillId`를 가질 수 없음 (부품의 부품 불가 — 1단계 참조만 허용) |

### 4-5. 메이커 UI 변경

```
효과 목록
├── [효과 추가]  [부품 스킬 가져오기]  [마지막 삭제]
│
├── 효과 #1 (직접 정의)
│   [대상 ▼] [스탯 ▼] [효과 유형 ▼] [값 ___] (지속 [___] 턴)
│   [트리거 오버라이드 ▼]  ← 직접 정의만 표시
│
├── 효과 #2 (부품: "심장 관통" 참조)
│   [읽기전용 요약: "시전자의 FH +5"]
│   [값 오버라이드 ___]  (비우면 원본값 사용)
│   ← 트리거 오버라이드 없음
```

### 4-6. 런타임 (`SrpSkills.cs`) 변경

`ApplyEffects()` 에서 `linkedSkillId`가 있으면:
1. `SkillLookup`에서 원본 스킬을 찾는다.
2. 원본의 `effects[0]`를 가져오되, `valueOverride`가 있으면 해당 값으로 교체한다.
3. 이후 기존 `switch(eff.type)` 로직을 그대로 탄다.

---

## 5. 옵션 & 추가 제안

### 5-A. `stat` 드롭다운 정리

현재 `StatOptions`에 `"self"`가 포함되어 있다. `effectTarget` 신설 후에는 `"self"`를 제거하고 실제 스탯만 남긴다:

```
"hp", "ap", "attackPower", "moveRange", "attackRange", "posture", "frozenHeart"
```

### 5-B. 효과 유형에 따른 스탯 자동 제한

모든 `stat`이 모든 `type`과 조합 가능할 필요는 없다. 예를 들어 `Damage`에 `stat` 선택은 무의미. `type` 선택 시 `stat` 드롭다운을 자동 필터링하거나 비활성화하면 혼동이 줄어든다.

| 효과 유형 | stat 필요 여부 |
|-----------|---------------|
| Damage | 불필요 (HP/AP 자동) |
| Heal | 불필요 (HP 자동) |
| BuffStat / DebuffStat | 필요 |
| FrozenHeart | 불필요 (FH 고정) |
| Cleave | 불필요 (HP 자동) |

### 5-C. 효과 유형 표시명 한글화

메이커 드롭다운에 영문 enum 이름 대신 한글 표시를 쓰면 인지 부담이 줄어든다.

| enum | 표시명 |
|------|--------|
| Damage | 피해 |
| Heal | 회복 |
| BuffStat | 버프 |
| DebuffStat | 디버프 |
| FrozenHeart | 빙결된 심장 |
| Cleave | 강타 |

### 5-D. 효과 접기/펼치기

효과가 많아지면 스크롤이 길어진다. 각 효과 행을 접을 수 있는 토글을 달면 편의성이 좋아진다. (우선순위 낮음)

### 5-E. "부품 스킬" 표시 분리

스킬 목록(왼쪽 패널)에서 부품 스킬은 `[C]` 태그를 붙여 일반 스킬과 시각적으로 구분한다.

```
[A] 강타
[P] 심장 관통 [C]
[P] 빙결 축복 [C]
```

---

## 6. 구현 단계 (추천 순서)

| 단계 | 작업 | 도메인 | 영향 파일 |
|------|------|--------|-----------|
| 1 | `SrpEffectTarget` enum 추가 + `SrpSkillEffect.effectTarget` 필드 | 데이터 | `SrpSkillData.cs` |
| 2 | `stat = "self"` 마이그레이션 + `StatOptions`에서 `"self"` 제거 | 데이터 | `SrpSkillData.cs`, `SrpDataIO.cs` |
| 3 | 음수 값 클램프 (`value ≥ 0` 강제) | 데이터 + 메이커 | `SrpSkillData.cs`, `SrpSkillMakerController.cs` |
| 4 | 효과 행 배치 순서 변경 + 요약 라벨 | 메이커 | `SrpSkillMakerController.cs` |
| 5 | 효과 유형별 stat 자동 필터링/비활성화 | 메이커 | `SrpSkillMakerController.cs` |
| 6 | enum 한글 표시명 | 메이커 | `SrpSkillMakerController.cs` |
| 7 | `SrpSkills.ApplyEffects()`에 `effectTarget` 반영 | 전투(데이터 인접) | `SrpSkills.cs` |
| 8 | 효과별 트리거 오버라이드 (`effectTrigger`) | 데이터 + 메이커 + 전투 | `SrpSkillData.cs`, `SrpSkillMakerController.cs`, `SrpSkills.cs` |
| 9 | `isComponent` 플래그 + `linkedSkillId` / `valueOverride` | 데이터 | `SrpSkillData.cs` |
| 10 | 부품 스킬 가져오기 UI + 해제 방지 로직 | 메이커 | `SrpSkillMakerController.cs` |
| 11 | 런타임에서 `linkedSkillId` 해석 | 전투 | `SrpSkills.cs` |
| 12 | (선택) 효과 접기/펼치기, 부품 태그 표시 | 메이커 | `SrpSkillMakerController.cs` |

---

## 7. 도메인 분리 (srpg-dispatch 기준)

| Task | 도메인 | 수정 파일 | 건드리지 말 것 |
|------|--------|-----------|---------------|
| Task A — 데이터 구조 | srpg-data | `SrpSkillData.cs`, `SrpDataIO.cs`, `SrpDefaultSkills.cs` | 메이커 UI, 전투 로직 |
| Task B — 메이커 UI | srpg-makers | `SrpSkillMakerController.cs` | 데이터 클래스 내부, 전투 로직 |
| Task C — 전투 런타임 | srpg-battle | `SrpSkills.cs` | 메이커 UI, 데이터 직렬화 |

> Task A → Task B, Task C 순서 (데이터 구조가 확정돼야 UI와 런타임이 쓸 수 있다). Task B와 C는 파일이 겹치지 않으므로 **병렬 가능**.
