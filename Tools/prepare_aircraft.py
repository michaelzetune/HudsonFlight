#!/usr/bin/env python3
"""Convert the pinned GPL C172P AC3D mesh to offline Unity mesh data.

Original model coordinates are aft/up/left; Unity uses right/up/forward.
Retain source assets and license alongside converted assets for redistribution.
"""
import json, pathlib, re, shlex, shutil
import numpy as np
from PIL import Image
ROOT=pathlib.Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Vendor/visual-upgrade/c172p'
OUT=ROOT/'Assets/StreamingAssets/Visuals/aircraft'
OUT.mkdir(parents=True,exist_ok=True)
lines=(SOURCE/'Models/c172-common.ac').read_text().splitlines()
materials=[];objects=[];cursor=1
while lines[cursor].startswith('MATERIAL'):
    line=lines[cursor];parts=shlex.split(line)
    materials.append({'name':parts[1],'color':list(map(float,parts[3:6])), 'alpha':1-float(parts[-1])})
    cursor+=1

def parse(parent=np.eye(4)):
    global cursor
    assert lines[cursor].startswith('OBJECT ')
    cursor+=1;name='';texture='';verts=[];surfaces=[];local=np.eye(4);texrep=(1,1);texoff=(0,0)
    while cursor<len(lines):
        parts=shlex.split(lines[cursor]);cursor+=1
        if not parts:continue
        tag=parts[0]
        if tag=='name':name=parts[1]
        elif tag=='texture':texture=parts[1]
        elif tag=='texrep':texrep=tuple(map(float,parts[1:]))
        elif tag=='texoff':texoff=tuple(map(float,parts[1:]))
        elif tag=='loc':local[:3,3]=list(map(float,parts[1:]))
        elif tag=='rot':local[:3,:3]=np.array(list(map(float,parts[1:]))).reshape(3,3)
        elif tag=='data':cursor+=1
        elif tag=='numvert':
            n=int(parts[1]);verts=[list(map(float,l.split())) for l in lines[cursor:cursor+n]];cursor+=n
        elif tag=='numsurf':
            for _ in range(int(parts[1])):
                assert lines[cursor].startswith('SURF');flags=int(lines[cursor].split()[1],16);cursor+=1
                mat=int(lines[cursor].split()[1]);cursor+=1
                count=int(lines[cursor].split()[1]);cursor+=1
                refs=[l.split() for l in lines[cursor:cursor+count]];cursor+=count
                surfaces.append((mat,flags,[(int(a),float(u)*texrep[0]+texoff[0],float(v)*texrep[1]+texoff[1]) for a,u,v in refs]))
        elif tag=='kids':
            world=parent@local
            if verts:objects.append((name,texture,np.array([world@np.array([*v,1]) for v in verts])[:,:3],surfaces))
            for _ in range(int(parts[1])):parse(world)
            return
parse()
meshes=[];textures=set()
skip=('hotspot','tiedown','blocker','cowlplug','courtesyon','beaconoffx')
for name,texture,vertices,surfaces in objects:
    if any(s in name.lower() for s in skip):continue
    groups={}
    for mat,flags,refs in surfaces:
        if flags&15 or len(refs)<3:continue
        g=groups.setdefault(mat,{'vertices':[],'uv':[],'triangles':[],'indices':{}})
        ids=[]
        for ref in refs:
            if ref not in g['indices']:
                g['indices'][ref]=len(g['vertices'])//3
                x,y,z=vertices[ref[0]];g['vertices']+=list(map(float,(-z,y,-x)))
                g['uv']+=list(ref[1:])
            ids.append(g['indices'][ref])
        for i in range(1,len(ids)-1):g['triangles'] += [ids[0],ids[i+1],ids[i]]
    for mat,g in groups.items():
        del g['indices'];m=materials[mat]
        if m['alpha']<.01:continue
        glass='glas' in name.lower() or 'window' in name.lower() and 'frame' not in name.lower()
        g.update(name=name,texture=texture,color=[1,1,1] if texture else m['color'],alpha=.16 if glass else m['alpha'],glass=glass)
        meshes.append(g)
        if texture:textures.add(texture)
for texture in textures:
    Image.open(SOURCE/'Models'/texture).save(OUT/texture)
(OUT/'aircraft.json').write_text(json.dumps({'meshes':meshes},separators=(',',':')))
# The original editable model, original textures, and notices accompany the derivative.
source_out=OUT/'Source';source_out.mkdir(exist_ok=True)
shutil.copyfile(__file__,source_out/'prepare_aircraft.py')
for path in ['LICENSE','README.md','Thanks','Models/c172-common.ac','Models/c172p.xml',*['Models/'+t for t in textures]]:
    dest=source_out/path;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(SOURCE/path,dest)
print('Aircraft:',len(meshes),'parts,',sum(len(m['triangles'])//3 for m in meshes),'triangles;',len(textures),'textures')
