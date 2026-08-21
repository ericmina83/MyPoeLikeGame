using MyPoeLikeGame.Combat;
using UnityEngine;

namespace MyPoeLikeGame
{
    /// <summary>
    /// 一次攻擊的資料。**純資料，不含行為**——行為由 <see cref="type"/>
    /// 對應到的 <see cref="AttackBehaviour"/> 提供。
    /// 詳見 Scripts/Handlers/AttackStateMachine.md。
    /// </summary>
    [CreateAssetMenu(fileName = "Skill", menuName = "Scriptable Objects/Skill")]
    public class Skill : ScriptableObject
    {
        public string skillName = "New Skill";

        [Tooltip("決定套用哪一套 AttackBehaviour")]
        public AttackType type = AttackType.RangeCharge;

        public Projectile projectilePrefab;

        public Vector3 offset = new(0, 1, 1);

        public Vector3 direction = new(0, 0, 1);

        [Header("時序")]
        [Tooltip("每秒可完成幾次完整的 DRAW → FIRE → WITHDRAW 循環。攻速加成乘這個值即可。")]
        public float attackFrequency = 1f;

        [Tooltip("前搖佔循環的幾份")]
        public float drawWeight = 1f;

        [Tooltip("判定區段佔循環的幾份。0 = 瞬發。")]
        public float fireWeight = 0f;

        [Tooltip("後搖佔循環的幾份")]
        public float withdrawWeight = 1f;

        [Header("蓄力")]
        [Tooltip("強度蓄滿所需秒數。這不是 OVERDRAW 的狀態時長——OVERDRAW 可以停留得更久（過蓄）或更短。")]
        public float fullChargeTime = 1f;
    }
}
