using UnityEngine;

namespace YearEnd
{
    public enum WorldAction2D { Plank, BananaQuest, WineQuest, RiceQuest, ToResearch, ToOutdoors, ToExhibition, FinalPuzzle, EnterBananaGarden, EnterWinery, EnterRiceCakeShop, HiddenPlank, HiddenWine }

    public sealed class SceneWorld2D : MonoBehaviour
    {
        public static SceneWorld2D Active { get; private set; }
        public int sceneIndex;
        public Transform Player;
        public Vector2 defaultSpawn = new Vector2(50,26);
        public GameHUD2D hud;
        public MiniGameUI2D minigames;
        private WorldInteractable2D[] targets;
        private float recentAction;
        public static float MapScale(int index) => index == 1 ? .42f : .15f;
        public static Vector2 MapToWorld(Vector2 pos, int index = 0)
        {
            float scale=MapScale(index);
            return new Vector2((pos.x-50f)*scale,(pos.y-32f)*scale);
        }
        public static Vector2 WorldToMap(Vector2 pos, int index = 0)
        {
            float scale=MapScale(index);
            return new Vector2(pos.x/scale+50f,pos.y/scale+32f);
        }
        private void Awake() { Active = this; }
        private void Start()
        {
            GameSession2D s = GameSession2D.Instance;
            if (s == null) return;
            Player.position = MapToWorld(s.UseSpawn(defaultSpawn), sceneIndex);
            targets = FindObjectsByType<WorldInteractable2D>(FindObjectsSortMode.None);
            foreach (WorldInteractable2D target in targets)
            {
                if (target == null) continue;
                if (target.action == WorldAction2D.Plank && s.State.Has(Relic.Plank)) target.gameObject.SetActive(false);
                if (target.action == WorldAction2D.HiddenPlank && s.State.HasHidden(HiddenRelic.FinePlank)) target.gameObject.SetActive(false);
                if (target.action == WorldAction2D.HiddenWine && s.State.HasHidden(HiddenRelic.MoonWine)) target.gameObject.SetActive(false);
            }
            if (hud != null) hud.Attach(this);
            if (minigames != null) minigames.Attach(this);
            DialogueUI2D dialogue = DialogueUI2D.Ensure(this);
            if (sceneIndex == 0 && s.ConsumeOpening() && dialogue != null)
                dialogue.Play("Opening");
        }
        private void Update()
        {
            GameSession2D s = GameSession2D.Instance;
            if (s == null || !s.IsWorldActive || Player == null || targets == null) return;
            WorldInteractable2D closest = null;
            float nearest = float.MaxValue;
            foreach (WorldInteractable2D target in targets)
            {
                if (target == null || !target.gameObject.activeInHierarchy) continue;
                float d = Vector2.Distance(Player.position, target.transform.position);
                if (target.action == WorldAction2D.Plank && d < target.range)
                {
                    if (s.TryCollect(Relic.Plank)) target.gameObject.SetActive(false);
                    continue;
                }
                if (AlreadyDone(target.action, s)) continue;
                if (d < target.range && d < nearest) { nearest = d; closest = target; }
            }
            if (hud != null) hud.SetHint(closest == null ? "" : "按 E：" + closest.displayName);
            if (closest != null && Input.GetKeyDown(KeyCode.E) && Time.unscaledTime > recentAction + 0.3f)
            {
                recentAction = Time.unscaledTime;
                Interact(closest);
            }
        }
        private bool AlreadyDone(WorldAction2D action, GameSession2D s)
        {
            if (action == WorldAction2D.BananaQuest) return false;
            if (action == WorldAction2D.HiddenPlank) return s.State.HasHidden(HiddenRelic.FinePlank);
            if (action == WorldAction2D.HiddenWine) return !s.State.Has(Relic.Wine) || s.State.HasHidden(HiddenRelic.MoonWine);
            if (action == WorldAction2D.WineQuest) return false;
            if (action == WorldAction2D.RiceQuest) return false;
            return false;
        }
        private void Interact(WorldInteractable2D target)
        {
            GameSession2D s = GameSession2D.Instance;
            switch (target.action)
            {
                case WorldAction2D.BananaQuest:
                    if (s.State.HasHidden(HiddenRelic.VeinedLeaf))
                        Talk("Banana_AfterHidden");
                    else if (s.State.Has(Relic.Leaf))
                    {
                        int next = s.ExtraGrandmaTalks + 1;
                        string key = next >= 5 ? "Banana_Hidden" : "Banana_Extra_0" + next;
                        Talk(key, () => s.TalkToGrandmaAfterLeaf());
                    }
                    else if (s.IsQuestReady(0))
                        Talk("Banana_Reward", () => { s.ClearQuestReady(0); s.TryCollect(Relic.Leaf); });
                    else
                        Talk(s.IsQuestStarted(0) ? "Banana_InProgress" : "Banana_Start",
                            () => { s.MarkQuestStarted(0); minigames?.OpenBanana(); });
                    break;
                case WorldAction2D.WineQuest:
                    if (s.State.Has(Relic.Wine))
                        Talk("Winery_AfterReward");
                    else if (s.IsQuestReady(1))
                        Talk("Winery_Reward", () => { s.ClearQuestReady(1); s.TryCollect(Relic.Wine); });
                    else
                        Talk(s.IsQuestStarted(1) ? "Winery_InProgress" : "Winery_Start",
                            () => { s.MarkQuestStarted(1); minigames?.OpenWine(); });
                    break;
                case WorldAction2D.RiceQuest:
                    if (s.State.Has(Relic.Rice))
                        Talk("Rice_AfterReward");
                    else if (s.IsQuestReady(2))
                    {
                        string key = s.PerfectRiceWaitingForReward ? "Rice_Reward_Perfect" : "Rice_Reward";
                        Talk(key, () => { s.ClearQuestReady(2); s.ReceiveRiceReward(); });
                    }
                    else
                        Talk(s.IsQuestStarted(2) ? "Rice_InProgress" : "Rice_Start",
                            () => { s.MarkQuestStarted(2); minigames?.OpenRice(); });
                    break;
                case WorldAction2D.HiddenPlank:
                    if (s.TryCollectHidden(HiddenRelic.FinePlank)) target.gameObject.SetActive(false);
                    break;
                case WorldAction2D.HiddenWine:
                    if (s.TryCollectHidden(HiddenRelic.MoonWine)) target.gameObject.SetActive(false);
                    break;
                case WorldAction2D.ToResearch: s.EnterRoom(0, new Vector2(66,39)); break;
                case WorldAction2D.ToOutdoors:
                {
                    Vector2 spawn=sceneIndex==3?new Vector2(22,40)
                        :sceneIndex==4?new Vector2(79,42)
                        :sceneIndex==5?new Vector2(77,28)
                        :sceneIndex==0?new Vector2(53,40)
                        :new Vector2(31,23);
                    s.EnterRoom(1,spawn); break;
                }
                case WorldAction2D.EnterBananaGarden:s.EnterRoom(3,new Vector2(50,30));break;
                case WorldAction2D.EnterWinery:s.EnterRoom(4,new Vector2(50,30));break;
                case WorldAction2D.EnterRiceCakeShop:s.EnterRoom(5,new Vector2(49,30));break;
                case WorldAction2D.ToExhibition: s.EnterRoom(2, new Vector2(50,25)); break;
                case WorldAction2D.FinalPuzzle:
                    if (s.State.Count < 4) s.Notify("還缺少物件，請先蒐集 4／4。", 5);
                    else minigames?.OpenFinal();
                    break;
            }
        }
        private void Talk(string eventId, System.Action after = null)
        {
            DialogueUI2D dialogue = DialogueUI2D.Active ?? DialogueUI2D.Ensure(this);
            if (dialogue == null || !dialogue.Play(eventId, after))
            {
                Debug.LogWarning("對話無法播放：" + eventId + "，執行原本任務流程。");
                after?.Invoke();
            }
        }
        private void OnDestroy() { if (Active == this) Active = null; }
    }
}
