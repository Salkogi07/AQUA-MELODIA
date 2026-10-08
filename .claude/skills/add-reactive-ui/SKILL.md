---
name: add-reactive-ui
description: Use when adding a new fishing UI element (gauge, popup, panel) that reflects FishingRod state in AQUA-MELODIA. Covers the R3 ReactiveProperty + Presenter/View pattern used under Assets/Scripts/UI.
---

# Add Reactive UI (R3 MVP)

States never touch UI directly. A state writes a value on `FishingRod` → a Presenter subscribes → a passive View renders it.

Reference implementations: `Assets/Scripts/UI/FishingCast/` (minimal), `Assets/Scripts/UI/FishingMiniGame/` (several bindings plus `Observable.CombineLatest`).

## Checklist

1. **Reuse first.** Check the existing `ReactiveProperty` fields in `Assets/Scripts/FishingSystem/FishState/FishingRod.cs` (`IsMiniGameActive`, `CastPower`, `LineStress`, `ShowcaseFish`, …). Only add a new one if nothing fits.

2. **Model (`FishingRod.cs`)**: add the property next to the others:
   ```csharp
   public ReactiveProperty<float> MyValue { get; private set; } = new(0f);
   ```
   Then add `MyValue.Dispose();` to `FishingRod.OnDestroy()`. **Don't skip this.**

3. **Writer (state)**: set `fishingRod.MyValue.Value = ...` in the relevant `FishingState` (`Enter`/`Update`/`Exit`). If the UI has an on/off flag, set it to `true` in `Enter` and reset it to `false` in `Exit`.

4. **View**: `Assets/Scripts/UI/<Feature>/<Feature>UIView.cs`, in namespace `FishingSystem.UI.<Feature>`.
   - `[SerializeField] private` references to UI objects, null-checked before use.
   - Only public `SetActive(bool)` / `UpdateXxx(value)` methods. No R3 and no game logic.

5. **Presenter**: `<Feature>UIPresenter.cs`, using the same shape as the existing ones:
   ```csharp
   [SerializeField] private <Feature>UIView view;
   [SerializeField] private FishingRod model;

   private void Start() { if (model != null) SetPlayerModel(model); }

   public void SetPlayerModel(FishingRod model)
   {
       this.model = model;
       this.model.MyValue.Subscribe(v => view.UpdateMyValue(v)).AddTo(this);
   }
   ```
   Every subscription must end with `.AddTo(this)`. For values derived from several properties, use `Observable.CombineLatest`.

6. **Scene wiring (tell the user, since it can't be done from code)**: add the Presenter and View to the UI GameObject in the relevant scene (usually `Ocean.unity`), then assign the `view` and `model` (the `FishingRod`) fields in the Inspector.

## Common mistakes
- Forgetting `Dispose` in `OnDestroy`, or forgetting `.AddTo(this)` on a subscription.
- Setting UI elements directly from a state or from `FishingRod`.
- Forgetting the Exit reset, so the UI stays visible after the state changes.
