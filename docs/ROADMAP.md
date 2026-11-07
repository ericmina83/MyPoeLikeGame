# Roadmap — 收尾 Reactive 重構

> **這是專案唯一的 TODO 來源。** 要做什麼、做到哪、下一步是什麼，都以這份為準。
> 做完一件事、新增一件事、改變順序時，**當輪就更新這份檔案**（規則見 `CLAUDE.md`
> 的「紀錄規則」）。為什麼這樣決定，寫在 [`decisions.md`](decisions.md)。
>
> 歷史：這份計畫最初存在 `~/.claude/plans/reactive-nibbling-dusk.md`（Claude 的本機計畫檔，
> 不在 repo 裡）。那個檔案後來從硬碟上消失，2026-10-19 從對話紀錄重建回來並移進 repo。
> 文中的 `檔案.cs:行號` 連結是當時的位置，asmdef 切分搬過檔案之後會對不上，以檔名搜尋為準。

## 目前狀態（2026-11-07）

**進行中：asmdef 切分（Phase 7.5）**——Task 1（`Core`）已完成。
下一步：先修正 spec／plan——`PlayerInputHandler` 與 `PlayerInput.cs` 要移到 Controller 層
（它依賴 `PlayerInput` 類別，留在 `Handlers.asmdef` 會編不過；見 `decisions.md`），再做 Task 2（`Handlers`）。
執行方式：不用 subagent，主對話在主資料夾逐 task 做，每個 task 停在未 add 等使用者 review。

- Spec：[`superpowers/specs/2026-08-30-asmdef-split-design.md`](superpowers/specs/2026-08-30-asmdef-split-design.md)
- Plan：[`superpowers/plans/2026-08-30-asmdef-split.md`](superpowers/plans/2026-08-30-asmdef-split.md)（7 個 task）

### 待決問題

- [x] ~~**subagent 執行時誰 commit**~~：2026-11-07 決定**不用 subagent**，由主對話在主資料夾
  逐 task 執行，每個 task 停在未 add 等使用者看過（見 `decisions.md`），這個問題不再存在。
- [x] ~~**`Packages/packages-lock.json` 的未 commit 變動**~~：`com.coplaydev.unity-mcp` 的 git hash
  從 `4ce7dd3…` 變成 `aa5fc63…`（MCP server 3.4.7 → 3.4.9），2026-11-07 單獨 commit 為 `64516db`。
- [x] ~~**21 個 commit 尚未 push**~~：2026-11-07 已 push 到 `7310942`。HTTPS 憑證已失效，
  這次是用 SSH 網址推送，之後 `origin` 已改為 SSH（`git@github.com:ericmina83/MyPoeLikeGame.git`）。

### 不屬於任何 phase 的待辦

- [ ] **Phase 1–7 補 spec 與測試**（2026-08-30 決定）：採用 superpowers 之後，已完成的階段要
  事後補上設計文件與測試。前提是 asmdef 切分完成、`Core.Tests` 存在。
- [ ] 中轉站的後續項目見 [`Handlers/readme.md`](../Assets/MyPoeLikeGame/Scripts/Handlers/readme.md) 的「已知待處理」
- [ ] 攻擊狀態機的後續項目見 [`AttackStateMachine.md`](../Assets/MyPoeLikeGame/Scripts/Handlers/AttackStateMachine.md) 的 TODO

## Context

> 以下是 2026-08-19 寫計畫時的現況描述，保留作為背景。

`damage_mechanic` 分支目前是一次重構做到一半的狀態。舊的全域事件匯流排 `Reactive.cs`（一條 `Subject<IEvent>`，用 `gameObjectId` 字串路由）已經刪掉，Handler 之間改成直接訂閱彼此的 Observable——這個方向是對的。但替換工作停在半途：

- 傷害鏈（`Projectile → DamageHandler → HpHandler`）、物件池、動畫層的接線全部還是註解狀態，共 7 處。
- 過程中混進四個會直接讓功能失效的錯誤（速度平方、攻擊輸入遺失、`LifetimeScope` 檔名、攻擊狀態機進不去 DRAW）。
- 角色 prefab 分裂成兩半，而且**兩半都不是可替換模型的形狀**（見下節）。
- R3 只裝了 NuGet 核心，缺 Unity 整合套件——現在無感，但之後任何時間類 operator 都會跑在背景執行緒。

這輪的目標是**把重構收尾**：讓專案回到可玩狀態，把事件鏈用一致的紀律接回來，把角色 prefab 重整成「邏輯 root + 可替換模型」，並且用「敵人也能驅動同一組 Handler」來驗證 Controller/Handler 這條界線真的成立。asmdef 切分與 VContainer 接管留到下一輪。

## 關鍵結構發現：兩個 prefab 都不是可替換模型的形狀

| | 實際結構 | 問題 |
|---|---|---|
| `Cube.prefab` | root 上有 MeshFilter + MeshRenderer + CharacterController + **12 個腳本**（4 Controllers + 8 Handlers，含 Hp/Damage） | 造型與邏輯焊在同一個 root |
| `Player.prefab` | **是 `Arissa.fbx` 的 Prefab Variant**（`!u!1001` + stripped GameObject），CharacterController 與 7 個 Handler 直接加在 Arissa 的 root 上 | 邏輯焊在特定模型身上；**Prefab Variant 無法解除與 base 的繼承關係**，所以它沒辦法被改造成純邏輯 root |

而且有三個設定以 override 的形式寄生在 `Player.prefab` 對 Arissa 的實例上，換模型會一起消失：

| Override | 值 | 換模型後的後果 |
|---|---|---|
| `m_Controller` | `PlayerController.controller` (`cf5791c7…`) | 動畫全沒了 |
| `m_Layer` | `3` = **Character** | **子彈打不中**——`Projectile.Awake` 的 `LayerMask.GetMask("Character")` 就是打這層 |
| `m_ApplyRootMotion` | `0` | root motion 設定沒了 |

### 目標結構

