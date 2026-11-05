using UnityEngine;
using UnityEngine.SceneManagement;

namespace YearEnd
{
    public sealed class GameSession2D : MonoBehaviour
    {
        public static GameSession2D Instance { get; private set; }
        public ExpeditionState State { get; private set; } = new ExpeditionState();
        public bool InMinigame { get; private set; }
        public bool InDialogue { get; private set; }
        private bool dialogueWasPaused;
        private bool openingPending;
        private readonly bool[] questStarted = new bool[3];
        public bool IsQuestStarted(int index) => index >= 0 && index < 3 && questStarted[index];
        public void MarkQuestStarted(int index) { if(index >= 0 && index < 3) questStarted[index] = true; }
        public int ExtraGrandmaTalks => extraGrandmaTalks;
        public bool PerfectRiceWaitingForReward => perfectRiceWaitingForReward;
        public bool ConsumeOpening() { bool open=openingPending; openingPending=false; return open; }
        public bool BeginDialogue(bool fromFinalMinigame = false)
        {
            if (InDialogue || JournalOpen || State.Status != Outcome.Playing ||
                (InMinigame && !fromFinalMinigame) || (!InMinigame && State.Paused)) return false;
            dialogueWasPaused = State.Paused;
            InDialogue = true;
            State.SetPaused(true);
            return true;
        }
        public void EndDialogue()
        {
            if (!InDialogue) return;
            InDialogue = false;
            State.SetPaused(dialogueWasPaused);
        }
        public bool JournalOpen { get; private set; }
        public int JournalDetailIndex { get; private set; } = -1;
        private int extraGrandmaTalks;
        private bool perfectRiceWaitingForReward;
        public static readonly string[] HiddenNames = {"精緻的小木板", "脈絡分明的香蕉葉", "Q彈的年糕", "月光的酒與酒杯"};
        private readonly bool[] questReady = new bool[3];
        public bool IsQuestReady(int questIndex) => questIndex >= 0 && questIndex < 3 && questReady[questIndex];
        public void SetQuestReady(int questIndex) { if (questIndex>=0 && questIndex<3) questReady[questIndex]=true; }
        public void ClearQuestReady(int questIndex) { if (questIndex>=0 && questIndex<3) questReady[questIndex]=false; }
        public bool IsWorldActive => State.Active && !InMinigame && !JournalOpen && !InDialogue;
        public bool HasPendingSpawn { get; private set; }
        public Vector2 PendingSpawn { get; private set; }
        public string Message { get; private set; } = "先取得研究室旁的小木板。";
        private float messageEnd;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance == null)
                new GameObject("GameSession2D [persistent]").AddComponent<GameSession2D>();
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void Update()
        {
            if (State.Status != Outcome.Playing || InDialogue) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (InMinigame) { MiniGameUI2D.Active?.Close(); return; }
                if (JournalOpen)
                {
                    if (JournalDetailIndex >= 0) JournalDetailIndex = -1;
                    else SetJournal(false);
                    return;
                }
                State.SetPaused(!State.Paused);
            }
            if (Input.GetKeyDown(KeyCode.Tab) && !InMinigame && JournalDetailIndex < 0
                && (JournalOpen || !State.Paused))
                SetJournal(!JournalOpen);
            if (IsWorldActive) State.Tick(Time.deltaTime);
        }
        public void Begin()
        {
            InMinigame = false;
            InDialogue = false;
            openingPending = true;
            JournalOpen = false;
            JournalDetailIndex = -1;
            extraGrandmaTalks = 0;
            perfectRiceWaitingForReward = false;
            for(int i=0;i<questReady.Length;i++){questReady[i]=false;questStarted[i]=false;}
            HasPendingSpawn = true;
            PendingSpawn = new Vector2(50,27);
            State.Start();
            Notify("在旅館房間取得小木板後，從出口探索聚落。", 6);
            SceneManager.LoadScene(WorldLayout.Scenes[0]);
        }
        public void EnterRoom(int index, Vector2 mapSpawn)
        {
            if (!IsWorldActive) return;
            HasPendingSpawn = true;
            PendingSpawn = mapSpawn;
            SceneManager.LoadScene(WorldLayout.Scenes[index]);
        }
        public Vector2 UseSpawn(Vector2 defaultSpawn)
        {
            Vector2 pos = HasPendingSpawn ? PendingSpawn : defaultSpawn;
            HasPendingSpawn = false;
            return pos;
        }
        public bool TryCollect(Relic relic)
        {
            if (!IsWorldActive || !State.Collect(relic)) return false;
            Notify("獲得「" + RelicNames[(int)relic] + "」！目前 " + State.Count + "／4", 5);
            return true;
        }
        public bool TryCollectHidden(HiddenRelic relic)
        {
            if (!IsWorldActive || !State.CollectHidden(relic)) return false;
            Notify("找到隱藏物件：「" + HiddenNames[(int)relic] + "」！", 5);
            return true;
        }
        public void TalkToGrandmaAfterLeaf()
        {
            if (!IsWorldActive || !State.Has(Relic.Leaf) || State.HasHidden(HiddenRelic.VeinedLeaf)) return;
            extraGrandmaTalks++;
            if (extraGrandmaTalks >= 5) TryCollectHidden(HiddenRelic.VeinedLeaf);
            else Notify("阿嬤繼續分享香蕉葉與聚落的故事。", 4);
        }
        public void RecordPerfectRiceCompletion(bool allPerfect)
        {
            perfectRiceWaitingForReward = allPerfect;
        }
        public void ReceiveRiceReward()
        {
            if (TryCollect(Relic.Rice) && perfectRiceWaitingForReward)
                TryCollectHidden(HiddenRelic.PerfectRice);
            perfectRiceWaitingForReward = false;
        }
        public void OpenJournalItem(int index)
        {
            if (!JournalOpen || State.Status != Outcome.Playing || index < 0 || index >= 8) return;
            bool owned = index < 4 ? State.Has((Relic)index) : State.HasHidden((HiddenRelic)(index - 4));
            if (owned) JournalDetailIndex = index;
        }
        public bool OpenMinigame()
        {
            if (!IsWorldActive) return false;
            InMinigame = true;
            State.SetPaused(true);
            return true;
        }
        public void CloseMinigame(Relic? reward = null)
        {
            if (!InMinigame) return;
            InMinigame = false;
            State.SetPaused(false);
            if (reward.HasValue) TryCollect(reward.Value);
        }
        public void SetJournal(bool show)
        {
            if (InMinigame || State.Status != Outcome.Playing) return;
            JournalOpen = show;
            JournalDetailIndex = -1;
            State.SetPaused(show);
        }
        public void Notify(string text, float seconds = 4f)
        {
            Message = text;
            messageEnd = Time.unscaledTime + seconds;
        }
        public string VisibleMessage => Time.unscaledTime <= messageEnd ? Message : "WASD 移動 ｜ E 互動 ｜ Tab 筆記 ｜ Esc 暫停";
        public bool TryWin()
        {
            if (!IsWorldActive || !State.Finish()) return false;
            Notify("研究完成！", 8);
            return true;
        }
        public static readonly string[] RelicNames = {"小木板", "香蕉葉", "糯米糕／麻糬", "酒與酒杯"};
        public static readonly string[] RelicNotes = {
            "研究用的小木板。遊戲中的收集與擺放流程是原創謎題。",
            "香蕉葉與香蕉絲織布的纖維不是同一物件；玩家透過協助工藝師取得香蕉葉。",
            "搗熟糯米的互動以麻糬類糯米食品為靈感。敲擊次數是遊戲化規則。",
            "玩家協助準備五罈等待發酵的新酒，交換先前釀好的酒與酒杯；五罈配方為遊戲化挑戰。"
        };
    }
}
