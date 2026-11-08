# asmdef 切分設計

## 背景與動機

專案目前所有程式碼都在 `Assembly-CSharp` 裡，沒有任何 `.asmdef`。這造成兩個問題：

1. **無法寫 EditMode 測試。** Unity 的 Test Assembly 需要引用明確的 asmdef 才能乾淨地隔離測試對象；在 `Assembly-CSharp` 裡沒有邊界可引用。原本的 19-phase 重構計畫（`Handlers/readme.md` 旁的重構計畫）在 Phase 19 明確把「切 asmdef」列為「這輪明確不做（下一輪）」的項目，理由正是這個。
2. **架構上的分層規則目前只靠自律。** `CLAUDE.md` 與 `Scripts/Handlers/readme.md` 都明文寫著「Handler 不可以認識 Controller」，但這條規則目前沒有任何機制強制執行——寫錯一個 `using` 也不會有任何警告。

現在因為要開始在 Phase 8 之後導入 TDD（見下方「決策記錄」），asmdef 切分從「下一輪要做」被提前成「這輪的前置工作」。順便把「Handler 不可以認識 Controller」變成編譯器強制的規則，一次做完，不要只解決測試的問題卻放著分層規則不管。

## 決策記錄

這份 spec 的存在本身，是幾個對話中做的決定的產物：

- **啟用 `superpowers` plugin**：使用者確認要採用 brainstorming → spec → plan → TDD 的正式流程，取代原本「直接討論設計、直接改、進 Unity 驗證」的方式。
- **範圍涵蓋已完成的階段**：不只是「Phase 8 之後」要照新流程走，Phase 1–7（已經 commit 或正在 review）也要**事後補上** spec 文件與測試——但補測試的前提是要先有 Test Assembly，所以這份 asmdef 切分同時是「解鎖 Phase 8 之後的 TDD」與「解鎖 Phase 1–7 的補測試」兩件事的共同前提。
- **選擇完整四層切分（方案 B），而非只切 `Core`（方案 A）**：方案 A 只解鎖測試，不解鎖分層強制；使用者認為既然要做，就把「Handler 不可以認識 Controller」也一併變成編譯錯誤，不要分兩次做。

## 現況依賴關係（CodeGraph 實測，非猜測）

```
Controllers/ (4 檔) ──using──> Handlers/
Handlers/AttackHandler.cs ──using──> Combat/
Handlers/DamageHandler.cs ──型別引用──> 裸 namespace 的 Damage
ScriptableObjects/Skill.cs ──using──> Combat/，型別引用裸 namespace的 Projectile
Sandbox/Scripts (5 檔) ──using──> Handlers/
```

裸 `namespace MyPoeLikeGame`（`Pool`、`Damage`、`Projectile`、`PoolObject`、`PostDamage`、
`PoolObjectService`）與 `Skill.cs` 目前沒有 namespace 包裝成 `MyPoeLikeGame.XXX` 的子命名空間，
這次維持原樣不改 namespace，只搬檔案位置——改 namespace 會牽動所有 using，超出這次範圍。

沒有發現循環依賴。

## 目標結構

```
Scripts/Core/           Core.asmdef          零相依
  ├─ Combat/            (AttackType, AttackBehaviour, AttackBehaviours,
  │                       AttackContext, IProjectileSpawner, RangeChargeAttackBehaviour)
  ├─ Pooling/            (Pool.cs, PoolObject.cs)
  ├─ Damage.cs
  ├─ Projectile.cs
  └─ ScriptableObjects/  (Skill.cs)

Scripts/Handlers/       Handlers.asmdef      References: Core
  └─ (現有九個檔案中的八個，原地不動；PlayerInputHandler 移到 Controllers，見下方「修正」)

Scripts/Controllers/    Controllers.asmdef   References: Handlers, Unity.InputSystem
  ├─ (現有 Controller/ 四個檔案搬過來；資料夾同時改名 Controller→Controllers，
  │   使其與現有 namespace `MyPoeLikeGame.Controllers` 一致——這是原計畫 Phase 19
  │   已經排定的動作，這次提前做，因為都在動 asmdef 邊界了)
  └─ PlayerInputHandler.cs、PlayerInput.cs（輸入來源，見下方「修正」）

Scripts/Game/           Game.asmdef          References: Core, Handlers, Controllers
  └─ Enemy.cs, EnemyGenerator.cs
      （目前散在 Scripts/ 根目錄與 Scripts/Enemy/，這次收進 Scripts/Game/）

Sandbox/                Sandbox.asmdef       References: Core, Handlers
  └─ (現有五個檔案，原地不動)

Tests/Core.Tests/       Core.Tests.asmdef    References: Core（EditMode）
  └─ 這次先不放任何測試案例，只搭好管道（見下方「測試計畫」）
```

