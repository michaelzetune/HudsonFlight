"""Small AC3D reader for offline scenery conversion; no renderer dependencies."""
import shlex
import numpy as np

def load(path):
    lines=[l for l in path.read_text(errors='replace').splitlines() if l.strip()];cursor=1;materials=[];objects=[]
    while lines[cursor].startswith('MATERIAL'):
        p=shlex.split(lines[cursor]);cursor+=1
        materials.append(dict(name=p[1],color=list(map(float,p[3:6])),alpha=1-float(p[-1])))
    def parse(parent):
        nonlocal cursor
        assert lines[cursor].startswith('OBJECT '),(path,cursor,lines[cursor]);cursor+=1
        name='';texture='';verts=[];surfaces=[];matrix=np.eye(4);repeat=(1,1);offset=(0,0)
        while cursor<len(lines):
            p=shlex.split(lines[cursor]);cursor+=1
            if not p:continue
            k=p[0]
            if k=='name':name=p[1]
            elif k=='texture':texture=p[1]
            elif k=='texrep':repeat=tuple(map(float,p[1:]))
            elif k=='texoff':offset=tuple(map(float,p[1:]))
            elif k=='loc':matrix[:3,3]=list(map(float,p[1:]))
            elif k=='rot':matrix[:3,:3]=np.array(list(map(float,p[1:]))).reshape(3,3)
            elif k=='data':cursor+=int(int(p[1])>0)
            elif k=='numvert':
                n=int(p[1]);verts=np.array([list(map(float,l.split())) for l in lines[cursor:cursor+n]]);cursor+=n
            elif k=='numsurf':
                for _ in range(int(p[1])):
                    flags=int(lines[cursor].split()[1],16);cursor+=1
                    material=int(lines[cursor].split()[1]);cursor+=1
                    n=int(lines[cursor].split()[1]);cursor+=1
                    refs=[l.split() for l in lines[cursor:cursor+n]];cursor+=n
                    surfaces.append((material,flags,[(int(i),float(u)*repeat[0]+offset[0],float(v)*repeat[1]+offset[1]) for i,u,v in refs]))
            elif k=='kids':
                world=parent@matrix
                if len(verts):objects.append((name,texture,np.c_[verts,np.ones(len(verts))].dot(world.T)[:,:3],surfaces))
                for _ in range(int(p[1])):parse(world)
                return
    parse(np.eye(4))
    return materials,objects