```
Prefabs/Characters/Player.prefab       ← 由 Cube.prefab 剝掉造型而來，layer = Character
  ├─ CharacterController
  ├─ 4 Controllers + 9 Handlers
  └─ Model/
       └─ Prefabs/Models/Arissa.prefab ← Variant of Arissa.fbx，承載 AnimatorController
```

兩個決定要點：

1. **從 `Cube.prefab` 出發，不是從 `Player.prefab`。** Cube 已經有完整且設定好的 12 個腳本，剝掉 mesh 就是現成的邏輯 root，完全不需要手動搬移序列化欄位。舊的 `Player.prefab` 因為是 variant 改造不了，做完後直接刪除。
2. **Character layer 設在邏輯 root，不是模型上。** 射線打的是 CharacterController（它本身就是 collider），設在 root 才能讓命中判定與模型無關；否則換一個模型角色就變成打不中。

這個結構也帶來一個程式碼後果：`AnimationHandler` 目前是 `[RequireComponent(typeof(Animator))]` + `GetComponent<Animator>()`，模型變成子物件後必須改成 `GetComponentInChildren<Animator>()` 並移除 `RequireComponent`，否則 Unity 會在 root 硬塞一個空的 Animator，AnimationHandler 會驅動錯的那一個。連帶地，`PlayerController.controller` 的 layer 名（Movement / Upper / Dodge）與參數名（Speed X/Y、Aiming、Dodge X/Y）就成為「任何可替換模型都必須滿足的契約」。

## 工作節奏

> **2026-08-30 起改用 superpowers 流程**：每個 phase 開工前先走 brainstorming → spec
> （`docs/superpowers/specs/`）→ plan（`docs/superpowers/plans/`）→ 執行（TDD）。
> 下面的五步與「commit 前停下來」規則仍然有效，套用在 plan 的每個 task 上。

每個 phase 固定走這五步，做完一個回報一次，你確認後再進下一個：

1. 改動（程式碼走 Edit，prefab / 場景走 `mcp__UnityMCP__manage_prefabs` / `manage_components` / `manage_scene`，不手改 YAML）
2. `mcp__UnityMCP__read_console` 確認沒有新增的 error / warning
3. 進 Play Mode 做該 phase 的驗收動作
4. **停在「未 add」狀態**，回報做了什麼，等你在本機看完所有變更
5. 你確認之後才 commit（沿用現有的 Conventional Commits 英文風格）

### 規則：commit 前一定要停下來給你 review

**每個 phase 結束時必須保持在未 `git add` 的狀態**，不可以自己 stage、更不可以自己 commit。
理由是你要能在本機用自己的工具（VSCode diff、Unity Inspector、實際玩一次）逐項確認，
而不是事後從 commit 裡回推我改了什麼。

- 我做完改動與驗證後，只回報「改了哪些檔案、為什麼、驗證結果如何」，然後停住。
- 你 review 完、明確說可以，我才 `git add` + `git commit`。
- 如果你要求改，就在同一個 phase 內修，改完再回到未 add 狀態等你重看。

**Windows 的例外情況：**涉及大小寫變更的檔名改動（例如 Phase 2），因為
`core.ignorecase=true` 會讓 `git status` 完全看不到差異。這種情況要先
`git config core.ignorecase false`，變更才會以「舊檔 deleted + 新檔 untracked」
的形式顯示出來供你 review。

**每個 phase 結束時專案都必須能編譯、能進 Play Mode 看到東西。** 任何 phase 如果做到一半發現會破壞這個前提，就停下來回報，不要硬推到下一個 phase 再修。

---

## Phase 總覽

| # | Phase | Commit 前綴 |
|---|---|---|
| **A. 地基** | | |
| 1 | 裝 R3 Unity 整合套件 ✅ `5489cea` `599a715` | `build:` |
| 2 | ~~修 `GameLifetimeScope` 檔名~~ → 改為移除 VContainer 範本 ✅ `c8b5c0c` | `chore:` |
| 3 | 修移動速度乘兩次 ✅ `dd3af97` | `fix(movement):` |
| 4 | 修攻擊輸入遺失與 DRAW 轉換 ✅ `173c9f8` | `fix(attack):` |
| 4b | 攻擊狀態機重構：多型 + 權重制 ✅ `7ae2fb0` `bbdc7e2`（計畫外新增） | `refactor(attack):` |
| 4c | 修 `PlayerInputHandler` 的 domain reload NRE（從 Phase 8 提前）✅ `ccd0b98` `d2d77ba` | `fix(input):` |
| 4d | 建立攻擊 sandbox（資料夾／場景／測試腳色 prefab）✅ `a647a7d` `c39177a` | `feat(sandbox):` |
| **B. 收斂介面（純重構，行為不變）** | | |
| 5 | `CharacterStateHandler` → `ReactiveProperty` ✅ `bbd4e38`（與 6 合併，做法改成 flag 板，見下方各節） | `refactor(state):` |
| 6 | 收斂三份 `canMove/canAim/canAttack` ✅ `bbd4e38`（收斂成 `IsBusy`，不是 `CanAct`） | `refactor(state):` |
| 7 | Handler 的 Subject 封裝 ✅ `1f41fc9` | `refactor:` |
| **7.5** | **asmdef 切分（從「下一輪」提前，TDD 的前提）** ⬅ **下一步**，spec `0318aa4`、plan `025ca57` | `build:` |
| 8 | Disposable 與 Subject 生命週期（`OnDisable` 防呆已在 `ccd0b98` 做完，剩 `OnDestroy`） | `refactor:` |
| **C. 重整 prefab 與接回鏈路** | | |
| 9 | 抽出 `Models/Arissa.prefab` | `refactor(prefab):` |
| 10 | 打造純邏輯 `Player.prefab`，場景換過去 | `refactor(prefab):` |
| 11 | AnimationHandler 接回訂閱 | `feat(animation):` |
| 12 | 抽出 `Models/Cube.prefab`，驗證換模型 | `chore(prefab):` |
| 13 | `HpHandler` → `ReactiveProperty` + 明確語意方法 | `refactor(hp):` |
| 14 | `IDamageable` 接回傷害鏈 | `feat(damage):` |
| 15 | 修物件池的回收與 key | `fix(pool):` |
| 16 | `Skill` 退回純資料 + `ProjectileSpawner` | `refactor(skill):` |
| **D. 驗收與清掃** | | |
| 17 | `EnemyAIController`（架構驗收） | `feat(enemy):` |
| 18 | 刪除空殼與未使用宣告 | `chore:` |
| 19 | 字串常數化與資料夾整理（`Controller/`→`Controllers/` 與 `Combat/`、`Pooling/` 分類已併入 7.5） | `chore:` |

