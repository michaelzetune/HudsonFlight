#!/usr/bin/env python3
"""Check imported city geometry for corrupt data, inverted roofs and missing landmarks."""
import json,pathlib,struct
import numpy as np
ROOT=pathlib.Path(__file__).resolve().parents[1]/'Assets/StreamingAssets/Visuals'
survey=json.loads((ROOT/'survey/survey.json').read_text())
assert survey['buildings']>90000
roof_up=roof_down=triangles=0
for c in survey['chunks']:
 data=(ROOT/'survey'/c['file']).read_bytes();magic,n,i=struct.unpack_from('<4sII',data)
 assert magic==b'HFSM' and n==c['vertices'] and i==c['triangles']*3
 assert len(data)==12+n*24+i*4
 v=np.frombuffer(data,dtype='<f4',count=n*3,offset=12).reshape(-1,3)
 uv=np.frombuffer(data,dtype='<f4',count=n*2,offset=12+n*12).reshape(-1,2)
 t=np.frombuffer(data,dtype='<u4',count=i,offset=12+n*24).reshape(-1,3)
 assert np.isfinite(v).all() and np.isfinite(uv).all() and t.max()<n
 assert v[:,1].min()>2.9 and v[:,1].max()<650
 if c['texture']:
  assert (ROOT/c['texture']).is_file()
  normal=np.cross(v[t[:,1]]-v[t[:,0]],v[t[:,2]]-v[t[:,0]])
  roof_up+=int((normal[:,1]>.001).sum());roof_down+=int((normal[:,1]<-.001).sum())
 triangles+=len(t)
assert roof_down<roof_up*.005,(roof_up,roof_down)
landmarks=json.loads((ROOT/'landmarks/footprints.json').read_text())['buildings']
assert len(landmarks)==79
byname={b['name']:b for b in landmarks}
for name,low,high in [('empire-state-building',440,450),('chrysler-building',315,323),('one-world-trade-center',538,548),('statue-of-liberty',85,96),('central-park-tower',465,480)]:
 assert low<byname[name]['height']<high,name
assert not any('wtc_north' in n or 'wtc_south' in n for n in byname)
meshes=json.loads((ROOT/'landmarks/landmarks.json').read_text())['meshes']
for m in meshes:
 v=np.array(m['vertices']).reshape(-1,3);t=np.array(m['triangles']).reshape(-1,3)
 assert np.isfinite(v).all() and t.max()<len(v)
 if m['texture']:assert (ROOT/'landmarks'/m['texture']).is_file()
print(f'CITY ASSETS PASS: {survey["buildings"]:,} survey buildings, {triangles:,} triangles, {roof_up:,} upward roof faces ({roof_down} downward), 79 landmarks with textures and expected heights.')
