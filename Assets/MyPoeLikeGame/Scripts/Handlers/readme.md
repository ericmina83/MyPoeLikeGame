# Handlers

機制本體。每個 Handler 負責一件事（移動、翻滾、攻擊、血量…），
彼此用 R3 的 Observable 互相訂閱，**不使用全域事件匯流排**。

攻擊狀態機另有專文：[AttackStateMachine.md](AttackStateMachine.md)。

---

## 分層與依賴方向

```text
PlayerInputHandler ──→ Controllers/ ──→ Handlers/ ──→ CharacterStateHandler
   （原始輸入）        （轉成意圖）      （機制本體）      （狀況共用板）
                                              ↑                 │
                                              └── 訂閱匯總狀況 ──┘
```

- **Controller 只認識「誰在下指令」**，Handler 只認識「機制怎麼運作」。
  這條線的價值要等 `EnemyAIController` 出現才算兌現——同一組 Handler
  應該能被 AI 驅動而不必修改。
- **Handler 不可以認識 Controller。** 目前靠自律，之後切 asmdef 讓編譯器強制。
- Handler 之間**單向**：誰需要誰的狀態，就去訂閱對方的 Observable。

---

## `CharacterStateHandler` 是 flag 板，不是狀態機

這是這一層最容易誤解的地方，所以講清楚。

它**沒有**「目前狀態」這種單一欄位。它持有一組**彼此正交的布林**
（`isDodging`，之後還會有 `isAttacking` / `isStunned` / `isDead`），
每個都忠實陳述「這個狀況現在是否成立」。

**`isDodging` 與 `isAttacking` 同時為 true 是合法的，不是壞掉。**
狀況之間怎麼互相牽制，是各 Handler 自己詮釋的事。

### 為什麼不是 enum

| | 能表達 | 不能表達 |
|---|---|---|
| enum 狀態機 | 「我現在是 ATTACK」 | 同時中毒、同時翻滾 |
| flag 板 | 任意組合 | — |

用 enum 等於假設所有狀況互斥，但角色可以同時中毒＋翻滾＋攻擊。
而且更關鍵：**enum 說「現在是 ATTACK」並不保證 `isDodging` 是 false**，
你還是得跑去問 `DodgeHandler`——那樣中轉站就完全失去意義了。

enum 只有在**狀況真的互斥**、而且**消費端需要知道是哪一個**時才划算。
角色狀況兩條都不滿足。

### 這個選擇的代價，以及補償

enum 有一個 flag 板沒有的好處：「翻滾中且已死亡」這種組合**無法表達**，
型別就擋掉了。flag 板放棄了這個保護——如果某個 bug 讓 `isDodging`
在死亡後還是 true，**沒有任何東西會攔截**。

補回來的方式是紀律：

> **每個 flag 恰好一個寫入者，而且寫入與狀態轉換同地發生。**

這就是 `DodgeHandler.BeginDodge` / `EndDodge` 這兩個方法存在的理由——
不是為了好看，是為了讓「不可能漏掉回報」變成結構性保證而不是靠記性：

```csharp
private void BeginDodge(Vector3 direction)
{
    dodgeDirection = direction.normalized;   // 改自己的狀態
    time = 0.0f;
    stateHandler.ReportDodging(true);        // 回報，同一個方法內，漏不掉
}
```

散在各處呼叫 `ReportDodging` 就會開始漂移。

### 權威 vs 匯總視圖

中轉站裡的 `isDodging` **不是翻滾的狀態，是角色的狀況**。
「什麼時候開始、什麼時候結束、方向、進度」的權威仍然完全屬於 `DodgeHandler`。
中轉站只是唯一寫入者所維護的一份匯總視圖。

副本躲不掉——就算反過來讓中轉站訂閱 `DodgeHandler`，那份資料一樣要存在中轉站裡。
**副本躲不掉，但單向可以選。**

---

## 三條通則

### ① 暴露「狀態」，不暴露「許可」

中轉站只**陳述事實**，由消費端自己詮釋那對它代表什麼。

```csharp
// ✅ 事實陳述
public ReadOnlyReactiveProperty<bool> IsBusy { get; }

// ❌ 許可式命名
public ReadOnlyReactiveProperty<bool> CanAct { get; }
```

**為什麼**：許可式的命名會誘導人把一個**瞬間性**的問題當成**持續性**的。
看到 `CanAct` 變 false，很自然會想「那是不是該停下？」，於是接出：

```text
開始翻滾 → isDodging = true → CanAct = false
        → DodgeHandler 收到 false → 取消翻滾    ← 自我否定
```

事實陳述沒有這個暗示。而且兩種問法**本來就都真實存在**：

| 消費端 | 性質 | 為什麼 |
|---|---|---|
| Movement / Aiming | **持續**，每幀問一次 | 移動本來就是持續在做的事 |
| Dodge / Attack 起手 | **瞬間**，只在按鍵那刻有意義 | 起手之後就不再問了 |

把兩者混在同一個許可名底下，是誤接的來源。

**推論：起手條件屬於各 Handler 自己。**
`DodgeHandler` 用自己的 `dodgeDirection != null` 判斷「我已經在翻滾」，
用自己的欄位就**天然排除了自己回報進中轉站的那份貢獻**，
不需要中轉站提供一份「排除呼叫者」的特製答案。

### ② 所有邊指向中轉站

`CharacterStateHandler` 不引用任何 Handler，也不訂閱任何 Handler。
是 Handler 主動呼叫 `Report*` 回報。**它的對外參照數是 0**，
不管加多少狀況來源都不會產生參照環。

