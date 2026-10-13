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
            Time.timeScale=1;
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
