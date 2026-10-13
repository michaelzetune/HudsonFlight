#!/usr/bin/env python3
"""Exercise the same ARM64 native bridge used by Unity, without needing a Unity license."""
import ctypes as c, pathlib, math
ROOT=pathlib.Path(__file__).resolve().parents[1]
lib=c.CDLL(str(ROOT/'Assets/Plugins/macOS/libHudsonFlight.dylib'))
lib.hf_create.argtypes=[c.c_char_p]+[c.c_double]*6
lib.hf_create.restype=c.c_void_p
lib.hf_destroy.argtypes=[c.c_void_p]
lib.hf_set.argtypes=[c.c_void_p,c.c_char_p,c.c_double]
lib.hf_get.argtypes=[c.c_void_p,c.c_char_p,c.POINTER(c.c_double)]
lib.hf_step.argtypes=[c.c_void_p,c.c_int]
lib.hf_error.restype=c.c_char_p
class Flight:
 def __init__(self, agl=4.7, speed=0, heading=31):
  self.ptr=lib.hf_create(str(ROOT/'Assets/StreamingAssets/JSBSim').encode(),40.77,-73.88,heading,21,agl,speed)
  assert self.ptr, lib.hf_error()
 def get(self, name):
  value=c.c_double()
  assert lib.hf_get(self.ptr,name.encode(),c.byref(value)),lib.hf_error()
  return value.value
 def set(self,name,value):
  assert lib.hf_set(self.ptr,name.encode(),value),lib.hf_error()
 def step(self,seconds):
  for _ in range(round(seconds*120)):
   assert lib.hf_step(self.ptr,1),lib.hf_error()
 def close(self):
  lib.hf_destroy(self.ptr);self.ptr=None

if __name__=='__main__':
 f=Flight()
 f.set('fcs/center-brake-cmd-norm',1)
 f.set('fcs/left-brake-cmd-norm',1)
 f.set('fcs/right-brake-cmd-norm',1)
 f.step(5)
 print('Parked:', {p:round(f.get(p),3) for p in ['position/h-agl-ft','velocities/vc-kts','gear/wow','propulsion/engine/engine-rpm']})
 assert f.get('gear/wow')==1
 assert f.get('velocities/vc-kts')<2
 assert f.get('propulsion/engine/engine-rpm')>400
 for p in ['center','left','right']: f.set('fcs/'+p+'-brake-cmd-norm',0)
 f.set('fcs/throttle-cmd-norm',1)
 for i in range(45*120):
  speed=f.get('velocities/vc-kts')
  f.set('fcs/elevator-cmd-norm',-.22 if speed>55 else 0)
  f.step(1/120)
  if f.get('position/h-agl-ft')>30: break
 print('Takeoff:', {p:round(f.get(p),3) for p in ['position/h-agl-ft','velocities/vc-kts','attitude/theta-deg','gear/wow']})
 assert f.get('position/h-agl-ft')>20
 assert f.get('gear/wow')==0
 f.close()
 # A fresh instance must reset independently, rather than retaining velocity or controls.
 for _ in range(3):
  f=Flight();assert abs(f.get('velocities/vc-kts'))<.1;f.close()
 # Pitch convention: a negative elevator command must raise the nose.
 f=Flight(1000,90);f.set('fcs/elevator-cmd-norm',-.3);f.step(.5)
 assert f.get('velocities/q-rad_sec')>0
 f.close()
 f=Flight(1000,90);f.set('fcs/aileron-cmd-norm',.4);f.step(.5)
 assert f.get('velocities/p-rad_sec')>0;f.close()
 f=Flight(1000,90);f.set('fcs/rudder-cmd-norm',-.4);f.step(.5)
 assert f.get('velocities/r-rad_sec')>0;f.close()
 f=Flight(4.7,15);f.set('fcs/rudder-cmd-norm',-.4);f.step(1)
 assert f.get('attitude/psi-deg')>31;f.close()
 print('PASS: ground contact, engine running, takeoff, reset lifecycle, Up/nose-up, right bank, right rudder and steering')
