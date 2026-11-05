using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace YearEnd
{
    public sealed class MiniGameUI2D : MonoBehaviour
    {
        public static MiniGameUI2D Active { get; private set; }
        public GameObject root;
        public Text heading, instruction, feedback;
        public Button closeButton;
        public SceneWorld2D world;
        public GameObject bananaPlant, bananaCut, bananaPeel, bananaScrape, bananaSun, bananaDone;
        public Button[] plantButtons;        
        public Image[] plantImages;
        public Button cutButton, cutSecondButton;
        public Image cutImage;
        public Sprite cutOriginalSprite, cutOneSprite, cutTwoSprite;
        public Image peelCore;
        public Image[] peelLayers;           
        public MinigameSwipeTarget[] peelSwipe;
        public Image cleanFiber;
        public Image[] stains;               
        public MinigameSwipeTarget[] scrapeSwipe;
        public Image rackFiber;
        public Image dryingFill;
        public Button retryDryButton, finishBananaButton;
        public Text bananaStatus;

        public GameObject winePanel;
        public Text recipeText, wineStatus;
        public Image jarImage;
        public Image[] jarContents;  
        public Button jarUndoButton, waterButton, recipeAgainButton;
        public Button[] ingredientButtons;
        public Image[] ingredientIcons;
        public Text[] ingredientLabels;
        public Text jarText;
        public Sprite[] ingredientSprites;

        public GameObject ricePanel;
        public Image mortarImage, doughImage, malletImage;
        public Sprite[] doughStates;   
        public RectTransform malletRect, rhythmMarker;
        public GameObject rhythmTrack;
        public Text riceStatus, riceJudgement;

        public GameObject finalPanel;
        public Button[] finalButtons;
        public Text finalStatus;

        private enum Mode { None, Banana, Wine, Rice, Final }
        private enum BananaStep { Plant, Cut, Peel, Scrape, Sun, Done }
        private Mode mode;
        private BananaStep bananaStep;
        private int cutCount, peeled, scraped, fiberIntegrity;
        private float drying;
        private bool sunOverdue;
        private float dryFailureTime;
        private readonly List<int> contents = new List<int>();
        private int currentJar;
        private double visibleRecipeUntil;
        private bool recipeWasHidden;
        private readonly int[,] recipes = { {3,1,1}, {2,1,1}, {3,2,1}, {4,1,1}, {2,2,1} };
        private int flipCount, hiddenProgress;
        private bool riceAllPerfect;
        private double nextBeat;
        private float malletAnimation;
        private float flipPoseAnimation;
        private int finalStep;
        private string latestFeedback = "";
        private const float SunDuration = 13f;
        private const double BeatInterval = .85;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
            if (peelSwipe != null) foreach (var z in peelSwipe) if (z != null) z.owner = this;
            if (scrapeSwipe != null) foreach (var z in scrapeSwipe) if (z != null) z.owner = this;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            for (int i=0; plantButtons!=null && i<plantButtons.Length; i++)
            {
                int index=i;
                if (plantButtons[i] != null) plantButtons[i].onClick.AddListener(()=>ChoosePlant(index));
            }
            if (cutButton != null) cutButton.onClick.AddListener(()=>Cut(0));
            if (cutSecondButton != null) cutSecondButton.onClick.AddListener(()=>Cut(1));
            if (retryDryButton != null) retryDryButton.onClick.AddListener(()=>BeginBananaStep(BananaStep.Sun));
            if (finishBananaButton != null) finishBananaButton.onClick.AddListener(()=>CompleteQuest(0));
            if (jarUndoButton != null) jarUndoButton.onClick.AddListener(Undo);
            if (waterButton != null) waterButton.onClick.AddListener(()=>AddIngredient(2));
            if (recipeAgainButton != null) recipeAgainButton.onClick.AddListener(()=>{ visibleRecipeUntil=Time.unscaledTimeAsDouble+5;recipeWasHidden=false;RefreshWine(); });
            for (int i=0; ingredientButtons!=null && i<ingredientButtons.Length; i++)
            {
                int index=i;
                if (ingredientButtons[i] != null) ingredientButtons[i].onClick.AddListener(()=>AddIngredient(IngredientType(index)));
            }
            for(int i=0; finalButtons!=null && i<finalButtons.Length;i++)
            {
                int index=i;
                if(finalButtons[i]!=null) finalButtons[i].onClick.AddListener(()=>PlaceFinal(index));
            }
        }
        public void Attach(SceneWorld2D scene) { world=scene; }
        private bool Open(Mode which)
        {
            var session=GameSession2D.Instance;
            if (session==null || !session.OpenMinigame()) return false;
            mode=which; Active=this; latestFeedback="";
            root.SetActive(true);
            DisplayMode(); return true;
        }
        public void OpenBanana()
        {
            if(!Open(Mode.Banana))return;
            cutCount=peeled=scraped=0;fiberIntegrity=100;
            BeginBananaStep(BananaStep.Plant);
        }
        public void OpenWine()
        {
            if(!Open(Mode.Wine))return;
            currentJar=0; ResetJar();
        }
        public void OpenRice()
        {
            if(!Open(Mode.Rice))return;
            flipCount=0; flipPoseAnimation=0; riceAllPerfect=true; BeginRicePhase();
        }
        public void OpenFinal()
        {
            if(!Open(Mode.Final))return;
            finalStep=0; RefreshFinal();
        }
        public void Close()
        {
            if (mode==Mode.None)return;
            mode=Mode.None; Active=null;
            if(root!=null)root.SetActive(false);
            GameSession2D.Instance.CloseMinigame();
        }
        private void CompleteQuest(int questIndex)
        {
            var session=GameSession2D.Instance;
            session.SetQuestReady(questIndex);
            if (questIndex == 2) session.RecordPerfectRiceCompletion(riceAllPerfect);
            Close();
            session.Notify("委託工作完成！與場景裡的 NPC 對話領取物品。",7);
        }
        private void DisplayMode()
        {
            if(bananaPlant!=null) bananaPlant.SetActive(false);
            if(bananaCut!=null) bananaCut.SetActive(false);
            if(bananaPeel!=null) bananaPeel.SetActive(false);
            if(bananaScrape!=null) bananaScrape.SetActive(false);
            if(bananaSun!=null) bananaSun.SetActive(false);
            if(bananaDone!=null) bananaDone.SetActive(false);
            if(winePanel!=null) winePanel.SetActive(mode==Mode.Wine);
            if(ricePanel!=null) ricePanel.SetActive(mode==Mode.Rice);
            if(finalPanel!=null) finalPanel.SetActive(mode==Mode.Final);
        }
        private void SetText(string title,string guide,string message="")
        {
            if(heading!=null) heading.text=title;
            if(instruction!=null) instruction.text=guide;
            if(feedback!=null)feedback.text=message;
        }
        private void BeginBananaStep(BananaStep step)
        {
            bananaStep=step; latestFeedback="";DisplayMode();
            if(step==BananaStep.Plant) bananaPlant.SetActive(true);
            if(step==BananaStep.Cut)bananaCut.SetActive(true);
            if(step==BananaStep.Peel)bananaPeel.SetActive(true);
            if(step==BananaStep.Scrape)bananaScrape.SetActive(true);
            if(step==BananaStep.Sun)bananaSun.SetActive(true);
            if(step==BananaStep.Done)bananaDone.SetActive(true);
            switch(step)
            {
                case BananaStep.Plant: SetText("阿嬤的香蕉園｜挑選植株","聽阿嬤的說明，在柵欄中的五棵植株選擇適合取纖的成熟植株。");break;
                case BananaStep.Cut: SetText("香蕉絲 1/4｜砍取兩次","點擊香蕉假莖指定區域兩次；不用蓄力條。"); UpdateCut();break;
                case BananaStep.Peel: SetText("香蕉絲 2/4｜剝取葉鞘","按住滑鼠，沿長條方向拖動外層、中層與內層葉鞘。"); peeled=0; RefreshPeel();break;
                case BananaStep.Scrape: SetText("香蕉絲 3/4｜刮除雜質","按住滑鼠向右拖曳刮除黑點；反覆刮已清理區域會傷到纖維。"); scraped=0;fiberIntegrity=100;RefreshScrape();break;
                case BananaStep.Sun: SetText("香蕉絲 4/4｜日曬收取","進度到 70%～80% 時按 E 收取；太早不行，太晚要重試。");drying=0;sunOverdue=false;dryFailureTime=0;retryDryButton.gameObject.SetActive(false);RefreshSun();break;
                case BananaStep.Done: SetText("香蕉絲前處理完成","後續工序由阿嬤處理。按下完成後回到香蕉園，向阿嬤領取香蕉葉。");break;
            }
        }
        private void ChoosePlant(int index)
        {
            if(mode!=Mode.Banana||bananaStep!=BananaStep.Plant)return;
            if(index==1) BeginBananaStep(BananaStep.Cut);
            else SetText("阿嬤的香蕉園｜挑選植株","請依成熟狀態選擇。", "這棵不是阿嬤指定的植株，請再觀察。");
        }
        private void UpdateCut()
        {
            if(bananaStatus!=null)bananaStatus.text="已砍下假莖："+cutCount+" / 2";
            if(cutImage!=null)cutImage.sprite=cutCount==0?cutOriginalSprite:cutCount==1?cutOneSprite:cutTwoSprite;
            if(cutButton!=null)cutButton.gameObject.SetActive(cutCount==0);
            if(cutSecondButton!=null)cutSecondButton.gameObject.SetActive(cutCount==1);
        }
        private void Cut(int location)
        {
            if(mode!=Mode.Banana||bananaStep!=BananaStep.Cut||location!=cutCount)return;
            cutCount++;
            if(cutCount>=2){BeginBananaStep(BananaStep.Peel);return;}
            UpdateCut();
        }
        private void RefreshPeel()
        {
            if(bananaStatus!=null)bananaStatus.text="已剝取："+peeled+" / 3";
            for(int i=0;i<3;i++)
            {
                if(peelLayers!=null&&i<peelLayers.Length&&peelLayers[i]!=null)peelLayers[i].gameObject.SetActive(i>=peeled);
                if(peelSwipe!=null&&i<peelSwipe.Length&&peelSwipe[i]!=null)peelSwipe[i].gameObject.SetActive(i==peeled);
            }
        }
        private void RefreshScrape()
        {
            if(bananaStatus!=null)bananaStatus.text="已刮除："+scraped+" / 6　　完整度 "+fiberIntegrity+"%";
            for(int i=0;i<6;i++)
            {
                if(stains!=null && i<stains.Length && stains[i]!=null)stains[i].gameObject.SetActive(i>=scraped);
                if(scrapeSwipe!=null&&i<scrapeSwipe.Length &&scrapeSwipe[i]!=null) scrapeSwipe[i].gameObject.SetActive(true);
            }
        }
        public void RegisterSwipe(int index,Vector2 drag)
        {
            if(mode!=Mode.Banana)return;
            if (drag.x<55 || Mathf.Abs(drag.y)>140) {latestFeedback="請按住後，沿著材料向右拖曳。";SetFeedback();return;}
            if(bananaStep==BananaStep.Peel)
            {
                if(index!=peeled){latestFeedback="請由外到內逐層剝取。";SetFeedback();return;}
                peeled++; if(peeled>=3)BeginBananaStep(BananaStep.Scrape);else RefreshPeel();
            }
            else if(bananaStep==BananaStep.Scrape)
            {
                if(index<scraped){fiberIntegrity=Mathf.Max(0,fiberIntegrity-10);latestFeedback="重複刮取會傷到纖維。";}
                else if(index==scraped){scraped++;latestFeedback="雜質已刮除。";}
                else latestFeedback="請按順序刮取尚有雜質的區域。";
                if(fiberIntegrity<70){scraped=0;fiberIntegrity=100;latestFeedback="纖維受損，重新處理這組葉鞘。";}
                if(scraped>=6)BeginBananaStep(BananaStep.Sun);else RefreshScrape();
            }
            SetFeedback();
        }
        private void SetFeedback(){if(feedback!=null)feedback.text=latestFeedback;}
        private void RefreshSun()
        {
            if(dryingFill!=null){dryingFill.gameObject.SetActive(true);dryingFill.fillAmount=Mathf.Clamp01(drying);}
            if(bananaStatus!=null)bananaStatus.text="日曬進度 "+Mathf.RoundToInt(drying*100f)+"%";
        }
        private void Update()
        {
            if (GameSession2D.Instance != null && GameSession2D.Instance.InDialogue) return;
            if(mode==Mode.Banana&&bananaStep==BananaStep.Sun&&!sunOverdue)
            {
                drying+=Time.unscaledDeltaTime/SunDuration;
                RefreshSun();
                if(Input.GetKeyDown(KeyCode.E))
                {
                    if(drying>=.70f&&drying<=.80f) {BeginBananaStep(BananaStep.Done);return;}
                    latestFeedback="還太早了，請觀察香蕉絲乾燥情況。";SetFeedback();
                }
                if(drying>.80f)
                {
                    sunOverdue=true;drying=1;RefreshSun();
                    latestFeedback="日曬過久，請重新挑戰這一階段。"; SetFeedback();
                    retryDryButton.gameObject.SetActive(true);
                }
            }
            if(mode==Mode.Wine && currentJar>=3 && !recipeWasHidden
                && Time.unscaledTimeAsDouble>=visibleRecipeUntil)
            {
                recipeWasHidden=true;
                RefreshWine();
            }
            if(mode==Mode.Rice)
            {
                if (flipPoseAnimation>0)
                {
                    flipPoseAnimation-=Time.unscaledDeltaTime;
                    if (flipPoseAnimation<=0) UpdateRiceDisplay();
                }
                double now=Time.unscaledTimeAsDouble;
                if(malletAnimation>0)
                {
                    malletAnimation-=Time.unscaledDeltaTime;
                    if(malletRect!=null)malletRect.anchoredPosition=new Vector2(235,malletAnimation>0?55:125);
                }
                if(hiddenProgress>=20)
                {
                    if(Input.GetKeyDown(KeyCode.E))FlipRice();
                    return;
                }
                while(now>nextBeat+.25) nextBeat+=BeatInterval;
                float move=Mathf.Clamp01(1f-(float)((nextBeat-now)/BeatInterval));
                if(rhythmMarker!=null)rhythmMarker.anchoredPosition=new Vector2(Mathf.Lerp(-248,248,move),0);
                if(Input.GetKeyDown(KeyCode.Space))HitRice(now);
            }
        }
        private int IngredientType(int slot)
        {
            if(slot==0||slot==3||slot==6)return 0;
            if(slot==1||slot==4||slot==7)return 1;
            return 3;
        }
        private void ResetJar()
        {
            contents.Clear(); latestFeedback=""; visibleRecipeUntil=Time.unscaledTimeAsDouble+5;
            recipeWasHidden=false;
            RefreshWine();
        }
        private void AddIngredient(int type)
        {
            if(mode!=Mode.Wine||currentJar>=5)return;
            contents.Add(type); if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);RefreshWine();
        }
        private void Undo()
        {
            if(mode!=Mode.Wine||contents.Count==0)return;
            contents.RemoveAt(contents.Count-1);if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);RefreshWine();
        }
        private int CountMaterial(int type)
        {
            int count=0;foreach(int i in contents)if(i==type)count++;return count;
        }
        private void RefreshWine()
        {
            if(mode!=Mode.Wine)return;
            if(wineStatus!=null)wineStatus.text="第 "+(currentJar+1)+"／5 罈　｜已完成 "+currentJar+" 罈";
            bool revealRecipe=currentJar<3 || Time.unscaledTimeAsDouble<visibleRecipeUntil;
            if(recipeText!=null)recipeText.text=revealRecipe
                ? "配方（遊戲標記）：糯米×"+recipes[currentJar,0]+"　酒麴×"+recipes[currentJar,1]+"　水×"+recipes[currentJar,2]
                : "配方卡已收起｜依記憶選取材料，需要時可按「重看配方」";
            if(recipeAgainButton!=null)recipeAgainButton.gameObject.SetActive(currentJar>=3);
            if(jarText!=null)jarText.text="罈內：糯米 "+CountMaterial(0)+" ｜酒麴 "+CountMaterial(1)+" ｜水 "+CountMaterial(2)+"\n點擊酒罈：撤回最後加入的一份";
            if(jarContents!=null)
            {
                for(int i=0;i<jarContents.Length;i++)
                {
                    if(jarContents[i]==null)continue;
                    int type=i;
                    jarContents[i].gameObject.SetActive(CountMaterial(type)>0);
                }
            }
            SetText("釀酒小遊戲｜第 "+(currentJar+1)+" 罈","右側點選材料加入左側酒罈；點擊水槽加水，點酒罈撤回。按 SPACE 確認目前配方。",latestFeedback);
        }
        private void LateUpdate()
        {
            if (GameSession2D.Instance != null && GameSession2D.Instance.InDialogue) return;
            if(mode==Mode.Wine && Input.GetKeyDown(KeyCode.Space))ConfirmWine();
        }
        private void ConfirmWine()
        {
            if(mode!=Mode.Wine ||currentJar>=5)return;
            bool correct=CountMaterial(0)==recipes[currentJar,0]&&CountMaterial(1)==recipes[currentJar,1]&&CountMaterial(2)==recipes[currentJar,2]&&CountMaterial(3)==0;
            if(!correct){latestFeedback="配方標記不符，點擊酒罈撤回最近的材料再試一次。";RefreshWine();return;}
            currentJar++;
            if(currentJar>=5){CompleteQuest(1);return;}
            ResetJar();
        }
        private void BeginRicePhase()
        {
            hiddenProgress=0;nextBeat=Time.unscaledTimeAsDouble+1.0;
            UpdateRiceDisplay();
        }
        private void UpdateRiceDisplay()
        {
            SetText("年糕店｜搗製糯米食品","看準下方節奏條，按 SPACE 敲擊；出現提示時按 E 翻動米糰。","");
            if(riceStatus!=null)riceStatus.text=hiddenProgress>=20?"按 E 翻動米糰" : "已翻動 "+flipCount+"／3 次，請看下方節奏條按 SPACE";
            if(riceJudgement!=null)riceJudgement.text=latestFeedback;
            if(flipPoseAnimation<=0&&doughImage!=null&&doughStates!=null&&doughStates.Length>=6)
                doughImage.sprite=hiddenProgress>=20?doughStates[3]:hiddenProgress>=13?doughStates[2]:hiddenProgress>=5?doughStates[1]:doughStates[0];
            if(rhythmTrack!=null)rhythmTrack.SetActive(hiddenProgress<20);
        }
        private void HitRice(double now)
        {
            double error=Math.Abs(now-nextBeat);
            if(error<=.10){hiddenProgress+=5;latestFeedback="Perfect！";}
            else if(error<=.25){hiddenProgress+=2;latestFeedback="Good！";riceAllPerfect=false;}
            else{hiddenProgress=Mathf.Max(0,hiddenProgress-1);latestFeedback="Miss";riceAllPerfect=false;}
            malletAnimation=.16f;
            nextBeat+=BeatInterval;
            if(hiddenProgress>=20)
            {
                hiddenProgress=20;
                if(flipCount>=3) {CompleteQuest(2);return;}
            }
            UpdateRiceDisplay();
        }
        private void FlipRice()
        {
            if(hiddenProgress<20)return;
            flipCount++;
            if(flipCount>3)return;
            flipPoseAnimation=.38f;
            if(doughImage!=null&&doughStates!=null&&doughStates.Length>3)doughImage.sprite=doughStates[3];
            latestFeedback="已翻動，重新累積進度。";
            BeginRicePhase();
        }
        private void RefreshFinal()
        {
            SetText("最終解謎｜原創儀式謎題","依研究筆記放置四項物品。這是虛構解謎，並非真實完整 Palilin 儀式。");
            if(finalStatus!=null)finalStatus.text="已擺放 "+finalStep+" / 4";
        }
        private void PlaceFinal(int item)
        {
            if(mode!=Mode.Final)return;
            if(item!=finalStep){latestFeedback="順序和研究筆記不一致。";SetFeedback();return;}
            int placed = finalStep;
            finalStep++;
            string eventId = new [] {"Final_Plank", "Final_Leaf", "Final_Rice", "Final_Wine"}[placed];
            System.Action afterward = () =>
            {
                if(finalStep==4){Close();GameSession2D.Instance.TryWin();}
                else RefreshFinal();
            };
            if (DialogueUI2D.Active == null || !DialogueUI2D.Active.Play(eventId, afterward, true))
                afterward();
        }
    }
}
