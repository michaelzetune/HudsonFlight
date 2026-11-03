using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace HudsonFlight
{
    // Opt-in executable test harness. Never runs during an ordinary flight.
    public sealed class RuntimeSmoke : MonoBehaviour
    {
        IEnumerator Start()
        {
            var game=GetComponent<FlightGame>();
            game.Paused=false;
            yield return new WaitForSecondsRealtime(3);
            string dir=Environment.GetEnvironmentVariable("HUDSON_CAPTURE_DIR")??Application.persistentDataPath;
            Directory.CreateDirectory(dir);
            if(!string.IsNullOrEmpty(game.Failure)){Debug.LogError("SMOKE FAIL: "+game.Failure);Application.Quit(1);yield break;}
            foreach(var landmark in new[]{new Vector3(-9534.77f,600,-3205.57f),new Vector3(-11848.67f,600,-7167.95f)})
            {
                if(!Physics.Raycast(landmark,Vector3.down,out var roof,400,1<<WorldBuilder.ObstacleLayer) || roof.point.y<300)
                {Debug.LogError("SMOKE FAIL: missing landmark roof collision");Application.Quit(1);yield break;}
                Debug.Log($"LANDMARK COLLISION PASS: {roof.collider.name} at {roof.point.y:0.0} m");
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"cockpit.png"));
            yield return new WaitForSecondsRealtime(1);
            game.Help=true;
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"help.png"));
            yield return new WaitForSecondsRealtime(1);
            game.Help=false;game.Chase=true;
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"chase.png"));
            yield return new WaitForSecondsRealtime(1);
            if(!game.OnGround || game.Rpm<400 || float.IsNaN(game.Altitude) || game.LandingReport!="")
            {Debug.LogError("SMOKE FAIL: invalid runway state");Application.Quit(1);yield break;}
            game.ResetFlight();
            yield return new WaitForSecondsRealtime(.5f);
            game.SetTestFlight(Geography.Position(40.788,-73.934),235,1800,90,16);
            Time.timeScale=0;game.Chase=false;
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"manhattan.png"));
            yield return new WaitForSecondsRealtime(1);
            game.Chase=true;game.Instruments=false;game.Hints=false;
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"manhattan-chase.png"));
            yield return new WaitForSecondsRealtime(1);
            float frameTime=0;
            for(int frame=0;frame<60;frame++){yield return null;frameTime+=Time.unscaledDeltaTime;}
            Debug.Log($"RENDER PERFORMANCE: {60/frameTime:0.0} FPS average over Manhattan at {Screen.width} × {Screen.height}");
            // Close views expose placement, winding, roof alignment, and landmark textures.
            game.SetTestFlight(Geography.Position(40.748,-73.974),273,1650,90,12);
            Time.timeScale=0;game.Chase=true;game.Instruments=false;game.Hints=false;
            yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"empire-state.png"));
            yield return new WaitForSecondsRealtime(1);
            game.SetTestFlight(Geography.Position(40.749,-73.960),300,1900,90,14);
            Time.timeScale=0;
            yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"midtown.png"));
            yield return new WaitForSecondsRealtime(1);
            game.SetTestFlight(Geography.Position(40.709,-73.997),288,2200,90,12);
            Time.timeScale=0;
            yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"world-trade-center.png"));
            yield return new WaitForSecondsRealtime(1);
            game.SetTestFlight(Geography.Position(40.697,-74.032),209,650,90,12);
            Time.timeScale=0;
            yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"liberty.png"));
            yield return new WaitForSecondsRealtime(1);
            Screen.SetResolution(1280,800,FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(1);
            game.Instruments=true;game.Hints=true;game.Chase=false;
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"resized.png"));
            yield return new WaitForSecondsRealtime(1);
            if(game.View.rect!=new Rect(0,0,1,1)){Debug.LogError("SMOKE FAIL: viewport does not fill window");Application.Quit(1);yield break;}
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hudson-visual")>=0)
            {Debug.Log("VISUAL PASS: both cameras, help, instruments hidden, resized viewport");Application.Quit(0);yield break;}
            Time.timeScale=1;
            game.ResetFlight();game.Ready=false;game.Throttle=1;game.Assistance=true;Time.timeScale=3;
            float assistStart=Time.time;
            float maxGroundDeviation=0;
            while(Time.time-assistStart<32 && !game.Crashed)
            {
                // Only pitch is piloted: exercise the exact assistance used in normal play.
                game.Elevator=Mathf.Clamp((game.Pitch-(game.Airspeed>55?8:3))*.03f+game.PitchRate*.4f,-.5f,.5f);
                if(game.OnGround)maxGroundDeviation=Mathf.Max(maxGroundDeviation,Mathf.Abs(Mathf.DeltaAngle(game.Departure.Heading,game.Heading)));
                yield return new WaitForFixedUpdate();
            }
            if(game.Crashed || game.Agl<50 || Mathf.Abs(game.Roll)>3 || maxGroundDeviation>5)
            {Debug.LogError($"SMOKE FAIL: assisted takeoff: {game.Failure}, bank {game.Roll}, deviation {maxGroundDeviation}");Application.Quit(1);yield break;}
            Debug.Log($"ASSIST TAKEOFF PASS: {game.Agl:0} ft AGL, bank {game.Roll:0.0}°, runway heading error {maxGroundDeviation:0.0}°");
            game.Aileron=-.55f;assistStart=Time.time;
            while(Time.time-assistStart<5)
            {
                game.Elevator=Mathf.Clamp((game.Pitch-4)*.03f+game.PitchRate*.4f,-.5f,.5f);
                yield return new WaitForFixedUpdate();
            }
            float commandedBank=game.Roll;game.Aileron=0;assistStart=Time.time;
            while(Time.time-assistStart<12)
            {
                game.Elevator=Mathf.Clamp((game.Pitch-4)*.03f+game.PitchRate*.4f,-.5f,.5f);
                yield return new WaitForFixedUpdate();
            }
            if(game.Crashed || commandedBank> -15 || Mathf.Abs(game.Roll)>3)
            {Debug.LogError($"SMOKE FAIL: bank recovery {commandedBank} -> {game.Roll}");Application.Quit(1);yield break;}
            Debug.Log($"ASSIST RECOVERY PASS: commanded {commandedBank:0.0}°, released {game.Roll:0.0}°");
            game.ResetFlight();
            if(!game.OnGround || game.Airspeed>1 || Mathf.Abs(game.Roll)>2)
            {Debug.LogError("SMOKE FAIL: assisted restart retained flight state");Application.Quit(1);yield break;}
            Debug.Log("ASSIST RESET PASS: stationary, level, on runway");
            game.Assistance=false;
            game.ResetFlight();game.Ready=false;game.Throttle=1;Time.timeScale=3;
            float start=Time.time;
            while(Time.time-start<32 && !game.Crashed)
            {
                Pilot(game,game.Airspeed>55?8:3);
                yield return new WaitForFixedUpdate();
            }
            Time.timeScale=1;
            if(game.Crashed || game.Agl<50){Debug.LogError("SMOKE FAIL: takeoff: "+game.Failure);Application.Quit(1);yield break;}
            Debug.Log($"TAKEOFF PASS: {game.Airspeed:0} kt, {game.Agl:0} ft AGL");
            Vector3 direction=(game.Departure.B-game.Departure.A).normalized;
            game.SetTestFlight(game.Departure.A+direction*100,game.Departure.Heading,120,65);
            game.Flaps=2f/3;game.Throttle=.15f;Time.timeScale=3;start=Time.time;
            while(Time.time-start<30 && !game.Crashed && game.LandingReport=="")
            {
                Pilot(game,game.Agl<16?6:0);
                if(game.Agl<16)game.Throttle=0;
                yield return new WaitForFixedUpdate();
            }
            Time.timeScale=1;
            if(game.Crashed || game.LandingReport==""){Debug.LogError("SMOKE FAIL: landing: "+game.Failure);Application.Quit(1);yield break;}
            game.Paused=true;
            Debug.Log("LANDING PASS: "+game.LandingReport);
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"landing.png"));
            yield return new WaitForSecondsRealtime(1);
            game.SetTestFlight(game.Departure.A+direction*100,game.Departure.Heading,120,85);
            game.Throttle=0;game.Elevator=.4f;Time.timeScale=3;start=Time.time;
            while(Time.time-start<20 && !game.Crashed)yield return new WaitForFixedUpdate();
            Time.timeScale=1;
            if(!game.Crashed){Debug.LogError("SMOKE FAIL: hard impact did not stop flight");Application.Quit(1);yield break;}
            Debug.Log("CRASH PASS: "+game.Failure);
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"crash.png"));
            yield return new WaitForSecondsRealtime(1);
            game.ResetFlight();
            if(game.Crashed || !game.Ready || game.LandingReport!=""){Debug.LogError("SMOKE FAIL: crash reset");Application.Quit(1);yield break;}
            Debug.Log("SMOKE PASS: native plugin; runway contact; engine idle; camera/help screenshots; takeoff; landing feedback; hard-impact crash; restart.");
            Application.Quit(0);
        }
        static void Pilot(FlightGame game,float targetPitch)
        {
            game.Elevator=Mathf.Clamp((game.Pitch-targetPitch)*.03f+game.PitchRate*.4f,-.5f,.5f);
            game.Aileron=Mathf.Clamp(-game.Roll*.025f-game.RollRate*.3f,-.5f,.5f);
            game.Rudder=-Mathf.Clamp(Mathf.DeltaAngle(game.Departure.Heading,game.Heading)*.03f+game.YawRate*.5f,-.5f,.5f);
        }
    }
}
