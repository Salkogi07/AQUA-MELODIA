# AQUA-MELODIA

Unity 6 (6000.3.12f1) 2D 픽셀 낚시 게임.

이 문서는 **기획자용 밸런스 가이드**입니다. 수치는 모두 Unity Inspector에서 바꿀 수 있고, 아래 공식은 현재 코드 기준입니다.

---

## 1. 낚시 흐름

```
캐스팅 → 입질 대기 → 입질(챔질) → 릴 미니게임 → 발악 패턴(선 그리기) → 획득
```

| 단계 | 규칙 | 조절 위치 |
|---|---|---|
| 입질 대기 | 찌가 **낚시 구역(물) 안**에 떨어져야 입질이 옵니다. `Bite Delay Range` 사이 무작위 초 후 물고기 추첨 | `FishingRod` → `Bite Delay Range` (Ocean: 7~12초) |
| 챔질 | 찌가 들어간 뒤 `Input Time Limit` 안에 좌클릭. 놓치면 다시 입질 대기 | `FishingRod` → `Input Time Limit` (Ocean: 1.2초) |
| 릴 미니게임 | 마우스 휠로 초록 범위를 움직여 물고기 아이콘을 범위 안에 유지 → 기력 0 만들기 | 아래 3장 |
| 발악 패턴 | 제한 시간 안에 점선을 따라 그리기. 완성도로 품질 결정 | 물고기의 `Escape Pattern Data` |

---

## 2. 물고기 출현 확률

물고기는 **① 등급 추첨 → ② 등급 안에서 어종 추첨** 2단계로 정해집니다.

### ① 등급 추첨 — 낚시 구역(`FishingZone`)의 `Grade Chances`
- 등급(Common / Rare / Epic / Unique / Legend)별 확률(%)을 입력합니다. **합계 100**을 권장합니다.
- 합계가 100보다 작으면 남는 확률은 **Common**으로 처리됩니다.
- ⚠️ 뽑힌 등급에 해당하는 물고기가 `Fish Spawn List`에 없으면 **입질이 오지 않습니다**. 확률을 준 등급에는 물고기를 반드시 1마리 이상 넣어 주세요.

### ② 어종 추첨 — `Fish Spawn List`의 `Weight`
- 같은 등급 물고기끼리 **가중치 비율**로 뽑습니다.
- 예: Common에 붕어 6, 고등어 3, 조개 1 → 각각 60%, 30%, 10%

### 미끼 보정 (`BaitDataSO`)
| 항목 | 효과 |
|---|---|
| `Preferred Fish List` → `Grade Chance Boost` | 대상 물고기가 속한 **등급의 출현율 +N%** |
| `Preferred Fish List` → `Weight Bonus` | 등급 안에서 그 물고기의 **가중치 +N** |
| `Region Grade Bonus List` | 특정 지역(`Target Region`)에서만 특정 등급 출현율 +N% |

- 보너스로 합계가 100%를 넘으면 **보너스를 받지 않은 등급의 확률이 비율대로 줄어듭니다**. 보너스 받은 등급은 유지됩니다.
- 정확한 최종 확률은 플레이 중 Console 로그(`[미끼 적용 후 등급별 최종 확률]`, `[물고기별 실제 당첨 확률]`)에서 확인할 수 있습니다. `FishingZone`의 `Enable Debug Log`를 켜면 출력됩니다.

---

## 3. 릴 미니게임 — 물고기 스탯 vs 낚싯대 스탯

스탯은 **물고기 스탯과 낚싯대 스탯이 1:1로 맞붙는 구조**입니다.

| 물고기 (`FishDataSO`) | 낚싯대 (`FishingRodDataSO`) | 결과 |
|---|---|---|
| `Strength` 힘 | `Rod Power` 강도 | 기력을 깎는 속도 |
| `Agility` 민첩 | `Rod Agility` 릴성 | 물고기 이동 속도 / 초록 범위 크기 / 스트레스 증가 |
| `Resistance` 저항 | `Rod Elasticity` 탄성 | 스트레스 증가·감소 속도 |
| `Max Stamina` 기력 | `Damage Rate Bonus` | 잡기까지 걸리는 시간 |
| — | `Sweet Spot Bonus` | 초록 범위 크기 |

### 속도 배율 (민첩 vs 릴성)
```
속도 배율 = 1 + (물고기 Agility − 낚싯대 Rod Agility) × 0.2     (최소 1, 최대 5)
```
- 낚싯대 릴성이 물고기 민첩 이상이면 배율 1(기본).
- 배율이 높을수록 **물고기가 빨리 움직이고**, **초록 범위가 좁아지고**, **놓쳤을 때 스트레스가 빨리 찹니다**.
- 예: 민첩 13 vs 릴성 8 → 1 + 5×0.2 = **2.0배** (물고기 2배 빠름, 초록 범위 절반)

