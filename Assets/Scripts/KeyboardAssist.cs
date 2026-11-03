using UnityEngine;

namespace HudsonFlight
{
    // Moves control surfaces only; JSBSim still determines all forces and motion.
    public sealed class KeyboardAssist
    {
        float groundHeading, rollTrim;
        public void Reset(float heading) {groundHeading=heading;rollTrim=0;}
        public Vector2 Controls(float rollInput,float rudderInput,float bank,float rollRate,
            float heading,float yawRate,float slip,float speed,bool onGround,float dt)
        {
            float targetBank=rollInput/.55f*30;
            float error=targetBank-bank;
            if(!onGround && speed>45 && Mathf.Abs(bank)<45)
                rollTrim=Mathf.Clamp(rollTrim+error*dt*.004f,-.16f,.16f);
            float authority=Mathf.InverseLerp(25,50,speed);
            float aileron=Mathf.Lerp(rollInput,Mathf.Clamp(error*.035f-rollRate*.48f+rollTrim,-.65f,.65f),authority);
            float rudder;
            if(onGround)
            {
                if(Mathf.Abs(rudderInput)>.02f || speed<3)groundHeading=heading;
                rudder=rudderInput+Mathf.Clamp(Mathf.DeltaAngle(heading,groundHeading)*.035f-yawRate*.55f,-.3f,.3f);
            }
            else
            {
                groundHeading=heading;
                float turnRate=9.81f*Mathf.Tan(Mathf.Clamp(bank,-45,45)*Mathf.Deg2Rad)/Mathf.Max(25,speed*.514444f);
                rudder=rudderInput+Mathf.Clamp((turnRate-yawRate)*.9f+slip*.025f,-.25f,.25f)*authority;
            }
            return new Vector2(aileron,Mathf.Clamp(rudder,-.7f,.7f));
        }
    }
}
