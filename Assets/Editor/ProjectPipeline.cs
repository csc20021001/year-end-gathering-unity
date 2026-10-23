using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using YearEnd;

public static class ProjectPipeline
{
    static int checks;
    static void Check(bool condition,string message)
    { if(!condition) throw new Exception("FAIL: "+message); checks++; Debug.Log("PASS: "+message); }

    [MenuItem("歲末拾遺/驗證規則與關卡")]
    public static void Verify()
    {
        checks=0;
        var s=new ExpeditionState(); Check(s.Status==Outcome.Ready,"Title does not start timer"); s.Start();
        Check(s.Hp==100 && s.Remaining==180 && s.Count==0,"Initial values");
        Check(!s.Finish(),"Cannot finish empty inventory");
        Check(s.Collect(Relic.Plank)&&!s.Collect(Relic.Plank)&&s.Count==1,"Unique item collection");
        s.SetPaused(true); s.Tick(30); Check(s.Remaining==180,"Pause freezes timer");
        Check(!s.Damage()&&!s.Collect(Relic.Leaf),"Pause freezes interactions");
        s.SetPaused(false); Check(s.Damage()&&s.Hp==80,"Hazard removes 20HP");
        Check(!s.Damage()&&s.Hp==80,"Repeated contact respects immunity");
        s.Tick(1.24f); Check(!s.Damage(),"Immunity lasts 1.25 seconds");
        s.Tick(.02f); Check(s.Damage()&&s.Hp==60,"Immunity expires");
        foreach(Relic r in Enum.GetValues(typeof(Relic))) s.Collect(r);
        Check(s.Count==4,"Four item progress"); Check(s.Score==400+1780+300,"Score floors remaining seconds");
        Check(s.Finish()&&s.Status==Outcome.Won,"Complete positive state wins");
        float saved=s.Remaining; s.Tick(20); Check(s.Remaining==saved&&!s.Damage(),"Result freezes simulation");
        s.Start(); s.Tick(180); Check(s.Remaining==0&&s.Status==Outcome.Lost,"Timeout loses");
        Check(!s.Collect(Relic.Plank)&&!s.Finish(),"Timeout cannot collect or win");
        s.Start(); for(int i=0;i<5;i++) { s.Damage(); s.Tick(1.26f); }
        Check(s.Hp==0&&s.Status==Outcome.Lost&&!s.Finish(),"Zero HP loses");
        s.Start(); Check(s.Count==0&&s.Hp==100&&s.Immunity==0&&!s.Paused,"Restart clears all session state");
        s.Tick(-1); s.Tick(float.NaN); s.Tick(float.PositiveInfinity); Check(s.Remaining==180,"Invalid deltas ignored");
        Check(WorldLayout.Move(1,new Vector2(30,34),new Vector2(50,0)).x<34,"Substeps prevent obstacle tunnelling");
        Check(WorldLayout.Move(0,new Vector2(50,10),new Vector2(0,-100)).y>=3,"Map boundary blocks movement");
        for(int room=0;room<3;room++)
        {
            Vector2 spawn=room==0?new Vector2(50,26):room==1?new Vector2(50,55):new Vector2(50,10);
            Check(!WorldLayout.Blocked(room,spawn),"Scene "+room+" spawn clear");
            Check(WorldLayout.Blocked(room,new Vector2(35,6)),"Scene "+room+" bottom wall blocks passage outside doorway");
            var reached=Reach(room,spawn);
            Check(reached.Contains(new Vector2Int(50,4)),"Scene "+room+" exit reachable");
            if(room==0) Check(reached.Contains(Vector2Int.RoundToInt(WorldLayout.ItemPositions[0])),"Plank reachable");
            if(room==1)
                for(int i=1;i<4;i++) Check(reached.Contains(Vector2Int.RoundToInt(WorldLayout.ItemPositions[i])),"Item "+i+" reachable");
            if(room==2) Check(reached.Contains(new Vector2Int(50,41)),"Exhibit goal reachable");
        }
        for(int i=0;i<WorldLayout.PatrolRoutes.Length;i++)
        {
            Vector2[] route=WorldLayout.PatrolRoutes[i]; bool clear=true;
            for(int j=0;j<route.Length;j++) for(int k=0;k<=100;k++)
                if(WorldLayout.Blocked(1,Vector2.Lerp(route[j],route[(j+1)%route.Length],k/100f))) clear=false;
            Check(clear,"Patrol "+i+" stays outside obstacles");
        }
        foreach(string name in new [] { "atlas","research","outdoors","exhibition" })
        {
            var t=Resources.Load<Texture2D>("Art/"+name); Check(t!=null && t.width>=1024,"Generated asset: "+name);
        }
        string report="PASS: "+checks+" assertions\nUnity "+Application.unityVersion+"\nRules, collision, traversability, patrol routes and generated art verified.\n";
        File.WriteAllText(Path.Combine(Application.dataPath,"..","Verification.txt"),report);
        Debug.Log(report);
    }
    static HashSet<Vector2Int> Reach(int room,Vector2 spawn)
    {
        var visited=new HashSet<Vector2Int>(); var q=new Queue<Vector2Int>();
        Vector2Int first=Vector2Int.RoundToInt(spawn); q.Enqueue(first); visited.Add(first);
        Vector2Int[] directions={Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
        while(q.Count>0)
        {
            Vector2Int p=q.Dequeue();
            foreach(var d in directions) { Vector2Int next=p+d; if(!visited.Contains(next)&&!WorldLayout.Blocked(room,next)) { visited.Add(next); q.Enqueue(next); } }
        }
        return visited;
    }

    [MenuItem("歲末拾遺/建立場景與 Windows 版本")]
    public static void BuildAndVerify()
    {
        PlayerSettings.companyName="Group Four"; PlayerSettings.productName="歲末拾遺";
        PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=1000;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.resizableWindow=true;
        PlayerSettings.runInBackground=false;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.colorSpace=ColorSpace.Gamma;
        PlayerSettings.bundleVersion="1.0.0";
        PlayerSettings.SplashScreen.show=false;
        Directory.CreateDirectory("Assets/Scenes");
        var scenes=new List<EditorBuildSettingsScene>();
        foreach(string sceneName in WorldLayout.Scenes)
        {
            string scenePath="Assets/Scenes/"+sceneName+".unity";
            if(!File.Exists(scenePath))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var camera=new GameObject("Main Camera").AddComponent<Camera>(); camera.tag="MainCamera";
                camera.orthographic=true; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.12f,.22f,.21f);
                EditorSceneManager.SaveScene(scene,scenePath);
            }
            scenes.Add(new EditorBuildSettingsScene(scenePath,true));
        }
        EditorBuildSettings.scenes=scenes.ToArray();
        foreach(string path in AssetDatabase.FindAssets("t:Texture2D",new [] { "Assets/Resources/Art" }))
        {
            string file=AssetDatabase.GUIDToAssetPath(path); var importer=(TextureImporter)AssetImporter.GetAtPath(file);
            importer.textureType=TextureImporterType.Default; importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false; importer.maxTextureSize=2048; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets(); Verify();
        string buildDir=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","Windows")); Directory.CreateDirectory(buildDir);
        BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=scenes.ConvertAll(x=>x.path).ToArray(),locationPathName=Path.Combine(buildDir,"YearEndGathering.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Build failed: "+report.summary.result+" errors="+report.summary.totalErrors);
        File.AppendAllText(Path.Combine(Application.dataPath,"..","Verification.txt"),"Windows x64 build succeeded; errors="+report.summary.totalErrors+"; warnings="+report.summary.totalWarnings+"\n");
        Debug.Log("BUILD_SUCCESS "+report.summary.totalSize+" bytes");
    }
}
