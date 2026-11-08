# asmdef Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split `Assembly-CSharp` into six asmdef-bounded assemblies (`Core`, `Handlers`, `Controllers`, `Game`, `Sandbox`, `Core.Tests`) so EditMode tests become possible and "Handler cannot see Controller" becomes a compiler error instead of a convention.

**Architecture:** Four layered assemblies mirror the existing `Controller → Handlers → Combat` dependency direction already confirmed clean via CodeGraph (`Core` has zero references; `Handlers` references `Core`; `Controllers` references `Handlers`; `Game` references all three). `Sandbox` gets its own asmdef referencing `Core` + `Handlers`. `Core.Tests` is an EditMode test assembly referencing `Core`, scaffolded with zero test files — real tests arrive with Phase 13/15 content, not this plan.

**Tech Stack:** Unity 6000.5.4f1, R3 (NuGet 1.3.1 + UPM `com.cysharp.r3` 1.3.1, auto-referenced — no explicit asmdef reference needed), Input System 1.19.0 (`Unity.InputSystem` asmdef).

**Spec:** `docs/superpowers/specs/2026-08-30-asmdef-split-design.md`

## Global Constraints

- Do not change any `namespace` declaration in any `.cs` file. All moved files keep their current namespace (bare `MyPoeLikeGame`, `MyPoeLikeGame.Combat`, `MyPoeLikeGame.Handlers`, `MyPoeLikeGame.Controllers`, `MyPoeLikeGame.Sandbox`) — only their file path changes.
- Every file move is `git mv <old> <new>` for both the `.cs` and its `.cs.meta` sibling, never a plain filesystem move and never delete+recreate. This preserves the GUID in the `.meta` file, which is what keeps scene/prefab references to script components intact. Moving a pre-existing folder moves its contents; the folder's own sibling `.meta` file (e.g. `Combat.meta` next to `Combat/`) must be moved separately with its own `git mv`.
- Do not hand-author `.meta` files for brand-new folders (`Core/`, `Core/Pooling/`, `Game/`, `Tests/`, `Tests/Core.Tests/`). Let Unity generate those on the next `refresh_unity` — only pre-existing assets need their meta preserved by moving it.
- `PostDamage.cs` and `PoolObjectService.cs` stay in `Scripts/` at the root, untouched. They are empty scaffolding slated for deletion in a later phase (Phase 18 of the main refactor plan), not part of this plan's scope.
- Do not fix the two known `Pool.cs` bugs (double-borrow on new instantiate, recycle never wired). That is Phase 15 of the main refactor plan.
- Do not add `excludePlatforms` or any build-exclusion setting to `MyPoeLikeGame.Sandbox.asmdef`. Having an asmdef makes excluding Sandbox from Player builds possible for the first time, but doing so is not part of this plan's scope — it stays a plain, includable assembly.
- Do not write any spec or test for the already-completed Phase 1–7 refactor work as part of this plan. That backfill is unblocked by Task 6's `Core.Tests` assembly existing, but it is a separate follow-up, not a task in this plan.
- Every task ends in the CLAUDE.md six-step Unity verification protocol (clear console → force recompile → confirm `is_compiling=false && is_domain_reload_pending=false && ready_for_tools=true` with a reload timestamp after the change → read console for 0 errors/warnings → enter Play Mode → read console again → stop) before it is considered done.
- After each task, stop in an un-`git add`ed state and report what changed. Do not stage or commit until told to. This is a standing project rule, not new to this plan.

---

### Task 1: Create `MyPoeLikeGame.Core.asmdef`

**Files:**
- Create: `Assets/MyPoeLikeGame/Scripts/Core/MyPoeLikeGame.Core.asmdef`
- Move: `Assets/MyPoeLikeGame/Scripts/Combat/*` → `Assets/MyPoeLikeGame/Scripts/Core/Combat/*`
- Move: `Assets/MyPoeLikeGame/Scripts/Combat.meta` → `Assets/MyPoeLikeGame/Scripts/Core/Combat.meta`
- Move: `Assets/MyPoeLikeGame/Scripts/Pool.cs(.meta)` → `Assets/MyPoeLikeGame/Scripts/Core/Pooling/Pool.cs(.meta)`
- Move: `Assets/MyPoeLikeGame/Scripts/PoolObject.cs(.meta)` → `Assets/MyPoeLikeGame/Scripts/Core/Pooling/PoolObject.cs(.meta)`
- Move: `Assets/MyPoeLikeGame/Scripts/Damage.cs(.meta)` → `Assets/MyPoeLikeGame/Scripts/Core/Damage.cs(.meta)`
- Move: `Assets/MyPoeLikeGame/Scripts/Projectile.cs(.meta)` → `Assets/MyPoeLikeGame/Scripts/Core/Projectile.cs(.meta)`
- Move: `Assets/MyPoeLikeGame/Scripts/ScriptableObjects/*` → `Assets/MyPoeLikeGame/Scripts/Core/ScriptableObjects/*`
- Move: `Assets/MyPoeLikeGame/Scripts/ScriptableObjects.meta` → `Assets/MyPoeLikeGame/Scripts/Core/ScriptableObjects.meta`