---

# Stage A — 地基

先讓工具正確，再讓現況可跑。順序不要顛倒：在 R3 行為不可預期的情況下除錯會浪費大量時間。

## Phase 1 — 裝 R3 Unity 整合套件 ✅（`5489cea` `599a715`）

唯一一個「現在不做、以後會以完全看不出原因的形式爆炸」的項目，而且不動任何自己的程式碼。

- `Packages/manifest.json` 加入
  `"com.cysharp.r3": "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity"`
- 保留 `Assets/Packages/R3.1.2.9`（NuGet 核心 DLL），Unity 套件正是依賴它。若版本不相容，改用對應 R3 1.2.9 的 git tag。

**驗收**：Console 無錯誤；選單出現 `Window > Observable Tracker`。
**Commit**：`build: add R3 Unity integration package`

## Phase 2 — 移除 VContainer 範本 ✅ 已完成（`c8b5c0c`）

**原本的計畫是改檔名**（`GameLIfetimeScope.cs` → `GameLifetimeScope.cs`，因為 `LifetimeScope`
是 MonoBehaviour，Unity 要求檔名與類別名一致才掛得上去）。實作過程中決定**改為整組刪除**——
改名的唯一目的是讓那個檔案能被掛上,檔案要刪的話就不需要先修好它。改名未曾 commit。

刪除的三個檔案是 VContainer 官方 "Hello World" 範例的原文,彼此閉環、外部零引用、
零場景掛載:

- `GameLifetimeScope.cs` — 註冊另外兩個
- `GamePresenter.cs` — `ITickable`,每幀呼叫 `helloWorldService.Hello()`
- `HelloWorldService.cs` — 一個方法印 `Debug.Log("Hello world")`

保留空殼 scope 只會讓人猜它是幹嘛的。VContainer 要等到真的有跨場景服務需要它時再回來——
**第一個會是 Phase 16 的 `ProjectileSpawner`**，那時候的形狀由實際需求決定，而不是照抄教學。
`jp.hadashikick.vcontainer` 套件保留在 manifest 裡不動。

**驗收**：Console 無錯誤；Play Mode 進出乾淨。✅
**Commit**：`chore: remove the VContainer hello-world scaffolding`

## Phase 3 — 修移動速度乘兩次 ✅（`dd3af97`）

