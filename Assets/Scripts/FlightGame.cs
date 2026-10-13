using System;
using System.IO;
using UnityEngine;

namespace HudsonFlight
{
    public sealed class FlightGame : MonoBehaviour
    {
        public WorldBuilder World {get;private set;}
        public Camera View {get;private set;}
        public Runway Departure, Destination;
        public bool ReverseApproach, Help, Paused, Hints=true, Chase, Crashed, Ready=true;
        public string Failure="", LandingReport="";
        public float Throttle=.12f, Elevator, Aileron, Rudder, Trim, Flaps;
        public float Airspeed, Altitude, Agl, VerticalSpeed, Heading, Pitch, Roll, Rpm, Slip;
        public float PitchRate, RollRate, YawRate;
        public bool OnGround;
        public Vector3 Position {get;private set;}
        public Quaternion Attitude {get;private set;}
        public Vector3 ApproachStart => ReverseApproach ? Destination.B : Destination.A;
        public Vector3 ApproachDirection => (ReverseApproach ? Destination.A-Destination.B : Destination.B-Destination.A).normalized;
        public string ApproachName => Destination.airport+" "+Destination.name.Split('/')[ReverseApproach?1:0];
        public bool Running => !Paused && !Help && !Crashed && string.IsNullOrEmpty(Failure);
        FlightDynamics dynamics;
        Transform aircraft,propeller;
        float lookYaw,lookPitch;
        bool hasFlown;
        AudioSource engine;
        readonly float[] flapStops={0,1f/3,2f/3,1};
        int flapIndex;
        int approachIndex;
        public bool Automated {get;private set;}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(FindFirstObjectByType<FlightGame>()==null) new GameObject("Hudson Flight").AddComponent<FlightGame>();
        }
        void Start()
        {
            string[] args=Environment.GetCommandLineArgs();
            Automated=Array.IndexOf(args,"--hudson-smoke")>=0;
            if(Automated){Application.runInBackground=true;gameObject.AddComponent<RuntimeSmoke>();}
            gameObject.AddComponent<FlightHud>();
            try
            {
                Application.targetFrameRate=60;Time.fixedDeltaTime=1f/60f;
                World=new WorldBuilder();World.Build();Physics.SyncTransforms();
                Departure=Array.Find(World.Runways,r=>r.airport=="KLGA" && r.name=="04/22");
                Destination=Departure;approachIndex=Array.IndexOf(World.Runways,Departure)*2;
                aircraft=World.BuildAircraft();propeller=aircraft.Find("Propeller");
                View=new GameObject("Pilot camera").AddComponent<Camera>();View.tag="MainCamera";
                View.fieldOfView=70;View.nearClipPlane=.1f;View.farClipPlane=75000;
                View.clearFlags=CameraClearFlags.SolidColor;View.backgroundColor=new Color(.61f,.77f,.9f);
                View.gameObject.AddComponent<AudioListener>();
                AddAudio();ResetFlight();
            }
            catch(Exception ex) {Failure=ex.Message;Debug.LogException(ex);}
        }
        void AddAudio()
        {
            engine=gameObject.AddComponent<AudioSource>();engine.loop=true;engine.spatialBlend=0;
            int rate=22050;var samples=new float[rate];
            for(int i=0;i<rate;i++)
            {
                float t=i/(float)rate;
                samples[i]=.22f*Mathf.Sin(2*Mathf.PI*45*t)+.10f*Mathf.Sin(2*Mathf.PI*90*t)+.05f*Mathf.Sin(2*Mathf.PI*135*t);
            }
            var clip=AudioClip.Create("Piston engine",rate,1,rate,false);clip.SetData(samples,0);
            engine.clip=clip;engine.Play();
        }
        public void ResetFlight()
        {
            try
            {
                dynamics?.Dispose();dynamics=null;
                Vector3 spawn=Departure.A+(Departure.B-Departure.A).normalized*90;
                dynamics=new FlightDynamics(Path.Combine(Application.streamingAssetsPath,"JSBSim"),
                    Geography.Latitude(spawn),Geography.Longitude(spawn),Departure.Heading,(Departure.elevation+.15)/.3048);
                Throttle=.12f;Elevator=Aileron=Rudder=Trim=Flaps=0;flapIndex=0;
                ApplyControls(1);
                for(int i=0;i<3;i++) dynamics.Step(120);
                ReadState();hasFlown=false;Crashed=false;Failure="";LandingReport="";Ready=true;
                lookYaw=lookPitch=0;Paused=false;
            }
            catch(Exception ex) {Failure=ex.Message;Debug.LogException(ex);}
        }
        void Update()
        {
            if(View==null)return;
            if(Input.GetKeyDown(KeyCode.H))Help=!Help;
            if(Input.GetKeyDown(KeyCode.Escape)) { if(Help)Help=false;else Paused=!Paused; }
            if(Input.GetKeyDown(KeyCode.C)) {Chase=!Chase;lookYaw=lookPitch=0;}
            if(Input.GetKeyDown(KeyCode.G))Hints=!Hints;
            if(Input.GetKeyDown(KeyCode.R) && (Crashed || Paused))ResetFlight();
            if(Running && !Automated)
            {
                float dt=Mathf.Min(Time.deltaTime,.05f);
                if(Input.GetKey(KeyCode.W)) {Throttle=Mathf.Clamp01(Throttle+dt*.28f);Ready=false;}
                if(Input.GetKey(KeyCode.S))Throttle=Mathf.Clamp01(Throttle-dt*.28f);
                Elevator=Mathf.MoveTowards(Elevator,(Input.GetKey(KeyCode.DownArrow)? .45f:0)-(Input.GetKey(KeyCode.UpArrow)? .45f:0),dt*1.1f);
                Aileron=Mathf.MoveTowards(Aileron,(Input.GetKey(KeyCode.RightArrow)? .55f:0)-(Input.GetKey(KeyCode.LeftArrow)? .55f:0),dt*1.4f);
                Rudder=Mathf.MoveTowards(Rudder,(Input.GetKey(KeyCode.D)? .6f:0)-(Input.GetKey(KeyCode.A)? .6f:0),dt*1.2f);
                if(Input.GetKey(KeyCode.LeftBracket))Trim=Mathf.Clamp(Trim-dt*.1f,-.5f,.5f);
                if(Input.GetKey(KeyCode.RightBracket))Trim=Mathf.Clamp(Trim+dt*.1f,-.5f,.5f);
                if(Input.GetKeyDown(KeyCode.F))
                {
                    flapIndex=Mathf.Clamp(flapIndex+(Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)?-1:1),0,3);
                    Flaps=flapStops[flapIndex];
                }
            }
            if(Input.GetMouseButton(1))
            {
                lookYaw=Mathf.Clamp(lookYaw+Input.GetAxisRaw("Mouse X")*2,-120,120);
                lookPitch=Mathf.Clamp(lookPitch-Input.GetAxisRaw("Mouse Y")*2,-55,65);
            }
            if(Input.GetMouseButtonDown(2))lookYaw=lookPitch=0;
            if(engine!=null) {engine.pitch=Mathf.Clamp(Rpm/1000,.6f,2.8f);engine.volume=Running?.13f+Throttle*.10f:0;}
        }
        void ApplyControls(float brakes)
        {
            dynamics.Set("fcs/throttle-cmd-norm",Throttle);
            dynamics.Set("fcs/elevator-cmd-norm",Elevator);
            dynamics.Set("fcs/aileron-cmd-norm",Aileron);
            // JSBSim's C172 uses negative rudder for a right pedal input.
            dynamics.Set("fcs/rudder-cmd-norm",-Rudder);
            dynamics.Set("fcs/pitch-trim-cmd-norm",Trim);
            dynamics.Set("fcs/flap-cmd-norm",Flaps);
            dynamics.Set("fcs/left-brake-cmd-norm",brakes);
            dynamics.Set("fcs/right-brake-cmd-norm",brakes);
            dynamics.Set("fcs/center-brake-cmd-norm",brakes);
        }
        void FixedUpdate()
        {
            if(!Running || dynamics==null)return;
            try
            {
                Vector3 previous=Position;bool wasGround=OnGround;float descent=-VerticalSpeed;
                if(Physics.Raycast(Position+Vector3.up*3000,Vector3.down,out var terrain,6000,1<<WorldBuilder.GroundLayer))
                    dynamics.Set("position/terrain-elevation-asl-ft",terrain.point.y/.3048);
                ApplyControls(Ready || Input.GetKey(KeyCode.B)?1:0);
                dynamics.Step(2);ReadState();
                // Ground geometry can settle on the first physics frame. Require actual
                // flight speed and loss of wheel contact before arming touchdown feedback.
                if(!OnGround && Airspeed>40 && Agl>12)hasFlown=true;
                Vector3 motion=Position-previous;
                if(motion.sqrMagnitude>.0001f && Physics.SphereCast(previous,.8f,motion.normalized,out _,motion.magnitude,1<<WorldBuilder.ObstacleLayer))
                    Crash("Collision with a building or bridge.");
                if(OnGround && hasFlown && !wasGround)
                {
                    Runway runway=Array.Find(World.Runways,r=>r.Contains(Position));
                    float alignment=runway==null?180:Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(Heading,runway.Heading)),Mathf.Abs(Mathf.DeltaAngle(Heading,runway.Heading+180)));
                    if(runway==null)Crash(Position.y<2?"Water impact.":"Touchdown outside a runway.");
                    else if(descent>650)Crash($"Hard landing: {descent:0} ft/min descent at touchdown.");
                    else if(Mathf.Abs(Roll)>12 || Pitch< -4 || Pitch>18)Crash("Wing or nose strike: aircraft attitude was unsafe at touchdown.");
                    else if(alignment>25)Crash($"Sideways touchdown: {alignment:0}° off runway heading.");
                    else
                    {
                        string grade=descent<180 && alignment<5?"Smooth landing":descent<360?"Good landing":"Firm landing";
                        LandingReport=$"{grade}  •  {descent:0} ft/min  •  {alignment:0}° alignment  •  {Airspeed:0} kt";
                        hasFlown=false;
                    }
                }
                if(OnGround && Airspeed>25 && Array.Find(World.Runways,r=>r.Contains(Position,12))==null)
                    Crash("Runway excursion: the aircraft left the runway at speed.");
                if(Agl< -2 || Mathf.Abs(Roll)>75 && Agl<5)Crash("Ground impact.");
            }
            catch(Exception ex){Failure=ex.Message;Debug.LogException(ex);}
        }
        void ReadState()
        {
            Airspeed=(float)dynamics.Get("velocities/vc-kts");Altitude=(float)dynamics.Get("position/h-sl-ft");
            Agl=(float)dynamics.Get("position/h-agl-ft");VerticalSpeed=-(float)dynamics.Get("velocities/v-down-fps")*60;
            Heading=(float)dynamics.Get("attitude/psi-deg");Pitch=(float)dynamics.Get("attitude/theta-deg");Roll=(float)dynamics.Get("attitude/phi-deg");
            Rpm=(float)dynamics.Get("propulsion/engine/engine-rpm");Slip=(float)dynamics.Get("aero/beta-deg");OnGround=dynamics.Get("gear/wow")>.5;
            PitchRate=(float)dynamics.Get("velocities/q-rad_sec");RollRate=(float)dynamics.Get("velocities/p-rad_sec");YawRate=(float)dynamics.Get("velocities/r-rad_sec");
            Position=Geography.Position(dynamics.Get("position/lat-geod-deg"),dynamics.Get("position/long-gc-deg"),Altitude*.3048);
            Attitude=Quaternion.AngleAxis(Heading,Vector3.up)*Quaternion.AngleAxis(-Pitch,Vector3.right)*Quaternion.AngleAxis(-Roll,Vector3.forward);
        }
        void LateUpdate()
        {
            if(View==null || aircraft==null)return;
            float aspect=Screen.width/(float)Mathf.Max(1,Screen.height),targetAspect=1.6f;
            View.rect=aspect>targetAspect
                ? new Rect((1-targetAspect/aspect)/2,0,targetAspect/aspect,1)
                : new Rect(0,(1-aspect/targetAspect)/2,1,aspect/targetAspect);
            aircraft.position=Position;aircraft.rotation=Attitude;aircraft.gameObject.SetActive(Chase);
            if(propeller!=null)propeller.Rotate(0,0,Rpm*6*Time.deltaTime,Space.Self);
            Quaternion look=Quaternion.Euler(lookPitch,lookYaw,0);
            if(Chase)
            {
                Quaternion orbit=Quaternion.Euler(0,Heading,0)*look;
                View.transform.position=Position+orbit*new Vector3(0,4,-17);
                View.transform.LookAt(Position+Vector3.up);
            }
            else {View.transform.position=Position+Attitude*new Vector3(0,.55f,.3f);View.transform.rotation=Attitude*look;}
        }
        public void NextApproach()
        {
            approachIndex=(approachIndex+1)%(World.Runways.Length*2);
            Destination=World.Runways[approachIndex/2];ReverseApproach=approachIndex%2==1;
        }
        public void Crash(string reason) {if(Crashed)return;Crashed=true;Failure=reason;}
        public void SetTestFlight(Vector3 location, float heading, float aglFeet, float knots, float lookDown=0)
        {
            if(!Automated)throw new InvalidOperationException("Test setup is unavailable during normal play");
            dynamics?.Dispose();
            dynamics=new FlightDynamics(Path.Combine(Application.streamingAssetsPath,"JSBSim"),
                Geography.Latitude(location),Geography.Longitude(location),heading,(Departure.elevation+.15)/.3048,aglFeet,knots);
            Elevator=Aileron=Rudder=Trim=Flaps=0;Ready=false;Crashed=false;Failure="";LandingReport="";
            lookPitch=lookDown;lookYaw=0;
            Paused=false;hasFlown=true;ReadState();
        }
        void OnApplicationFocus(bool focus){if(!focus && !Automated)Paused=true;}
        void OnDestroy(){dynamics?.Dispose();}
    }
}
