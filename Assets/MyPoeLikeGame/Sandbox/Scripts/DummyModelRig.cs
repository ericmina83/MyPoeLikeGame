using UnityEngine;

namespace MyPoeLikeGame.Sandbox
{
    /// <summary>
    /// 可替換造型必須滿足的契約。掛在造型 prefab 的根上，
    /// 把 <see cref="AttackDummyViz"/> 需要驅動的部位暴露出來。
    /// </summary>
    /// <remarks>
    /// 這是正式角色那套「模型契約」的簡化版：正式的 <c>AnimationHandler</c> 要求
    /// 模型帶一個 Animator，且其 AnimatorController 具備約定好的 layer 與參數名；
    /// 這裡則要求造型帶一個 <see cref="DummyModelRig"/> 並填好三個部位。
    ///
    /// 兩者的共同點是：**邏輯 root 用 GetComponentInChildren 找造型，
    /// 而不是假設造型就在自己身上**。
    /// </remarks>
    public class DummyModelRig : MonoBehaviour
    {
        [Header("軀幹：用顏色表示目前狀態")]
        public Transform body;
        public Renderer bodyRenderer;

        [Header("武器：用前後位置表示階段進度")]
        public Transform weapon;
        public Renderer weaponRenderer;

        [Header("蓄力球：只在 OVERDRAW 出現，大小 = chargeRatio")]
        public Transform chargeOrb;
        public Renderer chargeOrbRenderer;

        /// <summary>三個部位都填好了才算滿足契約。</summary>
        public bool IsComplete =>
            body != null && bodyRenderer != null &&
            weapon != null && weaponRenderer != null &&
            chargeOrb != null && chargeOrbRenderer != null;
    }
}
