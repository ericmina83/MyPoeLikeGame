using R3;
using UnityEngine;

namespace MyPoeLikeGame.Handlers
{
    /// <summary>
    /// 角色狀況的共用板。各 Handler 把「我現在處於什麼狀況」<b>回報</b>進來，
    /// 這裡把它們匯總成少數幾個共用的狀況，再廣播出去。
    /// </summary>
    /// <remarks>
    /// <para><b>這不是狀態機，是一塊 flag 板。</b>
    /// 沒有「目前狀態」這種單一欄位，只有一組彼此正交的布林。
    /// 用 enum 等於假設所有狀況互斥，但角色可以同時中毒＋翻滾＋攻擊；
    /// 而且 enum 說「現在是 ATTACK」並不保證沒在翻滾，你還是得去問 <c>DodgeHandler</c>——
    /// 那樣中轉站就白設了。<c>isDodging</c> 與 <c>isAttacking</c> 同時為 true
    /// 在這個模型裡是<b>合法且會發生</b>的，不是壞掉。</para>
    ///
    /// <para><b>代價與補償。</b>enum 的好處是「翻滾中且已死亡」這種組合<b>無法表達</b>，
    /// 型別就擋掉了；flag 板放棄了這個保護。補回來的方式是紀律：
    /// <b>每個 flag 恰好一個寫入者，而且寫入與狀態轉換同地發生</b>
    /// （見 <c>DodgeHandler.BeginDodge</c> / <c>EndDodge</c>）。
    /// 散在各處呼叫 <c>Report*</c> 就會開始漂移。</para>
    ///
    /// <para><b>三條通則（設計說明見 readme.md）</b></para>
    ///
    /// <para><b>① 暴露「狀態」，不暴露「許可」。</b>
    /// 這裡只忠實陳述事實（<c>IsBusy</c> = 正被一個已經開始的動作佔用），
    /// 由消費端自己詮釋那對它代表什麼。許可式的命名（<c>CanAct</c>）會誘導人
    /// 把一個瞬間性的問題當成持續性的——「false 的時候是不是該停下？」——
    /// 於是接出「開始翻滾 → 變 busy → 收到通知 → 取消翻滾」這種自我否定的線。
    /// 事實陳述沒有這個暗示。<b>起手條件（能不能開始翻滾／攻擊）屬於各 Handler 自己</b>，
    /// 它們用自己的欄位判斷「我已經在做了」，天然排除自己的貢獻。</para>
    ///
    /// <para><b>② 所有邊指向中轉站。</b>
    /// 這個類別<b>不引用任何 Handler</b>，也不訂閱任何 Handler——
    /// 是 Handler 主動呼叫 <c>Report*</c> 回報。所以它的對外參照數是 0，
    /// 不管加多少狀況來源都不會產生參照環。
    /// 注意方向看的是<b>誰持有誰</b>：<c>stateHandler.IsBusy.Subscribe(...)</c>
    /// 是「訂閱者依賴中轉站」，和 <c>Report*</c> 同向，兩者都不會反向。
    /// 只有「中轉站自己去 <c>dodgeHandler.DodgeEvent.Subscribe(...)</c>」才會產生反向邊，
    /// 而那樣也不會少一份副本（一樣要在這裡存），只會多一條相依。
    /// 副本躲不掉，單向可以選。</para>
    ///
    /// <para><b>③ 監聽匯總值，不要監聽個別 flag。</b>
    /// 如果消費端訂閱的是 <c>isDodging</c>，明天加 <c>isStunned</c> 時它就漏了，
    /// 而且不會有任何錯誤訊息——只會出現「暈眩了還在動」。
    /// 訂閱匯總值的話，新狀況只要進 <see cref="IsBusy"/> 的推導式，所有消費端自動生效。
    /// 這也是中轉站真正的價值：組合規則寫一次，不是每個 Handler 各列一遍
    /// 「暈眩、死亡、凍結…」。</para>
    ///
    /// <para><b>關於權威</b>：這裡的 <c>isDodging</c> 不是翻滾的狀態，是<b>角色的狀況</b>。
    /// 「什麼時候開始、什麼時候結束、方向、進度」的權威仍然完全屬於 <c>DodgeHandler</c>；
    /// 這裡只是唯一寫入者所維護的匯總視圖。</para>
    /// </remarks>
    public class CharacterStateHandler : MonoBehaviour
    {
        // ── 狀況旗標（唯一的可寫狀態）─────────────────────────────────────
        // 每一個都只有「一個寫入者」，而且寫入與狀態變更同地發生，所以不會漂移。
        // 之後會加入 isAttacking，以及 isStunned（受擊硬直）、isDead（來自 HpHandler.Depleted）。

        private readonly ReactiveProperty<bool> isDodging = new(false);

        // ── 匯總狀況（由旗標推導，不是手動維護）───────────────────────────

        /// <summary>
        /// 角色是否正被一個已經開始、不該被隨意打斷的動作佔用。
        /// 這是<b>事實陳述</b>，不是許可：訂閱者自己決定「busy 對我代表什麼」
        /// （移動與瞄準是每幀擋掉；攻擊是中斷收尾）。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsBusy { get; }

        /// <remarks>
        /// <para><b>為什麼在建構子，不在 <c>Awake</c>。</b>
        /// 消費端是在自己的 <c>OnEnable</c> 訂閱 <see cref="IsBusy"/> 的，而
        /// <b>Unity 不保證同物件上所有元件的 <c>Awake</c> 都先於任何 <c>OnEnable</c></b>。
        /// 實測過：把這裡放 <c>Awake</c>，進 Play Mode 立刻三個
        /// <c>NullReferenceException</c>（Movement / Aiming / Attack 的 <c>OnEnable</c>），
        /// 因為它們訂閱時 <see cref="IsBusy"/> 還是 null。
        /// 建構期（欄位初始化與建構子）必定早於所有生命週期回呼，所以沒有這個競態。</para>
        ///
        /// <para>這裡能放建構子，是因為內容<b>純 C#</b>（只有 R3 物件），完全不碰 Unity API。
        /// 反例見 CLAUDE.md：<c>PlayerInput</c> 是 ScriptableObject，在建構期建立會被
        /// Unity 直接擋下來，那種就只能放 <c>Awake</c>。</para>
        /// </remarks>
        public CharacterStateHandler()
        {
            // 匯總值是**推導出來**的，不是每次回報後手動重算。
            // 差別不在好不好看：手動重算的話，每個 Report* 都必須記得呼叫 Recompute()，
            // 多一個回報者就多一次忘記的機會，而且忘了不會有任何錯誤訊息。
            // 寫成推導之後，「旗標變了匯總值就會跟著變」是結構性保證。
            // （同一種手法見 DodgeHandler.BeginDodge／EndDodge：把回報綁在轉換裡。）
            //
            // 加第二個來源時就長成這樣，仍然只有這一處要改：
            //     IsBusy = isDodging.CombineLatest(isAttacking, static (d, a) => d || a)
            //                       .ToReadOnlyReactiveProperty();
            IsBusy = isDodging.ToReadOnlyReactiveProperty();
        }

        // ── 回報 API ─────────────────────────────────────────────────────

        /// <summary>由 <c>DodgeHandler</c> 在翻滾開始／結束時呼叫。</summary>
        public void ReportDodging(bool value) => isDodging.Value = value;

        private void OnDestroy()
        {
            IsBusy.Dispose();
            isDodging.Dispose();
        }
    }
}