### 초록 범위(판정 범위) 크기
```
초록 범위 너비(바 전체 대비) = (Sweet Spot Tolerance + Sweet Spot Bonus) × 2 ÷ 속도 배율
```
- 물고기 아이콘의 **중심**이 초록 범위 안에 있으면 성공 판정입니다.
- 예: 기본 0.05, 보너스 0, 배율 1 → 바의 **10%**. 보너스 0.05인 낚싯대 → **20%**.

### 기력 감소 (힘 vs 강도)
```
초당 기력 감소 = (Fish Damage Rate + Damage Rate Bonus) × (Rod Power ÷ 물고기 Strength)
                                                         └ 0.25 ~ 3배로 제한
```
- 예: Damage Rate 10, 강도 5 vs 힘 10 → 10 × 0.5 = 초당 5 → 기력 100짜리는 범위 안에 **20초** 유지해야 함.

### 스트레스 (저항 vs 탄성)
- 범위 **밖**: 초당 `Stress Increase Rate × 속도 배율 × 증가 보정` 만큼 상승
- 범위 **안**: 초당 `Stress Decrease Rate × 감소 보정` 만큼 하락
- **스트레스 100% → 줄 끊김(실패)**

| 상황 | 증가 보정 | 감소 보정 |
|---|---|---|
| 저항 > 탄성 (차이 d) | 1 + d × 0.08 | 1 |
| 탄성 ≥ 저항 (차이 a) | 1 − a × 0.04 (최소 0.5) | 1 + a × 0.1 |

### 제한 시간
- 기본 **40초**. 넘기면 스트레스가 저절로 오르고, 시간이 지날수록 더 빨리 오릅니다(초과 시간 × 0.08 /초).

### 공통 수치 (`Ocean` 씬 → `FishingRod` 오브젝트)
| 항목 | 의미 | 현재값 |
|---|---|---|
| `Wheel Sensitivity` | 휠 1칸당 초록 범위 이동량 (바 대비) | 0.05 |
| `Reel Smooth Speed` | 초록 범위가 따라가는 부드러움 (높을수록 즉각적) | 12 |
| `Sweet Spot Tolerance` | 초록 범위 기본 반폭 | 0.05 |
| `Fish Damage Rate` | 기본 기력 감소 속도 | 10 |
| `Stress Increase Rate` / `Decrease Rate` | 기본 스트레스 증가/감소 | 0.2 / 0.3 |

> 낚싯대가 장착되지 않았을 때 기본값: 강도 10, 릴성 5, 탄성 10. 장착 낚싯대는 씬의 `PlayerFishingEquipment` → `Equipped Rod`에서 바꿉니다.

### 물고기 움직임 (`PatternDataSO`)
- `Target Position X`: 이동할 위치 (낚시 구역 중심 기준, ±`Max Move Range`가 바의 양 끝)
- `Move Duration`: 이동 시간(초, 속도 배율로 나눠짐) / `Wait Time`: 도착 후 대기(초)
- `Loop Pattern`: 끝나면 처음부터 반복
- Inspector에서 민첩/릴성 값을 넣어 속도 변화를 미리 볼 수 있습니다.

---

## 4. 발악 패턴과 품질

기력이 0이 되면 점선 패턴이 나타나고, `Time Limit` 안에 따라 그립니다. 잉크량은 패턴 길이 × `Ink Buffer Multiplier`(1.2 = 20% 여유)입니다.

| 완성도 | 결과 |
|---|---|
| 95% 이상 | **S 품질** |
| 80% 이상 | **A 품질** |
| 70% 이상 | **B 품질** |
| 70% 미만 | 도망 (실패) |

- 물고기에 `Escape Pattern Data`가 없으면 발악 패턴 없이 **B 품질로 자동 성공**합니다.
- 물고기 길이는 `Min Length` ~ `Max Length` 사이에서 무작위로 정해집니다.

---

## 5. 물고기 테스트 씬

확률과 상관없이 **원하는 물고기만** 등장시켜 밸런스를 확인하는 씬입니다.

1. `Assets/Scenes/FishSelectTest.unity`를 엽니다.
2. Hierarchy에서 `FishTestOverride` 오브젝트를 선택합니다.
3. `Test Fish List`에 테스트할 물고기 데이터(`Assets/Data/...` 의 FishDataSO)를 끌어다 넣습니다.
   - 여러 마리를 넣으면 그중 무작위로 하나가 나옵니다.
   - 비워두면 원래 확률대로 추첨합니다.
4. 플레이 → 찌를 물에 던지면 지정한 물고기만 입질합니다. Console에 `[테스트 물고기 지정]` 로그가 뜹니다.

**팁**
- 낚싯대 비교: 씬의 `PlayerFishingEquipment` → `Equipped Rod`를 바꿔가며 테스트하세요.
- 입질 대기가 길면 `FishingRod` → `Bite Delay Range`를 테스트 씬에서만 1~2초로 줄이세요.
- 테스트 씬은 Ocean 씬의 복사본이라 Ocean을 수정해도 자동 반영되지 않습니다.
- Play 모드에서 바꾼 Inspector 값은 Play를 끄면 되돌아갑니다. **ScriptableObject(물고기·낚싯대 데이터) 값은 Play 중 바꿔도 저장되니** 주의하세요.
