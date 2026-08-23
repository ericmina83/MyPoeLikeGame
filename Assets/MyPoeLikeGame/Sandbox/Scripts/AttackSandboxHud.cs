using UnityEngine;

namespace MyPoeLikeGame.Sandbox
{
    /// <summary>
    /// 在畫面左上角列出場上每個測試腳色的狀態讀數。
    /// 場景裡放一個就好。
    /// </summary>
    public class AttackSandboxHud : MonoBehaviour
    {
        [SerializeField] private int fontSize = 15;
        [SerializeField] private Vector2 size = new(560f, 240f);

        private GUIStyle style;

        private void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label) { richText = true };
            style.fontSize = fontSize;

            GUILayout.BeginArea(new Rect(12f, 12f, size.x, size.y), GUI.skin.box);
            GUILayout.Label("<b>攻擊狀態機 Sandbox</b>", style);
            GUILayout.Space(6f);

            if (AttackDummyViz.Active.Count == 0)
            {
                GUILayout.Label("場上沒有測試腳色。", style);
            }
            else
            {
                foreach (var dummy in AttackDummyViz.Active)
                {
                    GUILayout.Label(
                        $"<b>{dummy.DisplayName}</b>\n" +
                        $"    state = <b>{dummy.State}</b>" +
                        $"    progress = {dummy.Progress:F2}" +
                        $"    chargeRatio = {dummy.ChargeRatio:F2}",
                        style);
                    GUILayout.Space(4f);
                }
            }

            GUILayout.Space(6f);
            GUILayout.Label(
                "灰 IDLE · 黃 DRAW · 橘→紅 OVERDRAW（越紅越過蓄） · 白 FIRE · 藍 WITHDRAW",
                style);
            GUILayout.EndArea();
        }
    }
}
