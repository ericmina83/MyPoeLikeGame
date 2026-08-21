namespace MyPoeLikeGame.Combat
{
    /// <summary>
    /// 決定一個 <see cref="Skill"/> 套用哪一套 <see cref="AttackBehaviour"/>。
    /// 只描述「這是什麼樣的攻擊」，不描述時序——時序在 Skill 的權重欄位裡。
    /// </summary>
    public enum AttackType
    {
        /// <summary>拉弓：可滿蓄停頓，放手發射投射物。</summary>
        RangeCharge = 0,
    }
}
