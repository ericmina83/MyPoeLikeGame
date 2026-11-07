# 決策紀錄

對話或工作中做出的、會影響設計或範圍的決定，一條一條記在這裡。**新的寫在最下面。**

每一條的格式：**決定**——理由。（細節在哪）

這份只回答「為什麼這樣決定」。要做什麼、做到哪，看 [`ROADMAP.md`](ROADMAP.md)。
長篇的設計理由已經寫在其他文件的，這裡只放一行和連結，不重複。

2026-10-19 以前的條目是從對話紀錄整理回來的。日期以當時的 commit 日期為準。

---

## 2026-08-19 ～ 08-20：計畫與工具

- **重構拆成很多小 phase，一個 phase 一個 commit**——使用者要能一步一步看懂每個變動。（`ROADMAP.md`）
- **每個 phase 停在未 `git add` 的狀態，使用者在本機 review 後才 commit**——要能用自己的工具
  （VSCode diff、Inspector、實際玩一次）確認，而不是事後從 commit 回推。（`ROADMAP.md`「工作節奏」）
- **可以 commit，但不可以 push**——push 由使用者決定。
- **角色 prefab 改成「邏輯 root + 可替換模型」**，從 `Cube.prefab` 出發，Character layer 放在 root——
  `Player.prefab` 是 FBX 的 Prefab Variant，無法改造成純邏輯 root；layer 放模型上會讓換模型後子彈打不中。
  （`ROADMAP.md`「關鍵結構發現」）
- **R3 的 UPM 套件升到 1.3.1，NuGet 核心也對齊 1.3.1**——1.3.0 在 Unity 6000.5 會因 hard-obsolete
  的 IMGUI `TreeView` 編不過（`CS0619`）。（`5489cea` `599a715` `e7e4412`）
- **建立 `CLAUDE.md` 的 Unity 驗證協定**——曾在編譯還沒開始時讀到 0 筆 console 就宣告通過，
  其實有 6 個錯誤，是使用者自己發現的。（`CLAUDE.md`「Unity 驗證協定」）
- **VContainer 範本整組刪掉，不是改檔名**——三個檔案是官方 Hello World 原文，零引用。
  VContainer 等 `ProjectileSpawner` 真的需要時再回來。（`c8b5c0c`）
- **Unity MCP 設定放在專案範圍，不放使用者範圍**——不是每個專案都需要 Unity MCP。
- **`ScriptCompilationDuringPlay` 設成 2（Stop Playing And Recompile）**——預設模式在 Play Mode 中
  重編譯會跳過 `Awake`，造成連鎖 NRE。（`CLAUDE.md`「環境設定」）
- **`GetComponent` 維持放在 `Awake`，只在 `OnDisable` 加防呆**——使用者質疑「業界都放 `Awake`」，
  實測後確認防呆就夠，搬到 `OnEnable` 沒有必要。（`ccd0b98`）

## 2026-08-21：攻擊狀態機

細節全部在 [`AttackStateMachine.md`](../Assets/MyPoeLikeGame/Scripts/Handlers/AttackStateMachine.md)。

- **四階段 DRAW / OVERDRAW / FIRE / WITHDRAW**；DRAW 是一定會播完的前搖，FIRE 可以是瞬間也可以是一段時間。
- **時序用權重，不用秒數**——`1 / attackFrequency` 是一個完整循環，三個階段依權重瓜分。
  不採用 `drawPeriod`，因為攻速加成只要乘一個值。
- **沒有 `chargeable` 開關**——DRAW 一定播完，所以不需要。
- **`Skill` 是純資料，行為由 `AttackType` 決定，用多型（`AttackBehaviour`）實作**；type 定義全部行為
  （包含 `CanOverdraw`），不是 type 加幾個獨立 bool。
- **不預先列出所有可能的 type**——「組合爆炸和要不要採用是兩回事」，要用到才加。
- **OVERDRAW 有兩個獨立的百分比**：`chargeRatio`（強度，可超過 1）與 `overdrawProgress`（完成度，由行為提供）。
  欄位改名 `fullChargeTime`。
- **放開按鍵一定離開 OVERDRAW**——共用邏輯保證的底線，行為只能加碼提前結束的條件。

## 2026-08-23：Sandbox

- **測試環境做成資料夾、場景、prefab，不是一支腳本**——使用者要求。（`a647a7d`）
- **一律做成 prefab，不在執行期 `AddComponent`**——Handler 之間有循環 `RequireComponent`。
- **Sandbox 不新增 `AttackType`，先只做攻擊**——兩個測試 Skill 只差權重資料就夠。
- **舊的 demo 不廢案，進 commit。**

## 2026-08-26：角色狀況中轉站

細節全部在 [`Handlers/readme.md`](../Assets/MyPoeLikeGame/Scripts/Handlers/readme.md)，規則摘要在 `CLAUDE.md`「架構通則」。

- **保留 `CharacterStateHandler` 當中轉站**——沒有它，Handler 之間會互相訂閱，甚至來回訂閱。
- **中轉站是 flag 板，不是狀態機；`CharacterState` enum 刪除**——角色狀況互相正交，
  而且 enum 說「現在是 ATTACK」不保證沒在翻滾，還是得去問 `DodgeHandler`。
