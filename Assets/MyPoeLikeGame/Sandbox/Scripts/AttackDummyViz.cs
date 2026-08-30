using System;
using System.Collections.Generic;
using MyPoeLikeGame.Handlers;
using R3;
using UnityEngine;

namespace MyPoeLikeGame.Sandbox
{
    /// <summary>
    /// 把 <see cref="AttackHandler"/> 的四個階段畫成看得見的東西。
    /// 掛在**邏輯 root** 上，用 <c>GetComponentInChildren</c> 找造型——
    /// 與正式的 <c>AnimationHandler</c> 找 Animator 是同一個模式。
    /// </summary>
    [RequireComponent(typeof(AttackHandler))]
    public class AttackDummyViz : MonoBehaviour
    {
        /// <summary>給 HUD 列舉用。</summary>
        public static readonly List<AttackDummyViz> Active = new();

        [Header("顯示名稱（HUD 用）")]
        [SerializeField] private string displayName = "Dummy";

        [Header("武器沿 forward 的位置")]
        [SerializeField] private float weaponRest = 0.5f;
        [SerializeField] private float weaponDrawn = -0.7f;
        [SerializeField] private float weaponSwungThrough = 1.8f;

        [Header("投射物（sandbox 專用的視覺替身）")]
        [SerializeField] private SandboxProjectile projectilePrefab;
        [SerializeField] private Vector3 muzzleOffset = new(0f, 0.3f, 1.5f);

        private AttackHandler attack;
        private DummyModelRig rig;
        private IDisposable subscription;

        private AttackHandler.AttackState state = AttackHandler.AttackState.IDLE;
        private AttackHandler.AttackState lastState = AttackHandler.AttackState.NONE;
        private float progress;
        private float chargeRatio;
        private float weaponZOnEnter;

        public string DisplayName => displayName;
        public AttackHandler.AttackState State => state;
        public float Progress => progress;
        public float ChargeRatio => chargeRatio;

        private void Awake()
        {
            attack = GetComponent<AttackHandler>();
            rig = GetComponentInChildren<DummyModelRig>();
            weaponZOnEnter = weaponRest;
        }

        private void OnEnable()
        {
            Active.Add(this);

            if (rig == null || !rig.IsComplete)
            {
                Debug.LogWarning(
                    $"[Sandbox] {name} 底下找不到完整的 DummyModelRig，" +
                    "造型不會有反應。把造型 prefab 放進 Model 子物件並填好三個部位。", this);
            }

            subscription = attack.AttackEvents.Subscribe(e =>
            {
                state = e.attackState;
                progress = e.progress;
                chargeRatio = e.chargeRatio;
            });
        }

        private void OnDisable()
        {
            subscription?.Dispose();
            subscription = null;
            Active.Remove(this);
        }

        private void Update()
        {
            if (rig == null || !rig.IsComplete)
                return;

            if (state != lastState)
            {
                weaponZOnEnter = rig.weapon.localPosition.z;
                if (state == AttackHandler.AttackState.FIRE)
                    SpawnProjectile();
                lastState = state;
            }

            Render();
        }

        private void Render()
        {
            var p = Mathf.Clamp01(progress);
            float z;
            Color color;

            switch (state)
            {
                case AttackHandler.AttackState.DRAW:
                    z = Mathf.Lerp(weaponZOnEnter, weaponDrawn, p);
                    color = new Color(0.95f, 0.85f, 0.2f);              // 黃：前搖
                    break;

                case AttackHandler.AttackState.OVERDRAW:
                    // 過蓄（chargeRatio > 1）越紅、抖越大
                    var over = Mathf.Clamp01(chargeRatio - 1f);
                    z = weaponDrawn - Mathf.Sin(Time.time * 25f) * 0.05f * over;
                    color = Color.Lerp(new Color(1f, 0.55f, 0.1f), Color.red, over);
                    break;

                case AttackHandler.AttackState.FIRE:
                    // fireWeight = 0 時 progress 恆為 0，武器直接彈到底；
                    // fireWeight > 0 時看得到揮擊過程。
                    z = Mathf.Lerp(weaponDrawn, weaponSwungThrough, p > 0f ? p : 1f);
                    color = Color.white;
                    break;

                case AttackHandler.AttackState.WITHDRAW:
                    z = Mathf.Lerp(weaponZOnEnter, weaponRest, p);
                    color = new Color(0.3f, 0.6f, 1f);                  // 藍：後搖
                    break;

                default:
                    z = Mathf.Lerp(rig.weapon.localPosition.z, weaponRest, Time.deltaTime * 10f);
                    color = new Color(0.45f, 0.45f, 0.45f);             // 灰：待機
                    break;
            }

            var wp = rig.weapon.localPosition;
            rig.weapon.localPosition = new Vector3(wp.x, wp.y, z);

            Tint(rig.bodyRenderer, color);
            Tint(rig.weaponRenderer, color * 1.2f);

            var orbSize = state == AttackHandler.AttackState.OVERDRAW
                ? 0.15f + chargeRatio * 0.35f
                : 0f;
            rig.chargeOrb.localScale = Vector3.one * orbSize;
        }

        /// <summary>
        /// 進入 FIRE 的第一幀生一顆球，大小與顏色由 chargeRatio 決定。
        /// </summary>
        /// <remarks>
        /// 正式路徑是行為的 OnEnterFire 呼叫 IProjectileSpawner，但那個介面還沒有實作，
        /// 所以這裡先自己生。等 Phase 16 接上之後，這段就可以刪掉。
        /// </remarks>
        private void SpawnProjectile()
        {
            if (projectilePrefab == null)
                return;

            var ball = Instantiate(
                projectilePrefab,
                transform.TransformPoint(muzzleOffset),
                transform.rotation);

            var size = 0.2f + Mathf.Clamp(chargeRatio, 0f, 3f) * 0.25f;
            ball.transform.localScale = Vector3.one * size;

            var r = ball.GetComponentInChildren<Renderer>();
            if (r != null)
                Tint(r, Color.Lerp(Color.cyan, Color.magenta, Mathf.Clamp01(chargeRatio)));

            ball.Launch(transform.forward);
        }

        private static void Tint(Renderer r, Color c)
        {
            if (r == null) return;
            var m = r.material;
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        }
    }
}
