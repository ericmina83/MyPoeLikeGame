using MyPoeLikeGame.Handlers;
using UnityEngine;

namespace MyPoeLikeGame.Sandbox
{
    /// <summary>
    /// 自動按放攻擊鍵，讓 sandbox 不需要玩家操作就能持續循環。
    /// 時間都在 Inspector 可調。
    /// </summary>
    [RequireComponent(typeof(AttackHandler))]
    public class AttackAutoDriver : MonoBehaviour
    {
        [Tooltip("關掉就完全不驅動，可以自己接手測試")]
        [SerializeField] private bool enabledDriver = true;

        [Header("長按（會走到 OVERDRAW）")]
        [SerializeField] private float holdSeconds = 1.8f;

        [Header("放開後的間隔")]
        [SerializeField] private float releaseSeconds = 1.2f;

        [Header("交替插入短按（DRAW 未滿就放手，不進 OVERDRAW）")]
        [SerializeField] private bool alternateWithTap = true;
        [SerializeField] private float tapSeconds = 0.2f;

        private AttackHandler attack;
        private float timer;
        private int step;

        private void Awake()
        {
            attack = GetComponent<AttackHandler>();
        }

        private void OnEnable()
        {
            timer = 0f;
            step = 0;
        }

        private void OnDisable()
        {
            // 停用時確保不會把「按著」的狀態留在狀態機裡
            if (attack != null)
                attack.attackInput.OnNext(false);
        }

        private void Update()
        {
            if (!enabledDriver)
                return;

            timer -= Time.deltaTime;
            if (timer > 0f)
                return;

            // 偶數步按下、奇數步放開；alternateWithTap 時長短交替
            var pressing = step % 2 == 0;
            attack.attackInput.OnNext(pressing);

            if (pressing)
            {
                var isTap = alternateWithTap && step % 4 == 2;
                timer = isTap ? tapSeconds : holdSeconds;
            }
            else
            {
                timer = releaseSeconds;
            }

            step++;
        }
    }
}
