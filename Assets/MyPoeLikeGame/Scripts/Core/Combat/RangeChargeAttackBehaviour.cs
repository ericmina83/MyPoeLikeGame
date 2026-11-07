namespace MyPoeLikeGame.Combat
{
    /// <summary>
    /// 拉弓：DRAW 拉滿後若仍按著就停在 OVERDRAW，放手發射投射物。
    /// 這是 <see cref="Handlers.AttackHandler"/> 原本寫死的那一套邏輯。
    /// </summary>
    public sealed class RangeChargeAttackBehaviour : AttackBehaviour
    {
        public override bool CanOverdraw => true;

        // OverdrawProgress 不覆寫：恆為 0，代表可以無限 hold，只有放手才發射。

        public override void OnEnterFire(in AttackContext ctx)
        {
            var skill = ctx.Skill;
            if (skill == null || skill.projectilePrefab == null)
                return;

            // 生成器要等物件池重整完才會接上（計畫 Phase 15、16）。
            // 在那之前 FIRE 會正常運轉，只是不會有投射物飛出來。
            ctx.Spawner?.Spawn(
                skill.projectilePrefab,
                ctx.Sender,
                skill.offset,
                skill.direction,
                ctx.ChargeRatio);
        }
    }
}