**Interfaces:**
- Consumes: nothing (Core is the leaf of the dependency graph)
- Produces: asmdef name `MyPoeLikeGame.Core`, which Task 2, 4, 5, 6 reference by this exact string

- [ ] **Step 1: Move the files**

```bash
cd Assets/MyPoeLikeGame/Scripts
mkdir -p Core

git mv Combat Core/Combat
git mv Combat.meta Core/Combat.meta

mkdir -p Core/Pooling
git mv Pool.cs Core/Pooling/Pool.cs
git mv Pool.cs.meta Core/Pooling/Pool.cs.meta
git mv PoolObject.cs Core/Pooling/PoolObject.cs
git mv PoolObject.cs.meta Core/Pooling/PoolObject.cs.meta

git mv Damage.cs Core/Damage.cs
git mv Damage.cs.meta Core/Damage.cs.meta
git mv Projectile.cs Core/Projectile.cs
git mv Projectile.cs.meta Core/Projectile.cs.meta

git mv ScriptableObjects Core/ScriptableObjects
git mv ScriptableObjects.meta Core/ScriptableObjects.meta
```

- [ ] **Step 2: Create the asmdef**

Write `Assets/MyPoeLikeGame/Scripts/Core/MyPoeLikeGame.Core.asmdef`:

```json
{
    "name": "MyPoeLikeGame.Core",
    "rootNamespace": "",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`autoReferenced: true` matters here: at this point in the migration, `Handlers/DamageHandler.cs` (still sitting directly in `Assembly-CSharp`, not yet split) references `Damage`, which just moved into `Core`. `autoReferenced: true` is what lets `Assembly-CSharp` keep seeing `Core`'s types without an explicit reference (Assembly-CSharp has no asmdef of its own to edit). This stays `true` through the rest of the plan — it does not weaken the Handler/Controller boundary Task 3 verifies, since that boundary is enforced by what is (and isn't) listed in each asmdef's own `references` array, not by `autoReferenced`.

- [ ] **Step 3: Verify — Unity six-step protocol**

Run `mcp__UnityMCP__read_console` (`action: clear`), then `mcp__UnityMCP__refresh_unity` (`mode: force, scope: all, compile: request`). Poll `mcpforunity://editor/state` until `compilation.is_compiling == false`, `compilation.is_domain_reload_pending == false`, `advice.ready_for_tools == true`, and `compilation.last_domain_reload_after_unix_ms` is later than when Step 1/2 were made. Run `mcp__UnityMCP__read_console` (`types: ["error","warning"]`) and confirm 0 entries. Run `mcp__UnityMCP__manage_editor` (`action: play`) — if it enters Play Mode, the whole project (including everything still in `Assembly-CSharp`) still compiles clean. Read console once more in Play Mode, then `manage_editor` (`action: stop`).

- [ ] **Step 4: Stop for review**

Do not `git add`. Report the file moves and the verification result, then wait.

---

### Task 2: Move the input source out of `Handlers/`, create `MyPoeLikeGame.Handlers.asmdef`

> **Revised 2026-11-08** (see `docs/decisions.md`, "Ruling：`PlayerInputHandler` 與 `PlayerInput.cs` 跟著 Controller 層走").
> The original Task 2 kept `PlayerInputHandler.cs` inside `Handlers.asmdef`. That cannot compile:
> it uses the generated `PlayerInput` class, which at this point is still in `Assembly-CSharp`
> (an asmdef cannot reference `Assembly-CSharp`), and the original Task 4 then moved it into
> `Game` (which `Handlers` must not reference). `PlayerInputHandler` is an input source that sits
> upstream of the Controllers — only the four Controllers use it — so it moves to the Controller side.