方向看的是**誰持有誰**：

```csharp
// 兩者都是 Handler → 中轉站，同一個方向
stateHandler.ReportDodging(true);          // 呼叫
stateHandler.IsBusy.Subscribe(...);        // 註冊訂閱

// 只有這個是 中轉站 → Handler
dodgeHandler.DodgeEvent.Subscribe(...);    // 中轉站主動去訂閱別人
```

**訂閱別人的 Observable，是「你依賴他」，不是「他依賴你」。**
所以消費端訂閱 `IsBusy` 不會破壞星狀拓撲。

### ③ 監聽匯總值，不要監聽個別 flag

如果消費端訂閱的是 `isDodging`，明天加 `isStunned` 時它就漏了，
而且**不會有任何錯誤訊息**——只會出現「暈眩了還在動」。

訂閱匯總值的話，新狀況只要加進 `IsBusy` 的推導式，所有消費端自動生效，一行都不用改。

這也是中轉站真正的價值：**組合規則寫一次**，不是每個 Handler 各列一遍
「暈眩、死亡、凍結…」。

### 規則住哪：兩種要分開

| 種類 | 住哪 | 例子 |
|---|---|---|
| **匯總規則**：這些狀況合起來構成什麼共用狀況 | 中轉站的推導式 | `isBusy = isDodging \|\| isAttacking` |
| **詮釋規則**：這對「我」特別代表什麼 | 各 Handler | 移動 → 直接擋掉；攻擊 → 趕快收尾 |

如果所有規則都下放到 Handler，中轉站就退化成純資料倉庫，
每個消費端又會各寫一份組合邏輯——正是要消滅的重複。

---

## 兩層狀態機

角色同時存在兩台狀態機，**它們是刻意分開的**：

| 層 | 型別 | 內容 | 性質 |
|---|---|---|---|
| **狀況** | `CharacterStateHandler` 的 flag | `isDodging`（之後 `isAttacking` / `isStunned` / `isDead`） | 正交，可同時成立 |
| **動作** | `AttackHandler.AttackState` | `DRAW` / `OVERDRAW` / `FIRE` / `WITHDRAW` | 互斥，有自己的進度與週期 |

**為什麼不合併**：合併會產生笛卡兒積——需要 `DODGE×DRAW`、`DODGE×FIRE`…
每加一個狀況就乘一次動作數量。分開之後兩者各自線性成長。

注意兩層的性質不同：**動作層是真正的狀態機**（同時只能在一個階段，
而且有明確的轉換規則），**狀況層不是**。

---

## 消費端目前怎麼詮釋 `IsBusy`

| 消費端 | 詮釋 | 程式碼 |
|---|---|---|
| `MovementHandler` | 直接擋掉 | `if (isBusy) return;` |
| `AimingHandler` | 直接擋掉 | `if (isBusy) return;` |
| `AttackHandler` | **收尾**，不是停住 | 拉滿的弓射出去（`OVERDRAW → FIRE`），沒成形的放棄 |

**注意瞄準那一格會變。**拉弓時必須能轉向，那是這個遊戲的核心手感。
今天沒問題，因為 `isBusy` 只由 `isDodging` 構成。
等 `isAttacking` 進來之後，`AimingHandler` 就得改成看個別狀況而不是 `IsBusy`
——而**改的是它自己的詮釋，中轉站不必動**，這正是通則①的用處。

---

## 已記錄的決定

### 翻滾可以打斷任何東西，這是刻意的

`DodgeHandler` 不用 `IsBusy` 當起手條件，所以翻滾隨時能起手，
並透過 `isBusy` 中斷移動、瞄準與攻擊。dodge-cancel 是 ARPG 的核心手感。

但要注意：這個行為目前成立的原因是「**沒有人寫檢查**」，而不是「有人決定如此」。
之後若要加「暈眩時不能翻滾」，檢查點在 `DodgeHandler` 的起手處，
而且是**讀取狀況自己組條件**，不是跟中轉站要許可（通則①）。

### 中轉站的輸入目前只有一個

唯一的 `Report*` 來源是 `DodgeHandler`。也就是說這個類別叫做
`CharacterStateHandler`，但**現階段實質上是 `DodgeStateHandler`**——
只有一個位元的資訊量。

這不是錯，是還沒長出來。但要意識到**這個結構還沒被真正驗證**：
它撐不撐得住，要等第二個狀況（`isAttacking`、受擊硬直）進來才知道。

### 死亡要走狀況層，不要走 `HpHandler`

`HpHandler` 只擁有數字，歸零時發布 `Depleted` 這個**事實**。
「死亡代表什麼」是政策，屬於擁有該實體的人（玩家 → Game Over／復活；
敵人 → 掉寶＋回物件池）。實作方式是讓 `CharacterStateHandler` 收下
`ReportDead(true)`，推導式把它算進 `IsBusy`——
現有訂閱者自動處理，零新增耦合。

---

## 已知待處理

- [ ] `isAttacking` 尚未回報進中轉站；接上時 `AimingHandler` 要改成看個別狀況
- [ ] `IsIncapacitated`（外力：暈眩／死亡）等第一個外力狀況出現時再加，
      現在加會恆為 false
- [ ] 兩層狀態機的互動規則目前是隱含的，需要在規則變複雜前明文化
- [ ] `AnimationHandler` 的模型契約（Animator layer 與參數名）待補
