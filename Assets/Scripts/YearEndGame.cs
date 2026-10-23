using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YearEnd
{
    public sealed class YearEndGame : MonoBehaviour
    {
        public static YearEndGame Instance { get; private set; }
        public ExpeditionState State { get; private set; } = new ExpeditionState();
        public int Room { get; private set; }
        public Vector2 Position { get; private set; } = new Vector2(50, 26);
        public static readonly string[] ItemNames = { "小木板", "香蕉葉", "糯米糕／年糕", "酒與酒杯" };
        public static readonly string[] ItemNotes = {
            "原視的家族訪談提到，準備祭祖時會在爐灶旁放小木板，再鋪香蕉葉。遊戲以此作為研究起點。Palilin 也是家人歲末團聚、追念祖先的時刻。",
            "原視報導記錄受訪家庭選用葉面完整的香蕉葉，鋪在小木板上承放祭品。這是該家族的實踐記錄，各家庭的方式可能不同。",
            "原視訪談將年糕列入祭品，蘭陽博物館的介紹也提及糯米糕。食物連結家庭記憶；本作將糯米糕／年糕合計為一項蒐集物。",
            "原視報導提及釀酒與年糕等供品。祭祖表達對祖先的感念，也凝聚家人。本作的酒與酒杯合計一項；不重現祭儀順序，也不把遊戲規則視為祭儀規範。"
        };
        static readonly string[] RoomNames = { "研究室", "戶外探索區", "展示區" };
        Texture2D atlas;
        Texture2D[] maps;
        Font font;
        GUIStyle label, title, small, body, button;
        bool stylesReady, loading, journal, debug, smoke;
        int selected, facing, hits;
        float walking, toastUntil, transitionUntil;
        string toast = "";
        Vector2[] npcs = new Vector2[3];
        int[] targets = { 1, 1, 1 };
        Rect mapRect = new Rect(36, 150, 1125, 720);
        readonly Color ink = new Color(.12f,.22f,.21f);
        readonly Color paper = new Color(.95f,.92f,.84f);
        readonly Color gold = new Color(.79f,.59f,.31f);
        readonly Color pale = new Color(.71f,.78f,.68f);
        const float Speed = 19f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance == null) new GameObject("YearEndGame").AddComponent<YearEndGame>();
        }
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            atlas = Resources.Load<Texture2D>("Art/atlas");
            maps = new [] { Resources.Load<Texture2D>("Art/research"), Resources.Load<Texture2D>("Art/outdoors"), Resources.Load<Texture2D>("Art/exhibition") };
            font = Font.CreateDynamicFontFromOSFont(new [] { "Microsoft JhengHei", "Noto Sans CJK TC", "Arial" }, 22);
            ResetPatrols();
            smoke=Array.IndexOf(Environment.GetCommandLineArgs(), "--smoke-test") >= 0;
            if(smoke) { Application.runInBackground=true; StartCoroutine(SmokeTest()); }
        }
        void ResetPatrols() { for(int i=0;i<3;i++) { npcs[i]=WorldLayout.PatrolRoutes[i][0]; targets[i]=1; } }
        public void Begin()
        {
            State.Start(); hits = 0; journal = false; ResetPatrols();
            StartCoroutine(ChangeRoom(0,new Vector2(50,26)));
            Notify("距離展示資料整理完成，只剩三分鐘。先拿好桌旁的小木板。", 8);
        }
        public void Notify(string message, float seconds=4) { toast=message; toastUntil=Time.unscaledTime+seconds; }
        public IEnumerator ChangeRoom(int index, Vector2 spawn)
        {
            if(loading) yield break;
            loading=true;
            AsyncOperation op=SceneManager.LoadSceneAsync(WorldLayout.Scenes[index]);
            while(!op.isDone) yield return null;
            Room=index; Position=spawn; transitionUntil=Time.unscaledTime+.7f; loading=false;
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3)) debug=!debug;
            if (Input.GetKeyDown(KeyCode.Escape) && State.Status==Outcome.Playing)
            {
                if(journal) { journal=false; State.SetPaused(false); }
                else State.SetPaused(!State.Paused);
            }
            if (Input.GetKeyDown(KeyCode.Tab) && State.Status==Outcome.Playing)
            { journal=!journal; State.SetPaused(journal); }
            if(!State.Active) return;
            State.Tick(Time.deltaTime);
            if(!State.Active || loading) return;
            float dt=Mathf.Min(Time.deltaTime,.1f);
            Vector2 input=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),
                (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0));
            if(input.sqrMagnitude>0)
            {
                input.Normalize(); walking+=dt*10;
                facing=Mathf.Abs(input.x)>Mathf.Abs(input.y)?(input.x<0?2:3):(input.y>0?1:0);
                Position=WorldLayout.Move(Room,Position,input*Speed*dt);
            } else walking=0;
            CheckPickups();
            if(Room==1)
            {
                for(int i=0;i<3;i++)
                {
                    Vector2[] route=WorldLayout.PatrolRoutes[i];
                    npcs[i]=Vector2.MoveTowards(npcs[i],route[targets[i]],(i==2?8:6)*dt);
                    if(Vector2.Distance(npcs[i],route[targets[i]])<.1f) targets[i]=(targets[i]+1)%route.Length;
                    if(Vector2.Distance(npcs[i],Position)<3.5f) TakeDamage();
                }
            }
            CheckExit();
        }
        public bool TakeDamage()
        {
            if(!State.Damage()) return false;
            hits++; Notify("碰到巡邏人物：−20 HP。閃爍期間暫時無敵。",3); return true;
        }
        public void CheckPickups()
        {
            for(int i=0;i<4;i++)
            {
                if((i==0?0:1)!=Room || State.Has((Relic)i)) continue;
                if(Vector2.Distance(Position,WorldLayout.ItemPositions[i])<4.2f && State.Collect((Relic)i))
                { selected=i; Notify("已尋回「"+ItemNames[i]+"」  ·  儀式碎片 "+State.Count+"／4。按 Tab 閱讀研究筆記。",5); }
            }
        }
        void CheckExit()
        {
            if(Time.unscaledTime<transitionUntil || Position.x<43 || Position.x>57) return;
            if(Room==0 && Position.y<7)
            {
                if(!State.Has(Relic.Plank)) { Notify("先取得桌旁的小木板，才能離開研究室。"); return; }
                StartCoroutine(ChangeRoom(1,new Vector2(50,55)));
                Notify("還缺少香蕉葉、年糕與盛酒器具。避開巡邏人物，繼續搜尋。",6);
            }
            else if(Room==1 && Position.y>57) StartCoroutine(ChangeRoom(0,new Vector2(50,10)));
            else if(Room==1 && Position.y<6) { StartCoroutine(ChangeRoom(2,new Vector2(50,10))); Notify("走到中央陳列桌前，整理找到的研究物件。",5); }
            else if(Room==2 && Position.y<5) StartCoroutine(ChangeRoom(1,new Vector2(50,10)));
            else if(Room==2 && Position.y>39.5f && Position.y<43)
            {
                if(!State.Finish()) Notify("研究資料尚未完整："+Missing()+"。可從下方出口返回探索區。",3);
            }
        }
        string Missing() { string s=""; for(int i=0;i<4;i++) if(!State.Has((Relic)i)) s+=(s.Length>0?"、":"")+ItemNames[i]; return s; }
        void OnApplicationFocus(bool focus) { if(!smoke && !focus && State.Status==Outcome.Playing) State.SetPaused(true); }
        void OnDestroy() { if(Instance==this) Instance=null; if(font!=null) Destroy(font); }

        void SetupStyles()
        {
            if(stylesReady) return;
            label=new GUIStyle(GUI.skin.label) { font=font, fontSize=22, wordWrap=true, normal={textColor=ink} };
            title=new GUIStyle(label) {fontSize=44,fontStyle=FontStyle.Bold};
            body=new GUIStyle(label) {fontSize=21,richText=true};
            small=new GUIStyle(label) {fontSize=17};
            button=new GUIStyle(GUI.skin.button) {font=font,fontSize=22,padding=new RectOffset(15,15,10,10),normal={textColor=paper,background=Texture2D.whiteTexture},hover={textColor=Color.white,background=Texture2D.whiteTexture},active={textColor=paper,background=Texture2D.whiteTexture},focused={textColor=paper,background=Texture2D.whiteTexture}};
            stylesReady=true;
        }
        void Box(Rect r, Color c) { Color old=GUI.color; GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=old; }
        void Text(float x,float y,float w,float h,string text,GUIStyle style=null,Color? color=null)
        {
            GUIStyle st=style??label; Color old=st.normal.textColor; st.normal.textColor=color??ink;
            GUI.Label(new Rect(x,y,w,h),text,st); st.normal.textColor=old;
        }
        bool Button(Rect r,string text) { Color old=GUI.backgroundColor; GUI.backgroundColor=ink; bool result=GUI.Button(r,text,button); GUI.backgroundColor=old; return result; }
        Vector2 ScreenPoint(Vector2 p) => new Vector2(mapRect.x+p.x/100f*mapRect.width,mapRect.y+(64-p.y)/64f*mapRect.height);
        Rect At(Vector2 p,float w,float h) { Vector2 s=ScreenPoint(p); return new Rect(s.x-w/2,s.y-h,w,h); }
        void Sprite(Rect dest,int index,Color tint)
        {
            if(atlas==null) return;
            // Four facing portraits occupy the upper 620 pixels; items occupy the lower 400.
            Rect source;
            if(index<4) source=new Rect(index*384,0,384,620);
            else if(index==4) source=new Rect(20,630,384,380);
            else if(index==5) source=new Rect(435,690,320,270);
            else if(index==6) source=new Rect(790,680,365,285);
            else source=new Rect(1180,630,356,390);
            Rect uv=new Rect(source.x/1536f,(1024-source.y-source.height)/1024f,source.width/1536f,source.height/1024f);
            float aspect=source.width/source.height;
            if(dest.width/dest.height>aspect) { float w=dest.height*aspect; dest.x+=(dest.width-w)/2; dest.width=w; }
            else { float h=dest.width/aspect; dest.y+=(dest.height-h)/2; dest.height=h; }
            Color old=GUI.color; GUI.color=tint; GUI.DrawTextureWithTexCoords(dest,atlas,uv,true); GUI.color=old;
        }
        int ItemSprite(int item) => item==0?5:item==1?4:item+4;
        void DrawWorld()
        {
            Box(new Rect(30,144,1137,732),gold);
            if(maps[Room]!=null) GUI.DrawTexture(mapRect,maps[Room],ScaleMode.StretchToFill);
            if(Room==2)
            {
                Vector2 s=ScreenPoint(new Vector2(50,46));
                Box(new Rect(s.x-100,s.y-35,200,46),new Color(.09f,.18f,.17f,.88f));
                Text(s.x-90,s.y-32,180,40,"研究資料整理處",small,paper);
            }
            for(int i=0;i<4;i++)
                if((i==0?0:1)==Room && !State.Has((Relic)i))
                {
                    Vector2 s=ScreenPoint(WorldLayout.ItemPositions[i]);
                    Box(new Rect(s.x-49,s.y-6,98,4),gold);
                    Sprite(At(WorldLayout.ItemPositions[i],77,80),ItemSprite(i),Color.white);
                    Box(new Rect(s.x-64,s.y+6,128,30),new Color(.07f,.16f,.14f,.88f));
                    Text(s.x-59,s.y+8,120,30,ItemNames[i],small,paper);
                }
            if(Room==1) for(int i=0;i<3;i++)
            {
                Vector2 s=ScreenPoint(npcs[i]);
                Sprite(At(npcs[i],56,90),0,new Color(1,.77f,.59f));
                Box(new Rect(s.x-28,s.y-111,56,27),new Color(.23f,.19f,.12f,.9f));
                Text(s.x-23,s.y-110,50,26,"巡邏",small,new Color(1,.92f,.76f));
            }
            Color playerTint=State.Immunity>0 && Mathf.FloorToInt(Time.unscaledTime*10)%2==0?new Color(1,1,1,.3f):Color.white;
            Rect player=At(Position,62,101); player.y+=Mathf.Sin(walking)*2;
            Sprite(player,facing,playerTint);
            if(debug)
            {
                foreach(Rect r in WorldLayout.Obstacles[Room]) { Vector2 p=ScreenPoint(new Vector2(r.x,r.y+r.height)); Box(new Rect(p.x,p.y,r.width/100*mapRect.width,r.height/64*mapRect.height),new Color(1,0,0,.25f)); }
            }
            Box(new Rect(42,151,250,36),new Color(.05f,.13f,.11f,.88f));
            Text(48,154,240,34,"0"+(Room+1)+"  /  "+RoomNames[Room],small,paper);
            Vector2 exit=ScreenPoint(new Vector2(50,4));
            Box(new Rect(exit.x-110,exit.y-10,220,32),new Color(.06f,.14f,.12f,.9f));
            Text(exit.x-103,exit.y-8,210,30,Room==0?"↓ 前往戶外探索區":Room==1?"↓ 前往展示區":"↓ 返回戶外探索區",small,paper);
            if(loading) Box(mapRect,new Color(.05f,.1f,.09f,.8f));
        }
        void OnGUI()
        {
            SetupStyles(); GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1600f,Screen.height/1000f,1));
            Box(new Rect(0,0,1600,1000),ink);
            Box(new Rect(0,0,1600,120),paper);
            Text(36,20,450,60,"歲末拾遺",title);
            Text(290,48,400,40,"PALILIN  /  一場三分鐘的尋訪",small);
            int seconds=Mathf.CeilToInt(State.Remaining);
            Text(970,27,180,60,(seconds/60).ToString("00")+":"+(seconds%60).ToString("00"),title,State.Remaining<30?new Color(.7f,.21f,.16f):ink);
            Text(1178,26,170,36,"生命  "+State.Hp+"／100",small);
            Box(new Rect(1184,70,188,7),new Color(.77f,.78f,.69f)); Box(new Rect(1184,70,188*State.Hp/100f,7),ink);
            if(Button(new Rect(1410,29,150,55),"暫停  Esc") && State.Status==Outcome.Playing) { journal=false; State.SetPaused(!State.Paused); }
            DrawWorld(); DrawSidebar();
            Text(38,907,1100,32,Time.unscaledTime<toastUntil?toast:"WASD／方向鍵 移動     ·     碰觸拾取     ·     Tab 研究筆記     ·     Esc 暫停",body,paper);
            Text(38,960,1500,28,"歲末祭祖文化主題探索  /  依第四組企劃製作  ·  所有插圖由 OpenAI 圖像生成",small,pale);
            if(State.Status==Outcome.Ready) DrawIntro();
            else if(State.Status==Outcome.Won||State.Status==Outcome.Lost) DrawResult();
            else if(journal) DrawJournal();
            else if(State.Paused) DrawPause();
        }
        void DrawSidebar()
        {
            Box(new Rect(1200,144,364,732),paper);
            Text(1224,168,320,32,"FIELD NOTES  /  研究任務",small);
            Text(1224,209,310,57,"儀式碎片  "+State.Count+"／4",new GUIStyle(title){fontSize=32});
            Text(1224,269,310,67,Room==0?"先找到小木板，再離開研究室。":State.Count<4?"尋回其餘物件，留意路上的巡邏人物。":"資料已完整。請前往展示區中央整理。",body);
            for(int i=0;i<4;i++)
            {
                float y=357+i*100;
                Box(new Rect(1220,y,324,87),State.Has((Relic)i)?new Color(.83f,.86f,.74f):new Color(.9f,.88f,.81f));
                Sprite(new Rect(1230,y+8,66,66),ItemSprite(i),State.Has((Relic)i)?Color.white:new Color(1,1,1,.46f));
                Text(1310,y+12,225,34,ItemNames[i],body);
                Text(1310,y+47,218,30,State.Has((Relic)i)?"已尋回  ·  可閱讀筆記":"尚待尋訪",small);
                if(GUI.Button(new Rect(1220,y,324,87),GUIContent.none,GUIStyle.none) && State.Status==Outcome.Playing)
                { selected=i; journal=true; State.SetPaused(true); }
            }
            if(Button(new Rect(1220,792,324,56),"打開研究筆記  Tab") && State.Status==Outcome.Playing) { journal=true; State.SetPaused(true); }
        }
        void Modal(float x,float y,float w,float h)
        { Box(new Rect(0,120,1600,880),new Color(.02f,.07f,.06f,.78f)); Box(new Rect(x-3,y-3,w+6,h+6),gold); Box(new Rect(x,y,w,h),paper); }
        void DrawIntro()
        {
            Modal(300,200,1000,640);
            Text(360,237,850,35,"第四組  /  UNITY 2D 限時探索遊戲",small);
            Text(360,292,850,75,"歲末拾遺",new GUIStyle(title){fontSize=64});
            Text(360,390,860,105,"距離展示資料整理完成，只剩最後三分鐘。\n帶著研究筆記，尋回四項物件，完成最後的研究任務。",new GUIStyle(body){fontSize=25});
            Text(360,520,870,100,"01  研究室拿取小木板\n02  戶外尋找香蕉葉、糯米糕／年糕、酒與酒杯\n03  集齊四項，抵達展示區中央",body);
            Text(360,636,870,55,"WASD 移動  ·  碰觸拾取  ·  Esc 暫停\n180 秒倒數  /  接觸巡邏人物扣 20 HP",small);
            if(Button(new Rect(360,728,380,64),"開始尋訪  →")) Begin();
            if(Button(new Rect(782,728,380,64),"離開遊戲")) Application.Quit();
        }
        void DrawPause()
        {
            Modal(500,300,600,440); Text(550,340,500,70,"稍作停留",title);
            Text(550,430,500,60,"倒數與巡邏已暫停。",body);
            if(Button(new Rect(550,510,500,60),"繼續尋訪")) State.SetPaused(false);
            if(Button(new Rect(550,590,500,60),"重新開始")) Begin();
            if(Button(new Rect(550,665,500,45),"離開遊戲")) Application.Quit();
        }
        void DrawJournal()
        {
            Modal(300,200,1000,650);
            Text(350,237,880,70,"研究筆記",title);
            for(int i=0;i<4;i++) if(Button(new Rect(350+i*227,331,213,55),ItemNames[i])) selected=i;
            Sprite(new Rect(360,425,200,200),ItemSprite(selected),State.Has((Relic)selected)?Color.white:new Color(1,1,1,.3f));
            Text(600,425,620,60,ItemNames[selected],new GUIStyle(title){fontSize=34});
            Text(600,500,620,145,State.Has((Relic)selected)?ItemNotes[selected]:"尚未取得這項物件。\n繼續探索，將資料帶回展示區。",body);
            Text(350,657,890,82,"本作依企劃以噶瑪蘭族 Palilin 歲末祭祖文化為背景。物件與動線經遊戲化處理，並非完整祭儀重現。文化參考：原住民族委員會「噶瑪蘭族－祭儀」與原視新聞網相關報導。",small);
            if(Button(new Rect(350,760,890,57),"收起筆記，繼續探索  /  Tab 或 Esc")) { journal=false; State.SetPaused(false); }
        }
        void DrawResult()
        {
            bool won=State.Status==Outcome.Won;
            Modal(380,210,840,640);
            Text(435,250,720,40,won?"RESEARCH COMPLETE  /  尋訪完成":"EXPEDITION ENDED  /  尋訪結束",small);
            Text(435,320,740,85,won?"把歲末的記憶，拾回。":State.Hp==0?"生命耗盡，稍作休息。":"時間到了，再試一次。",new GUIStyle(title){fontSize=37});
            Text(435,427,720,80,"儀式碎片  "+State.Count+"／4    ·    剩餘 "+Mathf.FloorToInt(State.Remaining)+" 秒\n生命值  "+State.Hp+"    ·    受傷 "+hits+" 次",body);
            Text(435,545,730,70,"結算分數    "+State.Score.ToString("N0"),title);
            Text(435,628,720,56,"物件數 × 100 ＋ 剩餘整秒 × 10 ＋ 剩餘 HP × 5",small);
            if(Button(new Rect(435,735,340,65),"再次尋訪")) Begin();
            if(Button(new Rect(815,735,340,65),"離開遊戲")) Application.Quit();
        }

        IEnumerator SmokeTest()
        {
            yield return new WaitForSeconds(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"..","title-preview.png"));
            yield return new WaitForSeconds(.4f);
            Begin(); while(loading) yield return null;
            yield return new WaitForSeconds(.8f);
            string path=Path.Combine(Application.dataPath,"..","smoke-test.txt");
            try
            {
                if(atlas==null || Array.Exists(maps,t=>t==null)) throw new Exception("Missing generated art");
                if(SceneManager.GetActiveScene().name!="ResearchRoom") throw new Exception("Research scene did not load");
                Position=new Vector2(50,4); CheckExit();
                if(loading||Room!=0) throw new Exception("Research exit must require plank");
                Position=WorldLayout.ItemPositions[0]; CheckPickups(); CheckPickups();
                if(State.Count!=1) throw new Exception("Duplicate pickup");
                if(!State.Has(Relic.Plank)) throw new Exception("Plank pickup failed");
                float before=State.Remaining; State.SetPaused(true); State.Tick(5); if(State.Remaining!=before) throw new Exception("Pause timer"); State.SetPaused(false);
                Position=new Vector2(50,4); CheckExit();
            } catch(Exception e) { File.WriteAllText(path,"FAIL "+e); Application.Quit(1); yield break; }
            while(loading) yield return null;
            yield return new WaitForSeconds(.8f);
            try
            {
                if(!State.Has(Relic.Plank)||SceneManager.GetActiveScene().name!="Outdoors") throw new Exception("Cross scene persistence");
                Position=new Vector2(50,4); CheckExit();
            } catch(Exception e) { File.WriteAllText(path,"FAIL "+e); Application.Quit(1); yield break; }
            while(loading) yield return null;
            yield return new WaitForSeconds(.8f);
            try
            {
                if(Room!=2||SceneManager.GetActiveScene().name!="Exhibition") throw new Exception("Exhibition exit trigger");
                Position=new Vector2(50,41); CheckExit();
                if(State.Status!=Outcome.Playing) throw new Exception("Incomplete inventory must not win");
                Position=new Vector2(50,4); CheckExit();
            } catch(Exception e) { File.WriteAllText(path,"FAIL "+e); Application.Quit(1); yield break; }
            while(loading) yield return null;
            yield return new WaitForSeconds(.8f);
            try
            {
                if(Room!=1||!State.Has(Relic.Plank)) throw new Exception("Return to exploration persistence");
                for(int i=1;i<4;i++) { Position=WorldLayout.ItemPositions[i]; CheckPickups(); }
                TakeDamage(); TakeDamage(); if(State.Hp!=80||hits!=1) throw new Exception("Damage immunity");
                Position=new Vector2(50,4); CheckExit();
            } catch(Exception e) { File.WriteAllText(path,"FAIL "+e); Application.Quit(1); yield break; }
            while(loading) yield return null;
            yield return new WaitForSeconds(.8f);
            try
            {
                Position=new Vector2(50,41); CheckExit();
                if(State.Status!=Outcome.Won) throw new Exception("Win trigger");
                File.WriteAllText(path,"PASS: generated art; three actual scene exits; locked research exit; unique pickups; cross scene persistence; pause; incomplete inventory rejection and return; damage immunity; actual win trigger and scoring. Score="+State.Score);
                ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"..","smoke-result.png"));
            } catch(Exception e) { File.WriteAllText(path,"FAIL "+e); Application.Quit(1); yield break; }
            yield return new WaitForSeconds(.5f);
            // Capture representative play screens as well as the result.
            Begin(); while(loading) yield return null;
            yield return new WaitForSeconds(.4f); ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"..","research-preview.png"));
            yield return new WaitForSeconds(.4f); journal=true; selected=0; State.Collect(Relic.Plank); State.SetPaused(true);
            yield return new WaitForSeconds(.4f); ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"..","journal-preview.png"));
            yield return new WaitForSeconds(.4f); journal=false; State.SetPaused(false);
            yield return new WaitForSeconds(.4f); State.Collect(Relic.Plank);
            yield return ChangeRoom(1,new Vector2(50,49));
            yield return new WaitForSeconds(.4f); ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"..","outdoor-preview.png"));
            yield return new WaitForSeconds(.5f); Application.Quit(0);
        }
    }
}
