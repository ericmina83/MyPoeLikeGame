using System.Collections.Generic;

namespace MyPoeLikeGame.Combat
{
    /// <summary>
    /// 一種攻擊型態的行為。狀態機負責推進進度與轉換狀態，
    /// 這裡負責「這種攻擊實際上會發生什麼事」。
    /// </summary>
    /// <remarks>
    /// 實作必須是無狀態的：每個 <see cref="AttackType"/> 共用單一實例，
    /// 所有 per-attack 的資料都從 <see cref="AttackContext"/> 取得。
    /// </remarks>
    public abstract class AttackBehaviour
    {
        /// <summary>
        /// DRAW 進度滿的那一刻，若 attacking 仍按著是否進入 OVERDRAW。
        /// false 代表永遠不會進 OVERDRAW，也就是按住可連發的攻擊。
        /// </summary>
        public abstract bool CanOverdraw { get; }

        /// <summary>
        /// OVERDRAW 的「完成度」，回傳 &gt;= 1 就結束滿蓄、進入 FIRE。
        /// 預設恆為 0 = 無限 hold，等玩家放手。
        /// </summary>
        /// <remarks>
        /// 這與廣播出去的「強度」<see cref="AttackContext.ChargeRatio"/> 是兩回事。
        /// 強度由共用邏輯（time / fullChargeTime）算出，動畫與傷害都要用它；
        /// 要提前結束滿蓄請用這個回傳值，不要去動強度。
        /// </remarks>
        /// <param name="elapsed">已在 OVERDRAW 停留的秒數。</param>
        public virtual float OverdrawProgress(in AttackContext ctx, float elapsed) => 0f;

        /// <summary>進入 FIRE 的第一幀。投射物型的攻擊在這裡生成一次。</summary>
        public virtual void OnEnterFire(in AttackContext ctx) { }

        // ── 各階段的每幀掛勾 ──────────────────────────────────────────
        // 全部在該階段的轉換判斷「之前」呼叫，所以進度滿的那一幀也收得到。
        // 這些是做事的地方（扣耐力、開判定窗、放特效），不負責回報要不要換狀態
        // ——換狀態一律由共用邏輯依進度決定。

        /// <summary>DRAW 期間每幀呼叫。前搖的耐力消耗、蓄力特效放這裡。</summary>
        public virtual void OnDrawTick(in AttackContext ctx, float progress) { }

        /// <summary>
        /// OVERDRAW 期間每幀呼叫。強度可由 <see cref="AttackContext.ChargeRatio"/> 取得。
        /// </summary>
        /// <param name="progress">
        /// <see cref="OverdrawProgress"/> 回報的完成度，不是強度。
        /// </param>
        public virtual void OnOverdrawTick(in AttackContext ctx, float progress) { }

        /// <summary>FIRE 期間每幀呼叫。近戰揮擊型的判定窗在這裡維持。</summary>
        public virtual void OnFireTick(in AttackContext ctx, float progress) { }

        /// <summary>WITHDRAW 期間每幀呼叫。後搖的破綻狀態放這裡。</summary>
        public virtual void OnWithdrawTick(in AttackContext ctx, float progress) { }
    }

    /// <summary>
    /// <see cref="AttackType"/> 到行為實例的對照表。
    /// </summary>
    /// <remarks>
    /// 行為是無狀態的 flyweight，所以一個型態共用一個實例即可。
    /// 之後導入 VContainer 時，這裡可以換成容器註冊。
    /// </remarks>
    public static class AttackBehaviours
    {
        private static readonly Dictionary<AttackType, AttackBehaviour> Map = new()
        {
            { AttackType.RangeCharge, new RangeChargeAttackBehaviour() },
        };

        /// <summary>找不到對應的行為時回傳 null，由呼叫端決定怎麼處理。</summary>
        public static AttackBehaviour Resolve(AttackType type)
            => Map.TryGetValue(type, out var behaviour) ? behaviour : null;
    }
}