**Files:**
- Move: `Assets/MyPoeLikeGame/Scripts/Handlers/PlayerInputHandler.cs(.meta)` → `Assets/MyPoeLikeGame/Scripts/Controller/PlayerInputHandler.cs(.meta)`
- Move: `Assets/MyPoeLikeGame/Scripts/PlayerInput.cs(.meta)` → `Assets/MyPoeLikeGame/Scripts/Controller/PlayerInput.cs(.meta)`
- Modify (through Unity, not by hand): the `wrapperCodePath` import setting of `Assets/MyPoeLikeGame/InputActions/PlayerInput.inputactions`
- Create: `Assets/MyPoeLikeGame/Scripts/Handlers/MyPoeLikeGame.Handlers.asmdef`

After the moves `Handlers/` contains eight files: `AimingHandler.cs`, `AnimationHandler.cs`, `AttackHandler.cs`, `CharacterStateHandler.cs`, `DamageHandler.cs`, `DodgeHandler.cs`, `HpHandler.cs`, `MovementHandler.cs`. None of them uses `UnityEngine.InputSystem` or `PlayerInput` (the only grep hit, in `CharacterStateHandler.cs`, is a comment).

Namespaces do not change (Global Constraints): `PlayerInputHandler` stays `MyPoeLikeGame.Handlers`, `PlayerInput` stays `MyPoeLikeGame`. Renaming either is deferred to Phase 19.

**Interfaces:**
- Consumes: `MyPoeLikeGame.Core` (from Task 1)
- Produces: asmdef name `MyPoeLikeGame.Handlers`, which Task 3, 4, 5 reference
- Produces: `PlayerInputHandler.cs` and `PlayerInput.cs` inside `Controller/`, which Task 3 carries into `Controllers.asmdef`

- [ ] **Step 1: Move the input source**

```bash
cd Assets/MyPoeLikeGame/Scripts
git mv Handlers/PlayerInputHandler.cs Controller/PlayerInputHandler.cs
git mv Handlers/PlayerInputHandler.cs.meta Controller/PlayerInputHandler.cs.meta
git mv PlayerInput.cs Controller/PlayerInput.cs
git mv PlayerInput.cs.meta Controller/PlayerInput.cs.meta
```

Until Task 3 gives `Controller/` its asmdef, both files compile in `Assembly-CSharp` next to the four Controllers, exactly as before.

- [ ] **Step 2: Point the Input Actions code generator at the new path**

`PlayerInput.inputactions.meta` has `wrapperCodePath: Assets/MyPoeLikeGame/Scripts/PlayerInput.cs`. If it is left alone, the next edit of the Input Actions asset regenerates the class at the old path and the project gets two `PlayerInput` classes (`CS0101`).

Change it through the importer (`mcp__UnityMCP__execute_code`), not by editing the `.meta` by hand:

```csharp
var path = "Assets/MyPoeLikeGame/InputActions/PlayerInput.inputactions";
var importer = UnityEditor.AssetImporter.GetAtPath(path);
var so = new UnityEditor.SerializedObject(importer);
// The serialized name is m_WrapperCodePath; the .meta YAML shows it without the m_ prefix.
so.FindProperty("m_WrapperCodePath").stringValue = "Assets/MyPoeLikeGame/Scripts/Controller/PlayerInput.cs";
so.ApplyModifiedPropertiesWithoutUndo();
importer.SaveAndReimport();
```

Expected: `PlayerInput.inputactions.meta` now reads `wrapperCodePath: Assets/MyPoeLikeGame/Scripts/Controller/PlayerInput.cs`; no `Assets/MyPoeLikeGame/Scripts/PlayerInput.cs` reappears; `git diff` on `Controller/PlayerInput.cs` is empty or only regenerated whitespace. If `FindProperty` returns null, stop and set the path in the asset's Inspector instead (Generate C# Class → C# Class File).

Task 3 renames `Controller/` to `Controllers/`, so this path is set once more there.

- [ ] **Step 3: Create the asmdef**

Write `Assets/MyPoeLikeGame/Scripts/Handlers/MyPoeLikeGame.Handlers.asmdef`:

