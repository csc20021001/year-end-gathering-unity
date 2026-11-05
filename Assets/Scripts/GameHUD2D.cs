using UnityEngine;
using UnityEngine.UI;

namespace YearEnd
{
    public sealed class GameHUD2D : MonoBehaviour
    {
        public Text statusText, tipText, interactionText;
        public GameObject startPanel, pausePanel, endPanel, journalPanel;
        public Text resultText, journalText;
        public Button startButton, resumeButton, restartButton, endRestartButton, journalCloseButton;
        public SceneWorld2D world;
        private Font dynamicChineseFont;
        private void Awake()
        {
            dynamicChineseFont = Font.CreateDynamicFontFromOSFont(
                new [] { "Microsoft JhengHei", "Noto Sans CJK TC", "PingFang TC", "Arial" }, 24);
            if (dynamicChineseFont != null)
                foreach (Text t in GetComponentsInChildren<Text>(true)) t.font = dynamicChineseFont;
            if (startButton != null) startButton.onClick.AddListener(() => GameSession2D.Instance.Begin());
            if (resumeButton != null) resumeButton.onClick.AddListener(() => GameSession2D.Instance.State.SetPaused(false));
            if (restartButton != null) restartButton.onClick.AddListener(() => GameSession2D.Instance.Begin());
            if (endRestartButton != null) endRestartButton.onClick.AddListener(() => GameSession2D.Instance.Begin());
            if (journalCloseButton != null) journalCloseButton.onClick.AddListener(() => GameSession2D.Instance.SetJournal(false));
        }
        private void OnDestroy() { if (dynamicChineseFont != null) Destroy(dynamicChineseFont); }
        public void Attach(SceneWorld2D controller) { world = controller; }
        public void SetHint(string hint) { if (interactionText != null) interactionText.text = hint; }
        private void LateUpdate()
        {
            GameSession2D s = GameSession2D.Instance;
            if (s == null) return;
            if (statusText != null)
            {
                int remaining = Mathf.CeilToInt(s.State.Remaining);
                statusText.text = "歲末拾遺  |  " + (world == null ? "" : new []{"旅館房間", "戶外探索區", "展示區", "阿嬤的香蕉園", "釀酒坊", "年糕店"}[world.sceneIndex]) +
                    "       物件 " + s.State.Count + "/4      HP " + s.State.Hp + "/100      " + (remaining / 60).ToString("00") + ":" + (remaining % 60).ToString("00");
            }
            if (tipText != null) tipText.text = s.VisibleMessage;
            if (startPanel != null) startPanel.SetActive(s.State.Status == Outcome.Ready);
            if (endPanel != null) endPanel.SetActive(s.State.Status == Outcome.Won || s.State.Status == Outcome.Lost);
            if (pausePanel != null) pausePanel.SetActive(s.State.Status == Outcome.Playing && s.State.Paused && !s.InMinigame && !s.JournalOpen && !s.InDialogue);
            if (journalPanel != null) journalPanel.SetActive(s.JournalOpen);
            if (journalText != null && s.JournalOpen)
            {
                string text = "研究筆記 / 四項祭儀相關物件\n\n";
                for (int i = 0; i < 4; i++) text += (s.State.Has((Relic)i) ? "✓ " : "□ ") + GameSession2D.RelicNames[i] + "\n" + GameSession2D.RelicNotes[i] + "\n\n";
                text += "本作包含原創交換任務與虛構解謎，並不重現完整 Palilin 祭儀。";
                journalText.text = text;
            }
            if (resultText != null && (s.State.Status == Outcome.Won || s.State.Status == Outcome.Lost))
                resultText.text = (s.State.Status == Outcome.Won ? "研究完成！" : s.State.Hp == 0 ? "生命耗盡" : "時間結束") +
                    "\n\n取得 " + s.State.Count + "/4 項物件\n分數 " + s.State.Score + "\n\n按下方按鈕重新開始";
        }
    }
}
