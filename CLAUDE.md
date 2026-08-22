# MyPoeLikeGame

Unity 6000.5.4f1 的 ARPG 專案。程式碼在 `Assets/MyPoeLikeGame/Scripts/`。

分層：`PlayerInputHandler` → `Controller/`（把輸入轉成意圖）→ `Handlers/`（機制本體）→
`CharacterStateHandler`（狀態權威，廣播回各 Handler）。Handler 之間用 R3 的 Observable
互相訂閱，**不使用全域事件匯流排**（舊的 `Reactive.cs` 已刪除，不要重建）。

主要套件：R3（NuGet 核心 1.2.9 + UPM `com.cysharp.r3` 1.3.1）、VContainer 1.19.0、
Cinemachine 3.1.7、Input System 1.19.0。

---

# 環境設定（每台機器都要做一次）

## Script Changes While Playing 必須設成 `Stop Playing And Recompile`

`Preferences → General → Script Changes While Playing`

**這是 EditorPrefs，屬於使用者偏好，不會進版控**，所以每個人／每台機器都得自己設。
用程式設定的話：`EditorPrefs.SetInt("ScriptCompilationDuringPlay", 2)`。

### 為什麼

Unity 的預設是 `Recompile And Continue Playing`，也就是在 Play Mode 中改程式碼時
當場做 domain reload 並讓遊戲繼續跑。實測過那個流程的真實順序：

```text
OnDisable（舊物件，欄位完好）→ domain reload → OnEnable（新物件，Awake 沒跑，欄位是 null）
```

**`Awake` 被跳過**——因為對 Unity 而言那些物件早就 awake 過了，只是 managed 表示被重建。
沒有任何設定能讓它「每次重編譯都重跑 `Awake`」。

後果是所有「在 `Awake` 快取、在 `OnEnable` 使用」的欄位都會變成 null。這曾造成
每次編譯 6 個 `NullReferenceException`，而且是**跨兩次 reload 的連鎖**：
第一次 reload 讓 `OnEnable` 炸掉 → `disposable` 沒被賦值 → 第二次 reload 換 `OnDisable` 炸。
更糟的是遊戲看起來還在跑，但輸入與所有訂閱已經靜默失效，非常難查。

改成 `Stop Playing And Recompile` 之後，那個情境根本不會發生，
程式碼就能維持「`GetComponent` 與 `new` 放 `Awake`」的慣例寫法。

**不要選 `Recompile After Finished Playing`**：它會把編譯延後到停止播放之後，
造成「我明明改了程式碼，怎麼遊戲沒反應」的困惑。

### 三種模式的實測差異

| 情境 | `Awake` | `OnEnable` | `OnDisable` |
|---|---|---|---|
| 正常進入 Play Mode | ✅ 先（相差約 13ms） | ✅ 後 | — |
| Play Mode 中重新編譯（預設模式） | ❌ **不跑** | ✅ 欄位為 null | ✅ 欄位完好 |
| Edit Mode 中重新編譯 | ❌ | ❌ | ❌ |

Edit Mode 重新編譯**完全不會**呼叫任何生命週期方法（除非標了
`[ExecuteAlways]` / `[ExecuteInEditMode]`），所以那個情境從來不是問題來源。

---

# Unity 驗證協定（必讀）

## 為什麼有這一節

曾經發生過：改完 `Packages/manifest.json` 後呼叫 `refresh_unity`，緊接著 `read_console`
回報 **0 筆**，於是判定通過並 commit。實際上 Editor 端的組件那時**根本還沒開始編譯**，
稍後才吐出 6 個 `CS0619` 編譯錯誤，而且是使用者自己看到才回報的。

**教訓：`read_console` 回傳 0 筆不等於沒有錯誤，只等於「現在這一刻沒有」。**

## 每次改動後的必要驗證步驟

不可省略、不可只做一半。順序有意義：

1. **先清空 console** — `read_console` action=`clear`。不清就分不出舊錯誤與新錯誤。
2. **觸發編譯** — `refresh_unity`（`mode: force`, `scope: all`, `compile: request`）。
3. **確認編譯真的結束** — 讀 `mcpforunity://editor/state`，必須同時滿足：
   - `compilation.is_compiling == false`
   - `compilation.is_domain_reload_pending == false`
   - `compilation.last_domain_reload_after_unix_ms` **晚於**你改動的時間
   - `advice.ready_for_tools == true`
4. **讀 console** — `read_console` types=`["error","warning"]`，count 給大一點（30+）。
5. **進 Play Mode** — `manage_editor` action=`play`。
   **這是最強的證據**：Unity 在有編譯錯誤時會直接擋住不讓進 Play Mode
   （"All compiler errors have to be fixed before you can enter playmode"）。
   進得去，就代表整個專案（含所有 Editor 組件）編譯乾淨。
6. **Play Mode 中再讀一次 console**，然後 `stop`。

只做到第 4 步就宣告通過，是不夠的。

## 套件改動的額外規則

- **`refresh_unity` 不會重新解析 `Packages/manifest.json` 的變更。** 手改 manifest 後
  Unity 可能完全沒反應（Editor 沒有 focus 時尤其如此）。
- 改套件一律用 `manage_packages` action=`add_package`（非同步，要用 `status` + `job_id` 輪詢
  到 `succeeded`），或 `resolve_packages`。
- 驗證套件真的換了版本，兩個都要看：
  - `Packages/packages-lock.json` 的 `version` 欄位
  - `Library/PackageCache/` 底下的資料夾 —— **資料夾名的後綴是 fingerprint hash，不是版本號**，
    必須 `cat` 裡面的 `package.json` 才知道真正版本。舊版資料夾會被 Unity 自動清掉。
- 裝新套件時順手確認它相容 Unity 6000.5。已知地雷：Unity 6000.5 把非泛型的 IMGUI
  `TreeView` / `TreeViewItem` / `TreeViewState` 標為 **hard-obsolete**（`CS0619` 是 error
  不是 warning），任何還在用它們的編輯器擴充都會編不過。

## Prefab 與場景

一律走 `manage_prefabs` / `manage_components` / `manage_scene` / `manage_gameobject`，
**不要手改 `.prefab` / `.unity` 的 YAML**。

讀取執行期狀態（HP、狀態機的 currState、Hierarchy 中的實例數量）用 `unity_reflect` /
`manage_gameobject`，不要為了觀察而插入臨時的 `Debug.Log`。

## 回報的誠實標準

- 沒有實際驗證過的事，不要寫成「已驗證」。
- 驗證方法本身有侷限時（例如只讀了一次 console），要把侷限講出來。
- 錯誤如果是自己前一步造成的，直接說明因果，不要含糊帶過。