- [MovementHandler.cs:84](Assets/MyPoeLikeGame/Scripts/Handlers/MovementHandler.cs#L84) 改為 `characterController.SimpleMove(speed)`。
- `MovementEvent` 算出的 `speed` 已經含 `movementSpeedBasic * (1 + movementSpeedIncrement)`，`Update` 又乘了一次，實際速度是設定值的平方。

**驗收**：Play Mode 下移動速度符合 Inspector 的 `movementSpeedBasic`（1.75，不是 3.06）。
**Commit**：`fix(movement): apply speed multiplier once instead of twice`

## Phase 4 — 修攻擊輸入遺失與 DRAW 轉換 ✅（`173c9f8`）

兩個問題都讓攻擊完全不運作，一起修才驗收得了。

- [AttackController.cs:40-43](Assets/MyPoeLikeGame/Scripts/Controller/AttackController.cs#L40-L43)：`Attack(bool _)` 丟棄參數永遠送 `true`，改為傳遞實際值。
- [AttackHandler.cs:118-126](Assets/MyPoeLikeGame/Scripts/Handlers/AttackHandler.cs#L118-L126)：補上 `IDLE → DRAW`（`if (attacking) currState = AttackState.DRAW;`）。
- [AttackHandler.cs:176-192](Assets/MyPoeLikeGame/Scripts/Handlers/AttackHandler.cs#L176-L192)：`WITHDRAW` 分支的事件發送目前也是註解，改為和其他狀態一樣走 `attackEvent.OnNext`。

**驗收**：按住左鍵能走完 `DRAW → OVERDRAW`，放開後 `FIRE → WITHDRAW → IDLE`。用 `mcp__UnityMCP__unity_reflect` 觀察 `currState`。
**Commit**：`fix(attack): forward release input and enable draw transition`

## Phase 4c — 修 `PlayerInputHandler` 的 domain reload NRE ✅（`ccd0b98` `d2d77ba`）

**從 Phase 8 提前，因為它每次重新編譯都在噴，正在污染所有驗證。**

Unity 在每次 domain reload（重新編譯）時，會對場景中的 MonoBehaviour 呼叫
`OnDisable` / `OnEnable`，但**不會重跑 `Awake`**。而
[PlayerInputHandler.cs:22-24](Assets/MyPoeLikeGame/Scripts/Handlers/PlayerInputHandler.cs#L22-L24)
把 `inputActions = new()` 放在 `Awake`，`OnEnable`（第 29 行）與 `OnDisable`（第 43 行）
都直接使用它 → 每次編譯必定兩個 `NullReferenceException`。

已用「把 `_Temp` 整個移開再編譯」證實與測試腳本無關，是既有問題。

副作用比表面嚴重：`OnDisable` 崩在第一行 `inputActions.Disable()`，
導致後面的 `-=` 全部沒執行，**訂閱沒解除**。這很可能就是先前觀察到
「Player 的 `attacking` 會莫名被重置」的原因。

- `inputActions` 改為欄位初始化（`private PlayerInput inputActions = new();`），
  這樣 domain reload 之後它必然存在；或在 `OnEnable` 開頭補 null 建立。
- `OnDisable` 開頭加 null 防呆。

**驗收**：改任意一行程式碼觸發重新編譯，Console 不再出現這兩個 NRE。
**Commit**：`fix(input): survive domain reload without losing the action bindings`

## Phase 4d — 建立攻擊 sandbox ✅（`a647a7d` `c39177a`）

把臨時的 `Scripts/_Temp/AttackViz.cs` 換成一個像樣的測試環境：獨立資料夾、
獨立場景、做好的測試腳色 prefab。之後 Phase 5–8 全是「行為不得改變」的純重構，
需要一個能一眼看出四個狀態是否正常的地方。

### 目標結構

```text
Assets/MyPoeLikeGame/Sandbox/
├── Scenes/AttackSandbox.unity
├── Prefabs/
│   ├── AttackDummy.prefab            ← 邏輯 root（無造型）
│   ├── Models/DummyModel.prefab      ← 可替換的幾何造型
│   └── VizProjectile.prefab
├── Skills/
│   ├── Sandbox_Instant.asset         ← RangeCharge, 權重 1 : 0 : 1（瞬發）
│   └── Sandbox_Sustained.asset       ← RangeCharge, 權重 1 : 2 : 1（FIRE 有長度）
└── Scripts/
    ├── AttackDummyViz.cs             ← 狀態 → 視覺
    ├── DummyModelRig.cs              ← 造型 prefab 的контракт（body / weapon / orb）
    ├── AttackAutoDriver.cs           ← 自動按放，Inspector 可調
    └── AttackSandboxHud.cs           ← 畫面狀態讀數
```

### 關鍵決定

**① 一律做成 prefab，不在執行期 `AddComponent`。**
Handler 之間是**循環 `RequireComponent`**：
`AttackHandler → CharacterStateHandler → DodgeHandler → MovementHandler → CharacterStateHandler`。
執行期逐一 AddComponent 會讓某些 `OnEnable` 在相依元件備齊前就跑掉（臨時版就是踩到這個，
只能用「先 inactive 再啟用」硬繞）。編輯期建好的 prefab 沒有這個問題。

**② 測試腳色採用「邏輯 root + 可替換模型」結構**，提前驗證 Phase 9–12 要對
`Player.prefab` 做的事：

```text
AttackDummy (邏輯 root, layer = Character)
  ├─ CharacterController + CharacterStateHandler + DodgeHandler
  ├─ MovementHandler + AttackHandler
  ├─ AttackDummyViz + AttackAutoDriver
  └─ Model/  ← 槽位
       └─ DummyModel (帶 DummyModelRig)
```

`AttackDummyViz` 用 `GetComponentInChildren<DummyModelRig>()` 找造型，
**與 Phase 11 的 `AnimationHandler` 改用 `GetComponentInChildren<Animator>()` 是同一個模式**。
`DummyModelRig` 暴露 `body` / `weapon` / `chargeOrb` 三個 Transform，
就是「可替換造型必須滿足的契約」的簡化版。

**③ 不新增 `AttackType`，正式程式碼零改動。**
兩個測試 Skill 都用既有的 `RangeCharge`，差別只在權重資料。
`AttackHandler.skill` 是 `[SerializeField]`，在 Inspector 指派即可——**不需要任何反射**。

> **已知限制**：`CanOverdraw` 屬於 type 而非 `Skill`，所以**長按型連發**
> （`CanOverdraw => false`，射速恰好 `1/attackFrequency`）在新增型態之前
> 無法在 sandbox 展示。
>
> **連點型連發不受影響**——`AttackAutoDriver` 的 tap 模式已經在做：
> 按 0.2 秒放掉（DRAW 有 0.5 秒），DRAW 跑完時 `attacking` 已是 false → 直接 FIRE，
> 每次點擊就是一次完整循環。兩者的差別是射速由玩家手速決定、還是由資料寫死。
>
> 這是「兩個開關放在 type 上」的第一個實際代價，等真的要做近戰時再一併處理。

### 視覺對應（沿用臨時版已驗證可行的做法）

| 狀態 | 造型表現 |
|---|---|
| `IDLE` | 灰，武器停在待機位 |
| `DRAW` | 黃，武器依 `progress` 後拉 |
| `OVERDRAW` | 橘→紅（依 `chargeRatio`，過蓄轉紅並抖動），蓄力球依強度脹大 |
| `FIRE` | 白；瞬發型直接彈出，有長度型看得到揮擊過程 |
| `WITHDRAW` | 藍，武器依 `progress` 歸位 |

投射物在 `OnEnterFire` 生成，**大小與顏色由 `ChargeRatio` 決定**——這是蓄力值有正確
傳進行為的視覺證據，也是之後傷害縮放的雛形。

### 收尾

- 刪除 `Assets/MyPoeLikeGame/Scripts/_Temp/`（整個資料夾）
- `AttackSandbox.unity` **不要**加進 Build Settings

> **暫時無法用 asmdef 隔離**：asmdef 不能引用 `Assembly-CSharp`，而遊戲程式碼目前全在
> 裡面。等下一輪切好 asmdef 之後，Sandbox 才能獨立成組件並排除於 build 之外。
>
> **更新**：Phase 7.5 會給 Sandbox 自己的 asmdef，但「排除於 build 之外」刻意不在 7.5 範圍內。

**驗收**：開 `AttackSandbox.unity` 按 Play——兩個測試腳色自動循環攻擊，HUD 顯示
state / progress / chargeRatio；瞬發型的 `FIRE` 一幀就過，有長度型看得到揮擊；
過蓄時轉紅；投射物大小隨蓄力變化。**再把 `Model` 底下的造型換掉，確認邏輯照常運作**
（這是「可替換模型」結構的驗收）。
**Commit**：`feat(sandbox): add an attack state machine test scene`

---

# Stage B — 收斂介面

這四個 phase 都是**純重構、行為不變**，所以每一個的驗收標準都包含「手感與 Phase 4 結束時完全一致」。

## Phase 5 — `CharacterStateHandler` → `ReactiveProperty` ✅（與 Phase 6 合併為 `bbd4e38`）

> **實際做法與下面的原始計畫不同。** 討論後 `CharacterStateHandler` 改成**正交布林的 flag 板**，
> `CharacterState` enum 整個刪除；匯總值是 `IsBusy`（由 `isDodging` 用
> `ToReadOnlyReactiveProperty()` 推導），不是 `State` 或 `CanAct`。
> 設計理由見 [`Handlers/readme.md`](../Assets/MyPoeLikeGame/Scripts/Handlers/readme.md)
> 與 `CLAUDE.md` 的「架構通則」。以下保留原始計畫文字作為背景。

[CharacterStateHandler.cs:19-74](Assets/MyPoeLikeGame/Scripts/Handlers/CharacterStateHandler.cs#L19-L74) 目前是「`state` 欄位 + `BehaviorSubject` + `SetState` 手動去重」三件事——這正是 `ReactiveProperty<T>` 的定義。

```csharp
private readonly ReactiveProperty<CharacterState> state = new(CharacterState.IDLE);
public ReadOnlyReactiveProperty<CharacterState> State => state;
// 設值 state.Value = next（自帶去重）；讀當前值 state.CurrentValue
```

- 刪掉 `state` 私有欄位與 `SetState` 的去重判斷。
- 更新三個訂閱端（`MovementHandler`、`AimingHandler`、`AttackHandler`）的引用名稱。

**驗收**：翻滾時仍會中斷移動、瞄準與攻擊，行為不變。
**Commit**：`refactor(state): replace manual BehaviorSubject with ReactiveProperty`

## Phase 6 — 收斂三份 `canMove / canAim / canAttack` ✅（`bbd4e38`）

> **實際做法**：三個 Handler 各自訂閱 `stateHandler.IsBusy` 並自行詮釋（移動與瞄準直接擋掉、
> 攻擊是收尾）。沒有做成 `CanAct`——許可式命名會誘導人接出自我否定的線，見 `decisions.md`。

`MovementHandler`、`AimingHandler`、`AttackHandler` 各有一份幾乎相同的 `StateHandler(state) { canX = state != DODGE; }`。每加一個狀態（受擊硬直、暈眩、施法中）都要改三處，而且很容易漏。

- 在 `CharacterStateHandler` 提供單一來源，例如 `ReadOnlyReactiveProperty<bool> CanAct`。
- 三個 Handler 刪掉本地鏡像欄位與各自的 `StateHandler` 方法，`Update` 直接讀 `.CurrentValue`。

**驗收**：同 Phase 5。
**Commit**：`refactor(state): derive action gating from a single source`

## Phase 7 — Handler 的 Subject 封裝 ✅（`1f41fc9`）

> **實際做法的差異**：輸出埠命名是 `DodgeEvents` / `AttackEvents`（複數），不是下面寫的
> `DodgeEvent`——屬性不能和巢狀的 payload 類別同名（CS0102）。`MovementEvent` 改成建構子建一次。

七個 public Subject 全部收進去。任何人都能對 public Subject `OnNext`，更麻煩的是能 `Dispose` 它——一次就永久關掉那條流，而且不會有編譯錯誤。

**輸出埠**改唯讀（比照 [PlayerInputHandler.cs:98-106](Assets/MyPoeLikeGame/Scripts/Handlers/PlayerInputHandler.cs#L98-L106) 已經做對的寫法）：
```csharp
private readonly Subject<DodgeEvent> dodgeEvent = new();
public Observable<DodgeEvent> DodgeEvent => dodgeEvent.AsObservable();
```

**輸入埠**改成方法，不要暴露 Subject：`SetMoveInput(Vector2)`、`SetLookTarget(Vector2)`、`RequestDodge()`、`SetAttackInput(bool)`。四個 Controller 隨之改為呼叫方法。

**順手**：[MovementHandler.cs:34-41](Assets/MyPoeLikeGame/Scripts/Handlers/MovementHandler.cs#L34-L41) 的 `MovementEvent` 是 expression-bodied property，每次讀取都重建一條 Select 鏈；改成欄位建好一次或 `ToReadOnlyReactiveProperty()`。

**驗收**：同 Phase 5。
**Commit**：`refactor: encapsulate handler subjects behind observables`

## Phase 7.5 — asmdef 切分 ⬅ 下一步

**從「這輪明確不做」提前。** 2026-08-30 改用 superpowers 流程後，Phase 8 之後要走 TDD，
而 EditMode 測試需要明確的 asmdef 可以引用；同時也是「Phase 1–7 補測試」的前提。

選的是完整四層（`Core` / `Handlers` / `Controllers` / `Game`，加上 `Sandbox` 與 `Core.Tests`），
不是只切 `Core`——要順便讓「Handler 不可以認識 Controller」變成編譯錯誤。

- Spec：[`superpowers/specs/2026-08-30-asmdef-split-design.md`](superpowers/specs/2026-08-30-asmdef-split-design.md)
- Plan：[`superpowers/plans/2026-08-30-asmdef-split.md`](superpowers/plans/2026-08-30-asmdef-split.md)

做完之後，下面各 phase 提到的檔案路徑會變（例如 `Scripts/Pool.cs` → `Scripts/Core/Pooling/Pool.cs`）。

## Phase 8 — Disposable 與 Subject 生命週期

> **這不是理論上的邊界情況。**Unity 每次 domain reload（重新編譯）都會呼叫
> `OnDisable` / `OnEnable` 而不重跑 `Awake`，所以這類 NRE **每次改程式碼都會發生**。
> `PlayerInputHandler` 已在 Phase 4c 先修掉，這裡處理其餘的。

- [x] ~~全部 `OnDisable` 統一 `disposable?.Dispose(); disposable = null;`~~ 已在 `ccd0b98` 完成。
- [x] ~~同樣檢查所有在 `Awake` 建立、卻在 `OnEnable` / `OnDisable` 使用的欄位~~ 已在 `ccd0b98` 完成
  （結論：防呆就夠，`GetComponent` 維持放 `Awake`；前提是 `ScriptCompilationDuringPlay = 2`，見 `CLAUDE.md`）。
- [ ] 各 Handler 加 `OnDestroy`（目前只有 `CharacterStateHandler` 與 `PlayerInputHandler` 有） 釋放自己的 Subject / ReactiveProperty，讓訂閱者收得到 `OnCompleted`。當之後出現比角色活得更久的訂閱者（UI、成就、統計）時這件事就從潔癖變成必要。

**驗收**：在 Play Mode 中反覆 disable / enable 角色元件，Console 無 NRE。
**Commit**：`refactor: dispose subjects on destroy and guard disposal`

---

# Stage C — 重整 prefab 與接回鏈路

## Phase 9 — 抽出 `Models/Arissa.prefab`

純新增，不影響任何現況，所以先做。

- 新增資料夾 `Assets/MyPoeLikeGame/Prefabs/Models/`。
- 對 `Models/Arissa/Arissa.fbx` 建立 **Prefab Variant** → `Prefabs/Models/Arissa.prefab`。用 Variant 而非包裝 prefab，是為了保留與 FBX 的連結（重新匯入模型時會跟著更新），同時有地方存覆寫。
- 在這個 variant 上設定 `Animator.controller = PlayerController.controller`（`cf5791c7…`）與 `Apply Root Motion = false`——這兩項目前是寄生在舊 `Player.prefab` 的 override 裡。
- **不要**在這個 variant 上設 Character layer，那個屬於邏輯 root。

**驗收**：把 `Arissa.prefab` 單獨拖進場景，Animator 的 Controller 欄位已填好、Root Motion 已關閉。
**Commit**：`refactor(prefab): extract Arissa model into a prefab variant`

## Phase 10 — 打造純邏輯 `Player.prefab`，場景換過去

從 `Cube.prefab` 出發，因為它已經有完整且設定好的 12 個腳本（4 Controllers + 8 Handlers，含 Hp/Damage），剝掉造型就是現成的邏輯 root——**完全不需要手動搬移序列化欄位**。

1. 複製 `Cube.prefab` → `Prefabs/Characters/Player.prefab`（先刪掉舊的 variant 版 Player.prefab，因為 variant 改造不了）。
2. 移除新 Player.prefab root 上的 MeshFilter + MeshRenderer，以及造型子物件（`Outfit`、`Cube`）。保留 CharacterController 與 12 個腳本。
3. 確認 root 的 layer 是 **Character (3)**。
4. 在 root 底下建子物件 `Model`，把 `Arissa.prefab` 放進去。
5. `Sample.unity` 把現有的 Cube 實例換成新的 Player.prefab 實例；**Cinemachine 的 Follow / LookAt 目標要一併改指向新的 root**，否則攝影機會追空氣。

**驗收**：Play Mode 下角色是 Arissa 模型，能移動、能翻滾、能拉弓，攝影機正常跟隨（動畫還不會播，這是預期的）。
**Commit**：`refactor(prefab): rebuild Player.prefab as a logic root with swappable model`

## Phase 11 — AnimationHandler 接回訂閱

- 移除 `[RequireComponent(typeof(Animator))]`，`Awake` 改 `GetComponentInChildren<Animator>()`，刪掉 `gameObjectId` 欄位。
- [AnimationHandler.cs:93-113](Assets/MyPoeLikeGame/Scripts/Handlers/AnimationHandler.cs#L93-L113) 的註解改成直接訂閱兄弟 Handler 的 Observable，**不需要任何 id 過濾**：`movementHandler.MovementEvent`、`dodgeHandler.DodgeEvents`、`attackHandler.AttackEvents`（Phase 7 定案的名稱）。
- 把 `AnimationHandler` 掛到 Player.prefab 的 root 上。
- 把 Animator 契約（layer 名 Movement/Upper/Dodge、參數名 Speed X/Y、Aiming、Dodge X/Y）寫進 `Scripts/Handlers/readme.md`，作為可替換模型的規格。

**驗收**：Arissa 依移動方向播放走路混合、翻滾播 Dodge layer 且權重隨 percentage 變化、按住左鍵播 Upper layer 的 Draw / Overdraw / Withdraw。
**Commit**：`feat(animation): reconnect animation layer to handler observables`

## Phase 12 — 抽出 `Models/Cube.prefab`，驗證換模型

- 把原 `Cube.prefab` 的造型部分（MeshFilter / MeshRenderer / `Outfit` 子物件）整理成 `Prefabs/Models/Cube.prefab`，與 `Arissa.prefab` 並列；移除舊的 `Prefabs/Characters/Cube.prefab`。
- 這個 phase 的驗收就是**實際做一次替換**，證明整個結構成立。

**驗收**：把 Player.prefab 的 `Model` 子物件從 Arissa 換成 Cube → 角色仍能移動、翻滾、攻擊，**子彈仍打得中**（因為 Character layer 在邏輯 root 上，與模型無關）；動畫不播是正常的，因為 cube 沒有 Animator。驗證完換回 Arissa。
**Commit**：`chore(prefab): extract Cube into a swappable model prefab`

## Phase 13 — `HpHandler` → `ReactiveProperty` + 明確語意方法

**先建好接收端，下一個 phase 才接上發送端。**

- `hp` 改 `ReactiveProperty<int>`，對外 `ReadOnlyReactiveProperty<int> Hp` 與 `Observable<Unit> Depleted`。之後血條 UI 直接綁上去，不必再發一次事件。
- **方法拆成 `TakeDamage(int amount)` / `Heal(int amount)`**，不要靠 delta 正負號約定。目前 `DamageHandler` 送正值而 `HpHandler` 做 `hp += delta`，兩邊約定相反，接起來會變成被打反而加血。
- 刪掉 `gameObjectId` 欄位與註解掉的 `DeathEvent`。

### `HpHandler` 的職責邊界（重要，避免日後長成巨獸）

**`HpHandler` 只擁有那個數字。**它知道 HP 是多少、什麼時候歸零，
**但不決定歸零代表什麼**。事件命名成 `Depleted`（「歸零了」這個事實）
而不是 `Died`，就是要擋住「往這裡加死亡處理」的衝動。

死亡的耦合災難來自把「死亡」當成一段**程序**：

```csharp
if (hp <= 0) {                          // ← 絕對不要變成這樣
    animator.Play("Death");
    GameManager.Instance.OnPlayerDied();
    UIManager.Instance.ShowGameOver();
    LootSystem.Drop(transform.position);
}
```

同一個事實，不同實體的政策完全不同（玩家 → Game Over／復活；敵人 → 掉寶＋回物件池；
可破壞物 → 播破碎）。政策屬於**擁有這個實體的人**，不屬於 HP。

**這個專案已經有現成的答案：`CharacterStateHandler`。**

> **已依 Phase 5/6 的實際設計更新**（原文寫的是 `CharacterState` enum 加 `DYING`/`DEAD`、
> 透過 `CanAct` 擋住，那兩者都已不存在）。

死亡是 flag 板上的另一個旗標，與 `isDodging` 完全同構：

```text
HpHandler.Depleted ──訂閱──→ （某個 Handler）呼叫 stateHandler.ReportDead(true)
                                       ↓
                    isDead 旗標 ──推導──→ 匯總值（IsBusy 或新增的 IsIncapacitated）
                                       ↓
                    現有訂閱者（Movement / Aiming / Attack / Animation）自動處理
```

要守住 `CLAUDE.md` 架構通則：

- **② 所有邊指向共用板**：`CharacterStateHandler` 不能自己去訂閱 `HpHandler.Depleted`。
  要由 `HpHandler`（或專門的一層）主動 `ReportDead`。
- **③ 監聽匯總值**：消費端不要直接看 `isDead`，加進匯總值的推導式就好，那三個 Handler 一行都不用改。
- 「受擊硬直、暈眩、死亡」這類**外力**狀況，`Handlers/readme.md` 已預留 `IsIncapacitated`
  作為匯總值的名字，等第一個外力狀況出現時再加。

> 死亡是一段**狀態**不是一個瞬間：`HP 歸零 → DYING（播動畫、不能行動、還在場上）→ DEAD`。
> 混在一起會出現「敵人死亡動畫期間還能造成傷害」「掉寶掉在屍體倒下之前」這類 bug。
> 另外有物件池時，「死亡」通常等於**歸還池子**而不是 `Destroy`。

`DYING` / `DEAD` 這兩個狀態**這輪不加**——還沒有死亡動畫也沒有復活流程，現在設計是憑空想像。
Game Over／復活屬於**場次層**（之後的 `GameFlow` 訂閱玩家的 `Depleted` 來決定），不屬於角色。

> **PoE-like 的真正複雜度不在這裡。**「這次傷害到底扣多少」（抗性 → 護盾 → 減傷 → 無敵幀）
> 應該是一條獨立的傷害管線，`HpHandler` 要蠢到只會 `hp -= amount`。
> Phase 14 的 `DamageInfo` 就是那條管線的起點。

**驗收**：用 `unity_reflect` 觸發 `TakeDamage`，HP 正確下降、歸零時 `Depleted` 觸發。
**Commit**：`refactor(hp): expose hp as ReactiveProperty with explicit damage API`

## Phase 14 — `IDamageable` 接回傷害鏈

依照先前討論的分界：**Reactive 用於一對多的狀態擴散，一對一的互動用直接呼叫。**子彈打中角色是有明確對象的互動，射線已經拿到 Collider，不需要繞全域流再用 id 撈回來。

- 新增 `Scripts/Combat/IDamageable.cs`：`void TakeDamage(DamageInfo info);` 與 `DamageInfo { int value; Transform source; }`。
- `DamageHandler` 實作 `IDamageable`，轉交 `HpHandler.TakeDamage`。
- [Projectile.cs:49-96](Assets/MyPoeLikeGame/Scripts/Projectile.cs#L49-L96) 重寫 `Detect`：不再產出 `string[] gameObjectIds`，改為命中時 `hit.collider.TryGetComponent<IDamageable>(out var target)` 直接呼叫。連帶消掉每幀的 `List<string>` 配置與 `int → string` 轉換。
- [Damage.cs:17-39](Assets/MyPoeLikeGame/Scripts/Damage.cs#L17-L39) 的 `Detect(out ...)` 簽章隨之調整，刪掉 `Damage.DamageEvent`。
- 刪掉 `PostDamage.cs`（空殼，原本是這條鏈的佔位）。

**驗收**：射擊場景中的 Enemy，Enemy 的 HP 下降。
**Commit**：`feat(damage): reconnect damage pipeline via IDamageable`

## Phase 15 — 修物件池的回收與 key

- **`Pool.Fire` 的核心錯誤**（[Pool.cs:72-84](Assets/MyPoeLikeGame/Scripts/Pool.cs#L72-L84)）：`if (!pool.TryPop(out var go)) { go = Instantiate(...); pool.Push(go); }` 新建後立刻推回池又拿去用，導致同一顆子彈被重複借出。新建時**不要** Push。
- **池的 key 從 `sender`（Transform）改成 prefab**，否則同一角色的兩個技能共用一個 stack，會 pop 到錯型別的 prefab。
- **回收機制**：`PoolObject.Die()` 目前只做 `SetActive(false)`，從來沒有人送 RECYCLE，物件永遠回不了池。改成 `PoolObject` 持有所屬的池並在 `Die()` 時歸還。
- `Pool.Awake` 的 `DontDestroyOnLoad(this)` 對非根物件會警告——確認 Pool 是場景根物件，或改成 `DontDestroyOnLoad(gameObject)`。

**驗收**：連續射擊 30 秒，Hierarchy 裡 Projectile 實例數量穩定不成長；子彈飛到 maxDistance 消失後會在下次發射被重用。
**Commit**：`fix(pool): recycle projectiles and key pools by prefab`

## Phase 16 — 實作 `IProjectileSpawner` 並接上 `AttackContext`

> **範圍已縮小。**Phase 4b 的重構已經把 `Skill` 變成純資料、刪掉 `Fire()`、
> 並宣告好 `IProjectileSpawner` 介面與 `AttackContext.Spawner` 欄位。
> 這個 phase 只剩「做出實作並把它接上」。

- 讓 Phase 15 整理過的 `Pool` 實作 `MyPoeLikeGame.Combat.IProjectileSpawner`
  （或另做一個 `ProjectileSpawner` 包住它）。
- `AttackHandler` 加 `[SerializeField]` 持有它，`Context()` 不再傳 `null`。
- 這裡刻意先用 `SerializeField` 而非 DI：這輪不引入 VContainer，
  下一輪只要把 `[SerializeField]` 換成 `[Inject]`，遷移成本極低。
- `RangeChargeAttackBehaviour.OnEnterFire` 已經寫好呼叫，接上就會生效。

**驗收**：攻擊能正常發射子彈；`ChargeRatio` 有正確傳到 spawner（蓄力越久的箭可用它做威力縮放）。
**Commit**：`feat(combat): implement the projectile spawner and wire it into attacks`

---

# Stage D — 驗收與清掃

## Phase 17 — `EnemyAIController`（架構驗收）

場景裡的 Enemy 已經掛了 `MovementHandler`、`DodgeHandler`、`CharacterStateHandler`，但沒有任何東西驅動它。這是整個 Controller/Handler 分層唯一真正的測試。

- 新增 `Controllers/EnemyAIController.cs`：最小行為即可——朝玩家移動、進入距離後停下，透過 `movementHandler.SetMoveInput(...)` 驅動。
- Enemy 補上 `DamageHandler` + `HpHandler`，使其成為 `IDamageable`；確認 Enemy 在 Character layer 上。
- `Enemy.cs` 目前是空的 MonoBehaviour 樣板，改成有意義的內容或刪除。

**這個 phase 的驗收標準比功能更重要**：
> 寫 `EnemyAIController` 的過程中，**不可以修改任何一個 Handler**。如果發現必須改，就停下來記錄——那代表那條界線畫錯了，要回頭調整 Phase 7.5 切好的 asmdef 邊界，並記進 `decisions.md`。

**驗收**：敵人會朝玩家移動；射擊敵人會掉血；血量歸零觸發死亡。
**Commit**：`feat(enemy): add EnemyAIController driving shared handlers`

## Phase 18 — 刪除空殼與未使用宣告

- 刪除 `PoolObjectService.cs`（空殼）。`HelloWorldService` / `GamePresenter` / `GameLifetimeScope` 已在 Phase 2 一併刪除。
- 刪除未使用的宣告：`AimingHandler.AimingEvent`、`DodgeHandler.DodgeState`。
- `AttackHandler.attackEvent` 的 `[NonSerialized]` 是無效標註（Unity 本來就無法序列化 Subject），一併移除。

**驗收**：Console 無錯誤，功能不變。
**Commit**：`chore: remove dead scaffolding and unused declarations`

## Phase 19 — 字串常數化與資料夾整理

- `AnimationHandler` 的動畫參數與 state 名改成 `static readonly int` + `Animator.StringToHash`，改名時才不會靜默失效。
- `Projectile` 的 `LayerMask.GetMask("Character")` 改 `[SerializeField] LayerMask`。
- ~~資料夾 `Scripts/Controller/` → `Scripts/Controllers/`~~ 併入 Phase 7.5 Task 3。
- `Enemy.cs`、`EnemyGenerator.cs` 補上 `MyPoeLikeGame` namespace（7.5 只搬位置進 `Scripts/Game/`，不改 namespace）。
- ~~根目錄分類：`Combat/`、`Pooling/`~~ 併入 Phase 7.5 Task 1（`Scripts/Core/Combat/`、`Scripts/Core/Pooling/`）。
  `IDamageable`（Phase 14）與 `ProjectileSpawner`（Phase 16）新增時直接放進 `Core/` 對應位置。

**驗收**：Console 無錯誤；動畫、傷害、物件池全部仍正常。
**Commit**：`chore: hash animator params and tidy folder structure`

---

## 驗證方式

Unity Editor 已透過 MCP 連線（`MyPoeLikeGame@5b5f2097d2a211e9`，Unity 6000.5.4f1），所以每個 phase 都能自動驗證：

- `mcp__UnityMCP__read_console`（`types: ["error","warning"]`）確認沒有新增的編譯錯誤或警告。
- `mcp__UnityMCP__manage_editor` 進出 Play Mode。
- `mcp__UnityMCP__unity_reflect` / `manage_gameobject` 讀取執行期狀態（HP、`currState`、Hierarchy 中的 Projectile 數量），取代臨時的 `Debug.Log`。
- Prefab 與場景改動一律走 `manage_prefabs` / `manage_components` / `manage_scene`。

專案目前沒有任何測試。Phase 13 的 `HpHandler` 與 Phase 14 的 `IDamageable` 是純 C# 邏輯、不依賴 MonoBehaviour 生命週期，是導入 EditMode 測試最自然的切入點。
**更新**：原本要等「下一輪」，現在 Phase 7.5 會先建好 `Core.Tests`，Phase 8 之後照 TDD 寫測試。

## 這輪明確不做（下一輪）

- ~~**asmdef 切分**~~ → 已提前成 Phase 7.5。原本「等 Phase 17 驗收結果出來，界線才有實據」的考量，
  改成：先依現有、已用 CodeGraph 確認過的依賴方向切，Phase 17 若發現界線畫錯再調整。
- **VContainer 接管** `ProjectileSpawner` 與跨場景服務；`[SerializeField]` → `[Inject]`。
- **跨系統事件匯流排**：等真的出現需要廣播的事件（死亡通知 UI / 掉寶 / 成就）再做，且要做成每個事件型別一條流的 `GameEvents`，不要重建 `Subject<IEvent>`。
- ~~EditMode 測試~~ → 已提前，見上。