**判準**：`Core` 收「不依賴任何 Handler、不依賴跨物件的 MonoBehaviour 生命週期規則」的東西——`Damage`/`Projectile`/`Pool` 雖然是 `MonoBehaviour`，但邏輯自成一體，沒有跨 Handler 耦合。

> `Core` 指的是「零相依的最底層」，不是領域核心——遊戲機制的本體其實是 `Handlers`。名字暫時不改（2026-11-07 決定）。

### 修正（2026-11-08）：`PlayerInputHandler` 與 `PlayerInput.cs` 歸 Controller 層

原設計把 `PlayerInputHandler` 留在 `Handlers.asmdef`、把 `PlayerInput.cs` 放進 `Game.asmdef`，
**這個組合編不過**：`PlayerInputHandler` 依賴自動產生的 `PlayerInput` 類別，而 `Handlers` 不准引用 `Game`
（在 `PlayerInput.cs` 搬家前，它又位於 asmdef 引用不到的 `Assembly-CSharp`）。

它其實不是「機制本體」，而是位在 Controller 上游的**輸入來源**（`CLAUDE.md` 的分層圖本來就畫成
`PlayerInputHandler → Controller/ → Handlers/`），只有四個 Controller 用它，Handler 一個都沒用。
所以兩個檔案一起放進 `Controllers.asmdef`，`Unity.InputSystem` 的引用也跟著從 `Handlers` 移到 `Controllers`
——順帶讓「Handler 不認識輸入裝置」也變成編譯器強制的規則。

- namespace 不變（`PlayerInputHandler` 仍是 `MyPoeLikeGame.Handlers`）；改 namespace 或改名（例如 `PlayerInputSource`）留給 Phase 19。
- 搬 `PlayerInput.cs` 時要一併改 `PlayerInput.inputactions` 的 `wrapperCodePath`，否則下次重新產生程式碼會在舊位置多出一份，造成重複類別（CS0101）。

## 測試計畫

`Core.Tests.asmdef` 這次刻意不放任何測試案例。`Core/` 目前的內容沒有現成的「已知該怎麼行為」的純邏輯值得寫紅綠燈：

- `Skill` 是純資料，沒有行為可測
- `Damage`/`Projectile` 的邏輯依賴 `Physics.RaycastNonAlloc` 等，EditMode 測試不容易覆蓋
- `Pool` 已知有兩個 bug（原計畫 Phase 15：新建物件被重複借出、回收機制從未真正運作），但修這兩個 bug是 Phase 15 的範圍，不是這次

真正的測試會在 Phase 13（`HpHandler` 傷害計算）、Phase 15（修 `Pool` 的已知 bug）這類有明確預期行為的階段，用正常的 TDD 紅綠燈自然長出來。這次的任務只是確保「想寫測試的時候，管道已經通了」。

## 驗證計畫

兩層驗證，缺一不可：

1. **編譯與既有功能不變**：沿用 `CLAUDE.md` 的六步驟協定——清 console → force recompile
   → 確認 `is_compiling=false`、`is_domain_reload_pending=false`、reload 時間戳晚於改動
   → 讀 console 0 error → 進 Play Mode → Play Mode 中再讀一次 console → stop。
   額外注意：`Sandbox.asmdef` 拆出去後，`AttackSandbox.unity` 場景要能正常進 Play Mode，
   證明沒有遺漏任何跨 asmdef 的隱藏耦合。

2. **驗證分層邊界真的擋得住**（這是選方案 B 而非 A 的核心價值，必須實測，不能只憑設定宣稱）：
   暫時在 `MovementHandler.cs` 加一行 `using MyPoeLikeGame.Controllers;`，確認 Unity 吐出
   編譯錯誤（預期是 `CS0234`：命名空間中沒有類型或命名空間名稱），記錄那個錯誤訊息，
   然後移除那行、確認錯誤消失、專案回到乾淨編譯的狀態。

## 明確不做（超出這次範圍）

- 不改裸 `namespace MyPoeLikeGame` 為子命名空間（`MyPoeLikeGame.Core` 等）——牽動所有
  using，這次只搬檔案位置
- 不修 `Pool.cs` 已知的兩個 bug（新建即用被重複借出、回收機制沒運作）——那是 Phase 15
- 不刪 `PostDamage.cs`、`PoolObjectService.cs` 這兩個空殼——那是 Phase 18
- 不把 `Sandbox.asmdef` 排除在正式 build 之外——asmdef 拆出來後這件事變得可能，但不是
  這次要做的事
- 不補 Phase 1–7 的 spec 文件與測試——那是這份 asmdef 工作解鎖之後的下一步，不是同一個
  spec 的範圍