- **所有邊指向中轉站**：Handler 主動 `Report*`，中轉站不引用任何 Handler。
- **暴露狀態（`IsBusy`），不暴露許可（`CanAct`）**——許可式命名會誘導人把瞬間的問題當成持續的。
  起手條件屬於各 Handler 自己。
- **匯總值用推導式（`ToReadOnlyReactiveProperty`），不手動 `Recompute()`**——使用者提議；
  手動重算時每個 `Report*` 都要記得呼叫，忘了也不會報錯。
- **推導式在建構子建立，不在 `Awake`**——實測放 `Awake` 會讓三個消費端在 `OnEnable` 拿到 null。
- **`isDodging` 維持 private，只透過 `ReportDodging` 寫入**——public 的 `ReactiveProperty` 會讓外部可以
  `Dispose()` 它，也能繞過匯總值直接訂閱個別旗標。
- **三個消費端先統一用 `IsBusy`，不各自拆**——YAGNI。等 `isAttacking` 進來、瞄準需要分家時再拆。
- **`IsIncapacitated` 先不加**——沒有外力狀況，加了會恆為 false。
- **dodge-cancel 是刻意的**，但目前成立只是因為沒人寫檢查。

## 2026-08-30：封裝、流程、asmdef

- **輸出埠命名 `DodgeEvents` / `AttackEvents`（複數）**——屬性不能和巢狀 payload 類別同名（CS0102）。（`1f41fc9`）
- **`DodgeHandler` 讀 `MovementEvent.input` 取方向**——不必為了它多開一個公開的原始輸入通道。（`1f41fc9`）
- **改用 superpowers 流程**：brainstorming → spec → plan → TDD。
- **已完成的 Phase 1–7 也要補 spec 與測試**——使用者選擇「連已完成的也補」。前提是先有 Test Assembly。
- **asmdef 切分從「下一輪」提前，當作 TDD 的前提**——沒有 asmdef 就沒有乾淨的 Test Assembly 可用。
- **asmdef 選完整四層（方案 B），不只切 `Core`（方案 A）**——使用者想一次改好，順便讓
  「Handler 不可以認識 Controller」變成編譯錯誤。（spec `0318aa4`）
- **組裝層命名 `Game.asmdef`**；**`Controller/` 資料夾改成複數 `Controllers/`**，與 namespace 一致；
  `Enemy.cs`、`EnemyGenerator.cs`、`PlayerInput.cs` 搬進 `Scripts/Game/`。
- **`Core.Tests` 一開始不放測試**——`Core` 目前沒有值得寫紅綠燈的純邏輯，管道先接通。
- **`.codegraph/` 只進版控它自己的 `.gitignore`**——索引是每台機器各自重建的快取。（`26d3aa2`）
- **執行 asmdef plan 時不用 git worktree**——Unity Editor 與 MCP 綁在這個資料夾，worktree 是另一個位置，
  驗證協定會做不了。
- **用 subagent-driven-development 執行時，每個 task 做完都停下來給使用者看，不連續跑完**——
  superpowers 預設是 task 之間不停，但這和專案一直以來「看過才往下」的規則衝突，使用者選擇保留後者。
  （補記：這條在 2026-10-19 建立紀錄規則時漏掉了，2026-11-07 補上。）

## 2026-10-19：紀錄方式

- **`docs/ROADMAP.md` 是唯一的 TODO 來源**——原本的 19-phase 主計畫存在 Claude 的本機計畫檔
  （不在 repo），後來從硬碟消失，只能從對話紀錄救回。
- **建立這份 `decisions.md`，並在 `CLAUDE.md` 加紀錄規則**——superpowers 只記錄 spec 與 plan，
  不記錄跨功能的路線圖、聊天中做的決定、執行時的 ruling（SDD 的 ledger 是 gitignore 的，做完會刪）。
- **不用 Claude 的 memory 功能保存專案進度**——它存在 `~/.claude/`，換電腦就沒了。

## 2026-11-07：執行方式與 repo 設定

- **asmdef plan 不用 subagent 執行，由主對話在主資料夾逐 task 直接做**（`superpowers:executing-plans`）——
  使用者決定。取代上面 08-30「用 subagent-driven-development 執行時每個 task 停下來」那條；
  「每個 task 做完停在未 add、使用者看過才往下」照舊。
- **`packages-lock.json` 的 unity-mcp 升版單獨 commit**——讓每台機器解析到同一版 MCP。（`64516db`）
- **`origin` 改用 SSH**——HTTPS 的 token 已失效，SSH 金鑰已重新產生並驗證過。
- **`Core` 這個名字先不改**——它指的是「零相依的最底層」，不是領域核心（領域核心其實是 Handlers）。
  使用者提出這個疑問後決定暫不改名。
- **Ruling：`PlayerInputHandler` 與 `PlayerInput.cs` 跟著 Controller 層走，不放進 `Handlers.asmdef`**——
  plan 的漏洞：它依賴自動產生的 `PlayerInput` 類別，Task 2 時那個類別還在 `Assembly-CSharp`（asmdef 引用不到），
  Task 4 又搬進 `Game`（Handlers 不准引用）。它實際上是輸入來源、位在 Controller 上游，不是「機制本體」。
  這次只搬位置；namespace 與改名（例如 `PlayerInputSource`）留給 Phase 19。