```json
{
    "name": "MyPoeLikeGame.Handlers",
    "rootNamespace": "",
    "references": [
        "MyPoeLikeGame.Core"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

No `Unity.InputSystem` reference: with `PlayerInputHandler` gone, nothing in `Handlers` knows about input devices. That is a boundary the compiler now enforces too.

- [ ] **Step 4: Verify — Unity six-step protocol**

Same procedure as Task 1 Step 3. This is the first point where `DamageHandler.cs`'s reference to `Core`'s `Damage` type and `AttackHandler.cs`'s reference to `Core.Combat`'s `AttackBehaviours` are resolved through an explicit asmdef reference rather than being alongside them in the same assembly — confirm 0 compile errors. Also confirm the Player in `Sample.unity` still has its `PlayerInputHandler` component (the `.meta` GUID moved with the file, so 0 "missing script" warnings is the evidence).

- [ ] **Step 5: Stop for review**

`git mv` stages the moves by itself; leave everything else un-added. Report the moves, the import-setting change, the asmdef contents and the verification result, then wait.

---

### Task 3: Rename `Controller/` → `Controllers/`, create `MyPoeLikeGame.Controllers.asmdef`, verify the boundary

**Files:**
- Move: `Assets/MyPoeLikeGame/Scripts/Controller/` → `Assets/MyPoeLikeGame/Scripts/Controllers/`
- Move: `Assets/MyPoeLikeGame/Scripts/Controller.meta` → `Assets/MyPoeLikeGame/Scripts/Controllers.meta`
- Create: `Assets/MyPoeLikeGame/Scripts/Controllers/MyPoeLikeGame.Controllers.asmdef`
- Modify (through Unity): the `wrapperCodePath` import setting of `PlayerInput.inputactions`, again
- Modify (temporarily, then revert): `Assets/MyPoeLikeGame/Scripts/Handlers/MovementHandler.cs`

**Interfaces:**
- Consumes: `MyPoeLikeGame.Handlers` (from Task 2)
- Produces: asmdef name `MyPoeLikeGame.Controllers`, which Task 4 references

- [ ] **Step 1: Rename the folder**

```bash
cd Assets/MyPoeLikeGame/Scripts
git mv Controller Controllers
git mv Controller.meta Controllers.meta
```

The folder already contains `AttackController.cs`, `DodgeController.cs`, `LookController.cs`, `MovementController.cs`, `readme.md`, and — since Task 2 — `PlayerInputHandler.cs` and `PlayerInput.cs` (all with their `.meta` files). `git mv` of the directory carries all of them.

Then repeat Task 2 Step 2 with the new path `Assets/MyPoeLikeGame/Scripts/Controllers/PlayerInput.cs`, and check the same Expected (meta updated, nothing regenerated at the old `Controller/` path).

- [ ] **Step 2: Create the asmdef**

Write `Assets/MyPoeLikeGame/Scripts/Controllers/MyPoeLikeGame.Controllers.asmdef`:

```json
{
    "name": "MyPoeLikeGame.Controllers",
    "rootNamespace": "",
    "references": [
        "MyPoeLikeGame.Handlers",
        "Unity.InputSystem"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Verify compiles clean — Unity six-step protocol**

`Unity.InputSystem` moved here from the original Task 2: `PlayerInputHandler.cs` and the generated `PlayerInput.cs` both use `UnityEngine.InputSystem` directly.

Same procedure as Task 1 Step 3. `Controller*` scripts still using `using MyPoeLikeGame.Handlers;` should resolve fine through the new explicit reference — `PlayerInputHandler` now lives in this same assembly, while the other Handlers come from `MyPoeLikeGame.Handlers`.

- [ ] **Step 4: RED — prove the boundary actually blocks (this is the whole point of choosing this plan over a `Core`-only split)**

Add this line near the top of `Assets/MyPoeLikeGame/Scripts/Handlers/MovementHandler.cs` (after the existing `using` lines):

```csharp
using MyPoeLikeGame.Controllers;
```

Run `mcp__UnityMCP__read_console` (`action: clear`), then `mcp__UnityMCP__refresh_unity` (`mode: force, scope: all, compile: request`). Poll `mcpforunity://editor/state` until `compilation.is_compiling == false`. Run `mcp__UnityMCP__read_console` (`types: ["error"]`).

Expected: at least one error whose message contains `CS0234` (`The type or namespace name 'Controllers' does not exist in the namespace 'MyPoeLikeGame'`) pointing at `MovementHandler.cs`. Record the exact message in the task report. If no error appears, stop — the asmdef reference graph has a leak (most likely `Handlers.asmdef` accidentally ended up with `Controllers` in its own `references`, or `autoReferenced` was misconfigured) and must be fixed before continuing.

- [ ] **Step 5: GREEN — remove the violation**

Remove the `using MyPoeLikeGame.Controllers;` line added in Step 4. Re-run the same clear → refresh → read_console sequence and confirm 0 errors. Enter Play Mode (`mcp__UnityMCP__manage_editor`, `action: play`), confirm 0 console entries, stop.

- [ ] **Step 6: Stop for review**

Do not `git add`. Report the folder rename, the asmdef contents, and the exact `CS0234` message captured in Step 4, then wait.

---

### Task 4: Create `Scripts/Game/` and `MyPoeLikeGame.Game.asmdef`

**Files:**
- Create: `Assets/MyPoeLikeGame/Scripts/Game/MyPoeLikeGame.Game.asmdef`
- Move: `Assets/MyPoeLikeGame/Scripts/EnemyGenerator.cs(.meta)` → `Assets/MyPoeLikeGame/Scripts/Game/EnemyGenerator.cs(.meta)`
- Move: `Assets/MyPoeLikeGame/Scripts/Enemy/Enemy.cs(.meta)` → `Assets/MyPoeLikeGame/Scripts/Game/Enemy.cs(.meta)`

> **Revised 2026-11-08:** `PlayerInput.cs` no longer moves here — Task 2 moved it to the Controller side.
- Remove: `Assets/MyPoeLikeGame/Scripts/Enemy.meta` (the now-empty `Enemy/` folder's sibling meta — git doesn't track empty directories, but this sibling file is a real tracked file that must go)

**Interfaces:**
- Consumes: `MyPoeLikeGame.Core`, `MyPoeLikeGame.Handlers`, `MyPoeLikeGame.Controllers` (from Tasks 1–3)
- Produces: asmdef name `MyPoeLikeGame.Game` (nothing downstream references it — it is the composition root)

- [ ] **Step 1: Move the files**

```bash
cd Assets/MyPoeLikeGame/Scripts
mkdir -p Game

git mv EnemyGenerator.cs Game/EnemyGenerator.cs
git mv EnemyGenerator.cs.meta Game/EnemyGenerator.cs.meta

git mv Enemy/Enemy.cs Game/Enemy.cs
git mv Enemy/Enemy.cs.meta Game/Enemy.cs.meta
git rm Enemy.meta
```

- [ ] **Step 2: Create the asmdef**

Write `Assets/MyPoeLikeGame/Scripts/Game/MyPoeLikeGame.Game.asmdef`:

```json
{
    "name": "MyPoeLikeGame.Game",
    "rootNamespace": "",
    "references": [
        "MyPoeLikeGame.Core",
        "MyPoeLikeGame.Handlers",
        "MyPoeLikeGame.Controllers"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

No `Unity.InputSystem` reference any more: it was only there for `PlayerInput.cs`, which now lives in `Controllers`. `Enemy.cs` and `EnemyGenerator.cs` are both empty `MonoBehaviour` stubs with no other dependency (confirmed by reading both files in full during planning).

- [ ] **Step 3: Verify — Unity six-step protocol**

Same procedure as Task 1 Step 3. After this task, `Assembly-CSharp` should contain only `PostDamage.cs` and `PoolObjectService.cs` from `Scripts/` (per Global Constraints, left in place) — confirm the project still compiles and enters Play Mode.

- [ ] **Step 4: Stop for review**

Do not `git add`. Report the file moves, the asmdef contents, and the verification result, then wait.

---

### Task 5: Create `MyPoeLikeGame.Sandbox.asmdef`

**Files:**
- Create: `Assets/MyPoeLikeGame/Sandbox/MyPoeLikeGame.Sandbox.asmdef`

No files move — this asmdef, placed at the `Sandbox/` root, covers the existing `Sandbox/Scripts/*.cs` (five files: `AttackAutoDriver.cs`, `AttackDummyViz.cs`, `AttackSandboxHud.cs`, `DummyModelRig.cs`, `SandboxProjectile.cs`) since Unity asmdef scope includes all descendant folders.

**Interfaces:**
- Consumes: `MyPoeLikeGame.Core`, `MyPoeLikeGame.Handlers` (from Tasks 1–2)
- Produces: nothing downstream depends on this

- [ ] **Step 1: Create the asmdef**

Write `Assets/MyPoeLikeGame/Sandbox/MyPoeLikeGame.Sandbox.asmdef`:

```json
{
    "name": "MyPoeLikeGame.Sandbox",
    "rootNamespace": "",
    "references": [
        "MyPoeLikeGame.Core",
        "MyPoeLikeGame.Handlers"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Verify — Unity six-step protocol, plus the Sandbox scene**

Run the standard six-step protocol. Additionally: load `Assets/MyPoeLikeGame/Sandbox/Scenes/AttackSandbox.unity` via `mcp__UnityMCP__manage_scene` (`action: "load"`, `path: "Assets/MyPoeLikeGame/Sandbox/Scenes/AttackSandbox.unity"`), enter Play Mode, confirm the two test dummies still auto-cycle through DRAW/OVERDRAW/FIRE/WITHDRAW with 0 console errors, then stop and load `Assets/MyPoeLikeGame/Scenes/Sample.unity` back via the same `manage_scene` (`action: "load"`) call before finishing.

- [ ] **Step 3: Stop for review**

Do not `git add`. Report the asmdef contents and the verification result, then wait.

---

### Task 6: Create `MyPoeLikeGame.Core.Tests.asmdef`

**Files:**
- Create: `Assets/MyPoeLikeGame/Tests/Core.Tests/MyPoeLikeGame.Core.Tests.asmdef`

No test files are created in this task — see Global Constraints and the spec's "測試計畫" section for why (`Core`'s current contents have no pure logic worth a test yet; real tests arrive with Phase 13/15 content).

**Interfaces:**
- Consumes: `MyPoeLikeGame.Core` (from Task 1)
- Produces: nothing downstream depends on this

- [ ] **Step 1: Create the folder and asmdef**

```bash
mkdir -p Assets/MyPoeLikeGame/Tests/Core.Tests
```

Write `Assets/MyPoeLikeGame/Tests/Core.Tests/MyPoeLikeGame.Core.Tests.asmdef`:

```json
{
    "name": "MyPoeLikeGame.Core.Tests",
    "rootNamespace": "",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "MyPoeLikeGame.Core"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

This is Unity's standard EditMode test assembly shape (matches what `Create > Testing > Tests Assembly Folder` generates) — `overrideReferences: true` plus the explicit `nunit.framework.dll` precompiled reference is what lets `[Test]` attributes resolve once someone adds the first test method.

- [ ] **Step 2: Verify — Unity six-step protocol, plus the Test Runner sees it**

Run the standard six-step protocol first (0 compile errors). Then run `mcp__UnityMCP__run_tests` (`mode: EditMode`) and confirm it reports the run completed with 0 tests found and no errors — this proves the Test Runner recognizes `MyPoeLikeGame.Core.Tests` as a valid, empty EditMode assembly rather than failing to load it.

- [ ] **Step 3: Stop for review**

Do not `git add`. Report the asmdef contents and the verification result, then wait.

---

### Task 7: Full regression pass on `Sample.unity`

No files change in this task — this is a closing end-to-end check that the whole six-assembly split, together, hasn't silently broken the actual gameplay loop the earlier phases (movement, dodge, attack) built and verified.

**Interfaces:**
- Consumes: everything from Tasks 1–6
- Produces: nothing — this is the plan's final verification gate

- [ ] **Step 1: Six-step protocol on `Sample.unity`**

Clear console, force recompile, confirm compile state, read console for 0 errors, enter Play Mode.

- [ ] **Step 2: Smoke-test the movement → dodge → attack chain**

While in Play Mode, run this via the Unity MCP `execute_code` tool (or equivalent) against the player GameObject in `Sample.unity`:

```csharp
var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var p  = UnityEngine.Object.FindFirstObjectByType<MyPoeLikeGame.Handlers.CharacterStateHandler>().gameObject;
var mv = p.GetComponent<MyPoeLikeGame.Handlers.MovementHandler>();
var dg = p.GetComponent<MyPoeLikeGame.Handlers.DodgeHandler>();
var st = p.GetComponent<MyPoeLikeGame.Handlers.CharacterStateHandler>();

mv.SetMoveInput(new UnityEngine.Vector2(0f, 1f));
dg.RequestDodge();

var dir = dg.GetType().GetField("dodgeDirection", F).GetValue(dg);
var result = "dodgeDirection=" + (dir == null ? "null" : dir.ToString()) + " IsBusy=" + st.IsBusy.CurrentValue;

dg.GetType().GetMethod("EndDodge", F).Invoke(dg, null);
return result;
```

Expected: `dodgeDirection=(0.00, 0.00, 1.00) IsBusy=True` — the exact same result Phase 6/7 verified before this plan started, now running across six separate assemblies instead of one.

- [ ] **Step 3: Read console, stop Play Mode**

Confirm 0 console entries in Play Mode, then `manage_editor` (`action: stop`).

- [ ] **Step 4: Stop for review**

Do not `git add`. Report the smoke-test result. This is the last task in the plan — once approved, the six asmdefs get committed (one commit per task, per the project's established convention, or as the user directs at review time).
