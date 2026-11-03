using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HudsonFlight
{
    public static class EditorBuild
    {
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            // Preserve the fog variant used by the runtime-built world.
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Flight.unity");
            PlayerSettings.companyName="HudsonFlight";PlayerSettings.productName="Hudson Flight";
            PlayerSettings.bundleVersion="0.3.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.hudsonflight.simulator");
            PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone,1);
            UnityEditor.OSXStandalone.UserBuildSettings.architecture=OSArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX,new[]{GraphicsDeviceType.Metal});
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=false;
            PlayerSettings.colorSpace=ColorSpace.Linear;
            Directory.CreateDirectory("Assets/Resources");
            if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/FlightSurface.mat")==null)
                AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")),"Assets/Resources/FlightSurface.mat");
            var importer=(PluginImporter)AssetImporter.GetAtPath("Assets/Plugins/macOS/libHudsonFlight.dylib");
            importer.SetCompatibleWithAnyPlatform(false);importer.SetCompatibleWithEditor(true);
            importer.SetEditorData("OS","OSX");importer.SetEditorData("CPU","ARM64");
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX,true);
            importer.SetPlatformData(BuildTarget.StandaloneOSX,"CPU","ARM64");importer.SaveAndReimport();
            // Verify P/Invoke inside Unity itself before producing the app.
            using(var flight=new FlightDynamics(Path.Combine(Application.streamingAssetsPath,"JSBSim"),40.77,-73.88,31,21))
            {
                flight.Set("fcs/left-brake-cmd-norm",1);flight.Set("fcs/right-brake-cmd-norm",1);
                flight.Step(120);
                if(flight.Get("propulsion/engine/engine-rpm")<400)throw new Exception("Native engine test failed");
            }
            AssetDatabase.SaveAssets();
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=new[]{"Assets/Scenes/Flight.unity"},locationPathName="Build/Hudson Flight.app",
                target=BuildTarget.StandaloneOSX,options=BuildOptions.None
            });
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Mac build failed: "+result.summary.result);
            Debug.Log("HUDSON BUILD SUCCEEDED: "+result.summary.outputPath);
        }
    }
}
