using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace HudsonFlight
{
    public sealed class FlightHud : MonoBehaviour
    {
        FlightGame game;
        Texture2D face,attitude,map;
        GUIStyle text,small,big,button,center;
        readonly Color ink=new Color(.035f,.05f,.065f,1),cream=new Color(.89f,.92f,.9f),muted=new Color(.52f,.62f,.65f),cyan=new Color(.36f,.87f,.83f),gold=new Color(1f,.73f,.35f);
        float nextAttitude, width=1440, height=900;
        readonly Color shade=new Color(.025f,.04f,.055f,.76f);
        void Awake(){game=GetComponent<FlightGame>();}
        void Init()
        {
            text=new GUIStyle(GUI.skin.label){fontSize=17};text.normal.textColor=cream;
            small=new GUIStyle(text){fontSize=12};big=new GUIStyle(text){fontSize=28,fontStyle=FontStyle.Bold};
            center=new GUIStyle(text){alignment=TextAnchor.MiddleCenter};
            button=new GUIStyle(GUI.skin.button){fontSize=14,padding=new RectOffset(10,10,8,8)};
            button.normal.background=Solid(new Color(.09f,.15f,.18f));button.normal.textColor=cream;
            button.hover.background=Solid(new Color(.14f,.25f,.28f));button.hover.textColor=Color.white;
            button.active.background=Solid(new Color(.12f,.32f,.33f));button.active.textColor=Color.white;
            face=new Texture2D(192,192,TextureFormat.RGBA32,false);
            for(int y=0;y<192;y++)for(int x=0;x<192;x++)
            {
                float d=Vector2.Distance(new Vector2(x,y),new Vector2(95.5f,95.5f));
                face.SetPixel(x,y,d>95?Color.clear:d>90?new Color(.22f,.26f,.27f):d>84?Color.black:new Color(.055f,.067f,.071f));
            }
            face.Apply();attitude=new Texture2D(192,192,TextureFormat.RGBA32,false);
            string file=Path.Combine(Application.streamingAssetsPath,"Scenery/gps.png");
            if(File.Exists(file)){map=new Texture2D(2,2);map.LoadImage(File.ReadAllBytes(file));map.wrapMode=TextureWrapMode.Clamp;}
        }
        Texture2D Solid(Color c){var t=new Texture2D(1,1);t.SetPixel(0,0,c);t.Apply();return t;}
        void Box(Rect r,Color color){Color old=GUI.color;GUI.color=color.linear;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void Label(Rect r,string value,GUIStyle style=null,Color? color=null)
        {
            GUIStyle s=style??text;Color old=s.normal.textColor;s.normal.textColor=color??cream;GUI.Label(r,value,s);s.normal.textColor=old;
        }
        void Line(Vector2 a,Vector2 b,Color color,float width=2)
        {
            Matrix4x4 old=GUI.matrix;Vector2 delta=b-a;
            GUI.matrix=old*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg),Vector3.one);
            Box(new Rect(0,-width/2,delta.magnitude,width),color);GUI.matrix=old;
        }
        void MapLine(Rect clip,Vector2 a,Vector2 b,Color color,float thickness)
        {
            // Clip before drawing so scaling never depends on IMGUI's group transform.
            Vector2 d=b-a;float lo=0,hi=1;
            float[] p={-d.x,d.x,-d.y,d.y},q={a.x-clip.x,clip.xMax-a.x,a.y-clip.y,clip.yMax-a.y};
            for(int i=0;i<4;i++)
            {
                if(Mathf.Abs(p[i])<.0001f){if(q[i]<0)return;continue;}
                float t=q[i]/p[i];if(p[i]<0)lo=Mathf.Max(lo,t);else hi=Mathf.Min(hi,t);
                if(lo>hi)return;
            }
            Line(a+d*lo,a+d*hi,color,thickness);
        }
        Vector2 Radial(Vector2 center,float angle,float radius)
        {float a=angle*Mathf.Deg2Rad;return center+new Vector2(Mathf.Sin(a),-Mathf.Cos(a))*radius;}
        void Needle(Vector2 c,float angle,float radius,Color color)
        {Line(Radial(c,angle,-9),Radial(c,angle,radius),color,2.5f);Box(new Rect(c.x-4,c.y-4,8,8),color);}
        void Dial(Vector2 c,string title,float value,float min,float max,string format,string unit)
        {
            const float radius=58;
            GUI.DrawTexture(new Rect(c.x-radius,c.y-radius,radius*2,radius*2),face);
            for(int i=0;i<=10;i++)
            {
                float a=-135+i*27;Line(Radial(c,a,46),Radial(c,a,i%2==0?38:42),muted,1.5f);
                if(i%2==0)
                {
                    Vector2 p=Radial(c,a,30);
                    Label(new Rect(p.x-17,p.y-8,34,16),Mathf.Lerp(min,max,i/10f).ToString("0"),new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=9});
                }
            }
            Needle(c,Mathf.Lerp(-135,135,Mathf.InverseLerp(min,max,value)),43,cream);
            Label(new Rect(c.x-60,c.y-83,120,20),title,new GUIStyle(small){alignment=TextAnchor.MiddleCenter},muted);
            Box(new Rect(c.x-34,c.y+13,68,19),Color.black);
            Label(new Rect(c.x-34,c.y+12,68,21),value.ToString(format),new GUIStyle(center){fontSize=14},cyan);
            Label(new Rect(c.x-40,c.y+36,80,18),unit,new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=9},muted);
        }
        void AttitudeIndicator(Vector2 c)
        {
            if(Time.unscaledTime>=nextAttitude)
            {
                nextAttitude=Time.unscaledTime+.08f;
                float angle=-game.Roll*Mathf.Deg2Rad;
                for(int y=0;y<192;y++)for(int x=0;x<192;x++)
                {
                    float xx=x-95.5f,yy=y-95.5f,d=Mathf.Sqrt(xx*xx+yy*yy);
                    float horizon=yy*Mathf.Cos(angle)+xx*Mathf.Sin(angle)+game.Pitch*2;
                    Color color=horizon>0?new Color(.21f,.43f,.57f):new Color(.42f,.29f,.17f);
                    if(Mathf.Abs(horizon)<1.3f)color=cream;
                    if(d>95)color=Color.clear;else if(d>85)color=new Color(.14f,.19f,.20f);
                    attitude.SetPixel(x,y,color);
                }
                attitude.Apply();
            }
            GUI.DrawTexture(new Rect(c.x-58,c.y-58,116,116),attitude);
            Line(c+new Vector2(-32,0),c+new Vector2(-9,0),gold,3);Line(c+new Vector2(9,0),c+new Vector2(32,0),gold,3);
            Line(c+new Vector2(-9,0),c+new Vector2(0,5),gold,3);Line(c+new Vector2(0,5),c+new Vector2(9,0),gold,3);
            Label(new Rect(c.x-60,c.y-83,120,20),"ATTITUDE",new GUIStyle(small){alignment=TextAnchor.MiddleCenter},muted);
            Label(new Rect(c.x-48,c.y+30,96,18),$"{game.Pitch:+0;-0;0}°   {game.Roll:+0;-0;0}°",new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=10});
        }
        void HeadingIndicator(Vector2 c)
        {
            GUI.DrawTexture(new Rect(c.x-58,c.y-58,116,116),face);
            for(int i=0;i<36;i++)
            {
                float a=i*10-game.Heading;
                Line(Radial(c,a,48),Radial(c,a,i%3==0?40:44),muted,1.3f);
                if(i%3==0)
                {
                    string t=i==0?"N":i==9?"E":i==18?"S":i==27?"W":(i*10).ToString();Vector2 p=Radial(c,a,31);
                    Label(new Rect(p.x-16,p.y-9,32,18),t,new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=10});
                }
            }
            Line(c+new Vector2(0,-14),c+new Vector2(0,17),gold,2);Line(c+new Vector2(-16,0),c+new Vector2(16,0),gold,2);
            Label(new Rect(c.x-65,c.y-83,130,20),"HEADING · TRUE",new GUIStyle(small){alignment=TextAnchor.MiddleCenter},muted);
            Box(new Rect(c.x-26,c.y+32,52,20),Color.black);Label(new Rect(c.x-26,c.y+30,52,23),game.Heading.ToString("000")+"°",new GUIStyle(center){fontSize=13},cyan);
        }
        Vector2 MapPoint(Vector3 p,Rect r)
        {
            Vector3 offset=p-game.Position;
            return new Vector2(r.center.x+offset.x/22000*r.width,r.center.y-offset.z/22000*r.width);
        }
        void GPS(Rect r)
        {
            Box(new Rect(r.x-12,r.y-38,r.width+24,r.height+68),shade);
            Label(new Rect(r.x,r.y-30,180,24),"GPS / NEW YORK",small,cyan);
            Label(new Rect(r.xMax-60,r.y-30,60,24),"N ↑",small,muted);
            Box(r,new Color(.09f,.16f,.2f));
            if(map!=null)
            {
                Rect uv=new Rect((game.Position.x-11000+35000)/70000,(game.Position.z-11000*r.height/r.width+35000)/70000,22000f/70000,22000f/70000*r.height/r.width);
                GUI.DrawTextureWithTexCoords(r,map,uv);
            }
            Rect local=r;
            if(game.Destination!=null)
            {
                Vector2 target=MapPoint(game.ApproachStart,local);
                MapLine(local,local.center,target,cyan,1);
                MapLine(local,target,MapPoint(game.ApproachStart-game.ApproachDirection*7000,local),gold,1.5f);
            }
            var labeled=new HashSet<string>();
            foreach(var runway in game.World.Runways)
            {
                Vector2 a=MapPoint(runway.A,local),b=MapPoint(runway.B,local);
                MapLine(local,a,b,cream,2);
                if(local.Contains(a) && labeled.Add(runway.airport))Label(new Rect(Mathf.Clamp(a.x+4,r.x,r.xMax-55),Mathf.Clamp(a.y-16,r.y,r.yMax-18),55,18),runway.airport,new GUIStyle(small){fontSize=10},cream);
            }
            Vector2 c=local.center;float h=game.Heading;
            Vector2 tip=Radial(c,h,10),l=Radial(c,h-145,8),rr=Radial(c,h+145,8);
            Line(tip,l,cyan,2);Line(tip,rr,cyan,2);Line(l,rr,cyan,2);
            Label(new Rect(r.x,r.yMax+5,r.width,22),"NORTH UP                 11.9 NM ACROSS",new GUIStyle(small){fontSize=10},muted);
        }
        void Panel()
        {
            float y=height-89;
            // Independent translucent cards overlay the full-window camera.
            Box(new Rect(20,height-197,770,177),shade);
            Box(new Rect(20,height-197,770,2),new Color(.55f,.72f,.72f,.3f));
            Dial(new Vector2(93,y),"AIRSPEED",game.Airspeed,0,160,"0","KNOTS");
            AttitudeIndicator(new Vector2(220,y));
            Dial(new Vector2(347,y),"ALTITUDE",game.Altitude,0,5000,"0","FEET MSL");
            HeadingIndicator(new Vector2(474,y));
            Dial(new Vector2(601,y),"VERTICAL SPEED",game.VerticalSpeed,-2000,2000,"+0;-0;0","FT / MIN");
            Dial(new Vector2(728,y),"SLIP",game.Slip,-15,15,"+0;-0;0","DEGREES");
            float x=width-626;
            Box(new Rect(x,height-197,294,177),shade);
            Label(new Rect(x+17,height-183,260,26),$"{game.Rpm:0} RPM   /   {game.Throttle*100:0}% POWER",text);
            Box(new Rect(x+17,height-150,260,3),new Color(.2f,.25f,.26f,.7f));
            Box(new Rect(x+17,height-150,260*game.Throttle,3),cyan);
            Label(new Rect(x+17,height-139,266,22),$"FLAPS {game.Flaps*30:0}°    TRIM {-game.Trim*100:+0;-0;0}%    AGL {Mathf.Max(0,game.Agl):0}",small);
            Vector3 delta=game.ApproachStart-game.Position;
            Label(new Rect(x+17,height-110,270,24),$"{game.ApproachName}     {new Vector2(delta.x,delta.z).magnitude/1852:0.0} NM",text,cyan);
            if(GUI.Button(new Rect(x+16,height-76,134,34),"Approach ›",button))game.NextApproach();
            if(GUI.Button(new Rect(x+156,height-76,122,34),game.Hints?"Hints on · G":"Hints off · G",button))game.Hints=!game.Hints;
            GPS(new Rect(width-294,height-232,262,180));
        }
        void Markers()
        {
            foreach(string airport in new[]{"KLGA","KJFK","KTEB","KEWR"})
            {
                Runway runway=Array.Find(game.World.Runways,r=>r.airport==airport);if(runway==null)continue;
                Vector3 location=(runway.A+runway.B)/2+Vector3.up*30;
                Vector3 p=game.View.WorldToViewportPoint(location);
                if(p.z<=0 || p.x<.05f || p.x>.95f || p.y<.29f || p.y>.90f)continue;
                float x=p.x*width,y=(1-p.y)*height;
                Box(new Rect(x+5,y-15,135,24),shade);
                Box(new Rect(x-4,y-4,8,8),cyan);
                Label(new Rect(x+10,y-13,180,24),$"{airport}  {Vector3.Distance(game.Position,location)/1852:0.0} NM",small,cream);
            }
        }
        string Hint()
        {
            if(game.Ready)return "Ready on runway 04. Hold W to advance the throttle and release the brakes.";
            if(game.OnGround && game.LandingReport!="")return "Touchdown. Reduce throttle with S, hold B to brake, and use A / D to stay on the centerline.";
            if(game.OnGround)return game.Airspeed<50?"Accelerate toward 55 kt before raising the nose. A / D adjusts runway steering.":"Gently tap Up to rotate. Aim for 70–80 kt in the climb.";
            if(game.Airspeed<45)return "LOW AIRSPEED — lower the nose with Down and add power.";
            if(Mathf.Abs(game.Roll)>45)return "Steep bank. Use the opposite arrow to roll toward level.";
            if(game.Agl<100 && game.VerticalSpeed< -600)return "High descent rate near the ground. Add power and reduce the descent.";
            Vector3 offset=game.Position-game.ApproachStart;
            float along=-Vector3.Dot(offset,game.ApproachDirection);
            float cross=Vector3.Dot(offset,Vector3.Cross(Vector3.up,game.ApproachDirection));
            if(along>0 && along<9000 && Mathf.Abs(cross)<2500)
            {
                float desired=along*Mathf.Tan(3*Mathf.Deg2Rad)+15;
                float error=(game.Position.y-game.ApproachStart.y-desired)/.3048f;
                return $"{game.ApproachName}: {(Mathf.Abs(cross)<30?"aligned":$"{Mathf.Abs(cross):0} m {(cross>0?"right":"left")} of centerline")} · {(Mathf.Abs(error)<60?"on 3° path":$"{Mathf.Abs(error):0} ft {(error>0?"high":"low")}")} · aim for 60–70 kt.";
            }
            return "Explore NYC. [ / ] adjusts pitch trim. T toggles keyboard assist. Tab hides instruments.";
        }
        void OnGUI()
        {
            if(game==null)return;if(text==null)Init();
            GUI.matrix=Matrix4x4.identity;
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);
            width=Screen.width/scale;height=Screen.height/scale;
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            Box(new Rect(20,18,310,67),shade);
            Label(new Rect(36,24,280,34),"HUDSON FLIGHT",new GUIStyle(big){fontSize=23});
            Label(new Rect(38,57,280,23),"NEW YORK  /  CLEAR DAY  /  FREE FLIGHT",new GUIStyle(small){fontSize=10},muted);
            Box(new Rect(width-686,18,666,52),shade);
            if(GUI.Button(new Rect(width-675,27,142,34),game.Assistance?"Assist on · T":"Assist off · T",button))game.ToggleAssistance();
            if(GUI.Button(new Rect(width-527,27,125,34),game.Chase?"Cockpit · C":"Chase · C",button))game.Chase=!game.Chase;
            if(GUI.Button(new Rect(width-396,27,130,34),"Instruments · Tab",button))game.Instruments=!game.Instruments;
            if(GUI.Button(new Rect(width-260,27,113,34),game.Paused?"Resume":"Pause · Esc",button))game.Paused=!game.Paused;
            if(GUI.Button(new Rect(width-141,27,109,34),"Help · H",button))game.Help=!game.Help;
            if(game.World!=null && game.Destination!=null && game.View!=null)
            {
                if(game.Instruments){Markers();Panel();}
                if(game.Hints && !game.Crashed && !game.Help)
                {
                    float hintY=height-(game.Instruments?314:80);
                    Box(new Rect((width-1080)/2,hintY,1080,44),shade);
                    Label(new Rect((width-1040)/2,hintY+10,1040,27),Hint(),new GUIStyle(text){fontSize=15},cream);
                }
                if(!string.IsNullOrEmpty(game.LandingReport))
                {Box(new Rect((width-780)/2,96,780,48),shade);Label(new Rect((width-750)/2,107,750,28),game.LandingReport,new GUIStyle(center){fontSize=17},cyan);}
            }
            // Keep dialogs centered independently of the window's aspect ratio.
            GUI.matrix=Matrix4x4.TRS(new Vector3((width-1440)*scale/2,(height-900)*scale/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            if(game.Help)HelpPanel();
            else if(game.Crashed || !string.IsNullOrEmpty(game.Failure))
            {
                Modal("FLIGHT ENDED",game.Failure);
                if(GUI.Button(new Rect(545,427,350,45),"Restart on runway · R",button))game.ResetFlight();
            }
            else if(game.Paused)
            {
                Modal("PAUSED","Your aircraft is held in place.");
                if(GUI.Button(new Rect(545,427,170,45),"Resume · Esc",button))game.Paused=false;
                if(GUI.Button(new Rect(725,427,170,45),"Restart · R",button))game.ResetFlight();
            }
        }
        void Modal(string title,string detail)
        {
            Box(new Rect(-(width-1440)/2,-(height-900)/2,width,height),new Color(0,0,0,.55f));Box(new Rect(430,285,580,225),ink);
            Label(new Rect(465,315,510,42),title,big);Label(new Rect(465,368,510,55),detail,new GUIStyle(text){wordWrap=true});
        }
        void HelpPanel()
        {
            Box(new Rect(-(width-1440)/2,-(height-900)/2,width,height),new Color(0,0,0,.6f));Box(new Rect(325,100,790,740),ink);
            Label(new Rect(362,128,650,44),"YOUR FLIGHT CONTROLS",big);
            Label(new Rect(364,177,650,25),"Simulation pauses while this panel is open.",small,muted);
            string[,] rows={{"↑ / ↓","Raise / lower the nose"},{"← / →","Bank left / right"},{"A / D","Left / right rudder and ground steering"},{"W / S","Increase / decrease throttle (setting holds)"},{"F / Shift + F","Lower / raise flaps by one detent"},{"B (hold)","Wheel brakes"},{"[ / ]","Nose-up / nose-down pitch trim"},{"T","Toggle wings-level and rudder assistance"},{"Tab","Show / hide instrument overlays"},{"C","Switch cockpit / chase camera"},{"Right mouse drag","Look around; middle click recenters"},{"G","Toggle speed and approach hints"},{"Esc / H","Pause / show this help"},{"R","Restart when paused or after a crash"}};
            for(int i=0;i<rows.GetLength(0);i++)
            {Label(new Rect(365,213+i*31,230,28),rows[i,0],text,cyan);Label(new Rect(600,213+i*31,480,28),rows[i,1],text);}
            Label(new Rect(365,657,690,70),"Assist: release bank keys to level wings; A / D overrides rudder assistance.\nTakeoff: full power, rotate gently near 55 kt.\nLanding: line up, slow down, lower flaps, and flare gently before touchdown.",new GUIStyle(small){wordWrap=true},muted);
            Label(new Rect(365,739,690,30),"Scenery: USGS / USDA · NYC DCP · © OSM contributors · OurAirports   |   Aircraft: c172p-team (GPL-2.0)",new GUIStyle(small){fontSize=10},muted);
            if(GUI.Button(new Rect(865,779,205,36),"Back to flight · H",button))game.Help=false;
        }
    }
}
