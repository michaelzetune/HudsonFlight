#!/usr/bin/env python3
"""Bake separately authored NYC landmarks with their own geometry and textures."""
import json,math,pathlib,shlex,shutil,xml.etree.ElementTree as ET
import numpy as np
import mapbox_earcut as earcut
from PIL import Image
from shapely.geometry import MultiPoint,mapping
from ac3d import load
ROOT=pathlib.Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Vendor/building-upgrade/flightgear/Objects/w080n40'
OUT=ROOT/'Assets/StreamingAssets/Visuals/landmarks';OUT.mkdir(parents=True,exist_ok=True)
SX=111319.49079327358*math.cos(math.radians(40.7772));SZ=111319.49079327358
meshes=[];buildings=[];sources=set()
for stg in sorted(SOURCE.glob('*/*.stg')):
 for line in stg.read_text().splitlines():
    p=shlex.split(line)
    if len(p)<6 or p[0]!='OBJECT_STATIC':continue
    file=p[1];lon,lat,alt,heading=map(float,p[2:6])
    if not(-74.06<lon< -73.91 and 40.67<lat<40.86):continue
    if any(s in file.lower() for s in ['wtc_north','wtc_south','klga','jfk','verrazano']):continue
    path=stg.parent/file
    if path.suffix=='.xml':
        xml=ET.parse(path).getroot();sources.add(path)
        ref=xml.findtext('path')
        if not ref:continue
        path=path.parent/ref
    if path.suffix!='.ac':continue
    materials,objects=load(path);sources.update([path,stg])
    # AC3D uses aft/up/left. Scenery STG orientation is counterclockwise.
    a=math.radians(-heading);c,s=math.cos(a),math.sin(a)
    pivot=np.array([(lon+73.8726)*SX,0,(lat-40.7772)*SZ])
    transformed=[]
    for name,tex,v,surfaces in objects:
        x,z=-v[:,2],-v[:,0]
        vv=np.stack([x*c+z*s,v[:,1],-x*s+z*c],axis=1)+pivot
        transformed.append((name,tex,vv,surfaces))
    all_v=np.concatenate([o[2] for o in transformed]);base=float(np.min(all_v[:,1]))
    is_bridge='bridge' in file.lower() or file.lower().startswith('gwb')
    shift=0 if is_bridge else 3-base
    footprint=MultiPoint(all_v[:,[0,2]]).convex_hull
    buildings.append(dict(name=path.stem,lat=lat,lon=lon,heading=heading,height=round(float(np.ptp(all_v[:,1])),2),bridge=is_bridge,footprint=mapping(footprint)))
    for name,tex,vv,surfaces in transformed:
        vv[:,1]+=shift
        groups={}
        for mat,flags,refs in surfaces:
            if flags&15 or len(refs)<3:continue
            g=groups.setdefault(mat,dict(vertices=[],uv=[],triangles=[]))
            start=len(g['vertices'])//3
            v=vv[[r[0] for r in refs]]
            # Use the original polygon's orientation and earcut for concave faces.
            normal=np.sum(np.cross(v,np.roll(v,-1,axis=0)),axis=0)
            drop=int(np.argmax(np.abs(normal)));coords=np.delete(v,drop,axis=1)
            ids=earcut.triangulate_float64(np.ascontiguousarray(coords),np.array([len(v)],dtype=np.uint32)).reshape(-1,3)
            for tri in ids:
                a,b,c=tri
                if np.dot(np.cross(v[b]-v[a],v[c]-v[a]),normal)>0:b,c=c,b
                g['triangles'] += [start+int(a),start+int(b),start+int(c)]
            face_indices=g['triangles'][-len(ids)*3:]
            g['vertices']+=np.round(v,3).reshape(-1).tolist();g['uv'] += [value for r in refs for value in r[1:]]
            if flags&0x20:
                # Back faces need separate vertices so opposite normals do not cancel.
                for i in range(0,len(face_indices),3):
                    a,b,c=face_indices[i:i+3];g['triangles'] += [a+len(v),c+len(v),b+len(v)]
                g['vertices']+=np.round(v,3).reshape(-1).tolist();g['uv'] += [value for r in refs for value in r[1:]]
        for mat,g in groups.items():
            if not g['triangles']:continue
            m=materials[mat];texture=''
            if tex:
                src=path.parent/tex;sources.add(src)
                texture=path.parent.name+'_'+pathlib.Path(tex).stem+'.png'
                dest=OUT/texture
                if not dest.exists():Image.open(src).save(dest)
            g.update(name=path.stem+'/'+name,texture=texture,color=[1,1,1] if tex else m['color'],alpha=1,glass=False)
            meshes.append(g)
    print(path.stem,round(float(np.ptp(all_v[:,1]))),'m',flush=True)
(OUT/'landmarks.json').write_text(json.dumps({'meshes':meshes},separators=(',',':')))
(OUT/'footprints.json').write_text(json.dumps({'buildings':buildings},separators=(',',':')))
source_out=OUT/'Source';source_out.mkdir(exist_ok=True)
for src in sources:
    dest=source_out/src.relative_to(SOURCE);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src,dest)
for script in ['ac3d.py','prepare_landmarks.py']:shutil.copyfile(ROOT/'Tools'/script,source_out/script)
shutil.copyfile(ROOT/'Vendor/visual-upgrade/c172p/LICENSE',OUT/'GPL-2.0.txt')
(OUT/'CREDITS.txt').write_text('NYC landmark models: FlightGear scenery contributors. GPL-2.0-or-later. Copyright remains with each original author.\nhttps://scenery.flightgear.org/objects?country=us\nSource: https://terrasync.b-cdn.net/Objects/w080n40/\nOriginal AC3D models, textures, placement files, and conversion scripts accompany this derivative in Source/. Historical Twin Towers and obsolete airport models are excluded.\n')
print('LANDMARKS:',len(buildings),'models,',sum(len(m['triangles'])//3 for m in meshes),'triangles')
