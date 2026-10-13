#!/usr/bin/env python3
"""Bake downloaded geography into small, offline Unity meshes (meters, north = +Z)."""
import json, pathlib, math, re, hashlib, datetime
import numpy as np
import mapbox_earcut as earcut
from shapely.geometry import shape, Polygon, LineString, box
from shapely.ops import transform

ROOT=pathlib.Path(__file__).resolve().parents[1]
CACHE=ROOT/'Vendor/scenery'
OUT=ROOT/'Assets/StreamingAssets/Scenery'
LAT,LON=40.7772,-73.8726
SCALE=111319.49079327358
XS=SCALE*math.cos(math.radians(LAT))
def project(lon,lat,z=None): return ((lon-LON)*XS,(lat-LAT)*SCALE)
meshes=[]
buckets={}
COLORS={'land':[.37,.43,.36], 'building':[.57,.60,.61], 'airport':[.43,.45,.44], 'bridge':[.49,.53,.55], 'park':[.27,.39,.28]}
def add(poly,base,height,kind,name=''):
    if poly.is_empty:return
    if poly.geom_type!='Polygon':
        for p in getattr(poly,'geoms',[]):add(p,base,height,kind,name)
        return
    poly=poly.simplify(1.2,preserve_topology=True)
    if not poly.is_valid:poly=poly.buffer(0)
    if poly.geom_type!='Polygon':return add(poly,base,height,kind,name)
    rings=[list(poly.exterior.coords)[:-1]]+[list(r.coords)[:-1] for r in poly.interiors]
    coords=np.array([p for ring in rings for p in ring],dtype=np.float64)
    if len(coords)<3:return
    inds=earcut.triangulate_float64(coords,np.cumsum([len(r) for r in rings],dtype=np.uint32)).tolist()
    key=(kind, int(poly.centroid.x//1500),int(poly.centroid.y//1500))
    bucket=buckets.setdefault(key,{'name':kind+' '+str(key[1:]),'kind':kind,'vertices':[],'triangles':[],'color':COLORS[kind]})
    verts=bucket['vertices'];tris=bucket['triangles'];offset=len(verts)//3
    for x,z in coords:verts.extend([round(x,2),round(base+height,2),round(z,2)])
    # Earcut winds CCW in XY. Our XY maps to XZ, so reverse to point normals up.
    for a,b,c in zip(inds[::3],inds[1::3],inds[2::3]):
        ax,az=coords[a];bx,bz=coords[b];cx,cz=coords[c]
        if (bx-ax)*(cz-az)-(bz-az)*(cx-ax)>0:b,c=c,b
        tris.extend([offset+a,offset+b,offset+c])
    if height>0:
        # Each wall has distinct vertices for crisp corners and flat normals.
        from shapely.geometry.polygon import orient
        poly=orient(poly,sign=1)
        for ring in [poly.exterior,*poly.interiors]:
            ps=list(ring.coords)
            for (x,z),(xx,zz) in zip(ps,ps[1:]):
                n=len(verts)//3
                verts.extend([x,base,z,xx,base,zz,xx,base+height,zz,x,base+height,z])
                tris.extend([n,n+2,n+1,n,n+3,n+2])

boroughs=json.loads((CACHE/'boroughs.geojson').read_text())
for feature in boroughs['features']:
    p=transform(project,shape(feature['geometry']))
    add(p,0,3,'land')

# Simplified west bank backdrop; NYC shorelines above use city boundary data.
nj=[(-74.21,40.57),(-74.06,40.66),(-74.035,40.70),(-74.035,40.716),(-74.022,40.735),(-74.010,40.751),(-74.002,40.771),(-73.977,40.805),(-73.960,40.85),(-73.924,40.9),(-74.3,40.98),(-74.3,40.55)]
add(Polygon([project(*p) for p in nj]),0,5,'land')

elements=json.loads((CACHE/'buildings.json').read_text())['elements']
count=0
for item in elements:
    tags=item.get('tags',{});geo=item.get('geometry',[])
    try:
        text=tags.get('height','12');h=float(re.search(r'[\d.]+',text).group())
        if 'ft' in text or "'" in text:h*=.3048
        p=Polygon([project(n['lon'],n['lat']) for n in geo])
        # Keep every tall building and a deterministic sampling of low-rise blocks.
        if h<24 and item['id']%4:continue
        if p.area<35:continue
        name=tags.get('name','')
        if name=='Empire State Building':
            from shapely.affinity import scale
            for bottom,top,factor in [(0,60,1),(60,320,.63),(320,370,.42),(370,390,.2)]:
                add(scale(p,xfact=factor,yfact=factor,origin='centroid'),3+bottom,top-bottom,'building')
            add(p.centroid.buffer(1.8,resolution=5),393,50,'building')
        elif name=='One World Trade Center':
            from shapely.affinity import scale
            for i in range(12):
                add(scale(p,xfact=1-i*.036,yfact=1-i*.036,origin='centroid'),3+i*34.75,34.75,'building')
            add(p.centroid.buffer(2,resolution=5),420,124,'building')
        else:add(p,3,min(h,600),'building')
        count+=1
    except (ValueError,AttributeError):continue

airport_path=CACHE/'airport.json'
if airport_path.exists():
    for item in json.loads(airport_path.read_text())['elements']:
        tags=item.get('tags',{});g=item.get('geometry',[])
        if len(g)<2:continue
        points=[project(n['lon'],n['lat']) for n in g]
        if tags.get('aeroway') in ('taxiway','apron'):
            p=Polygon(points) if len(points)>3 and points[0]==points[-1] else LineString(points).buffer(11)
            add(p,6.1,0,'airport')
        elif 'building' in tags and len(points)>3:
            add(Polygon(points),6.1,15,'building')

bridge_path=CACHE/'bridges.json'
if bridge_path.exists():
    for item in json.loads(bridge_path.read_text())['elements']:
        if item.get('tags',{}).get('highway') not in ('motorway','trunk','primary','secondary','motorway_link','trunk_link','primary_link','secondary_link'):continue
        g=item.get('geometry',[])
        if len(g)<2:continue
        line=LineString([project(n['lon'],n['lat']) for n in g])
        if line.length<70:continue
        add(line.buffer(9),29,4,'bridge')
        for d in range(0,int(line.length),110):
            pt=line.interpolate(d);add(pt.buffer(2.8,resolution=2),0,29,'bridge')

# Central Park, deliberately above the flat city ground.
park=[(-73.9819,40.7681),(-73.9581,40.8006),(-73.9495,40.7968),(-73.9730,40.7646)]
add(Polygon([project(*p) for p in park]),3.04,0,'park')
for b in buckets.values():
    b['vertices']=[round(x,2) for x in b['vertices']]
    # PhysX warns about triangle edges over 500 m. Bisect long edges so
    # shoreline/bridge collision queries remain well conditioned.
    vertices=b['vertices'];pending=[b['triangles'][i:i+3] for i in range(0,len(b['triangles']),3)]
    triangles=[];midpoints={}
    while pending:
        a,bx,c=pending.pop()
        def distance(i,j):return sum((vertices[i*3+k]-vertices[j*3+k])**2 for k in range(3))
        edges=[(distance(a,bx),a,bx,c),(distance(bx,c),bx,c,a),(distance(c,a),c,a,bx)]
        length,u,v,w=max(edges)
        if length<=450**2:
            triangles.extend((a,bx,c));continue
        edge=tuple(sorted((u,v)))
        if edge not in midpoints:
            midpoints[edge]=len(vertices)//3
            vertices.extend(round((vertices[u*3+k]+vertices[v*3+k])/2,2) for k in range(3))
        mid=midpoints[edge]
        pending.extend([(u,mid,w),(mid,v,w)])
    b['triangles']=triangles
    meshes.append(b)
(OUT/'world.json').write_text(json.dumps({'meshes':meshes},separators=(',',':')))
sources=[{'name':p.name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in CACHE.iterdir() if p.is_file()]
(OUT/'sources.json').write_text(json.dumps({'generated':datetime.datetime.now(datetime.timezone.utc).isoformat(),'files':sources,'buildings':count,'note':'NYC DCP borough geometry; OSM buildings/airport/bridges under ODbL; OurAirports public-domain runways. NJ shore is an approximate backdrop.'},indent=2))
print('Baked',len(meshes),'mesh chunks;',count,'buildings;',round((OUT/'world.json').stat().st_size/1e6,1),'MB')

from PIL import Image, ImageDraw
image=Image.new('RGB',(2048,2048),(24,44,57));draw=ImageDraw.Draw(image)
def pixel(x,z):return ((x+35000)/70000*2048,(35000-z)/70000*2048)
for mesh in meshes:
    if mesh['kind'] not in ('land','park'):continue
    vertices=mesh['vertices'];tris=mesh['triangles']
    color=(61,80,76) if mesh['kind']=='land' else (57,91,68)
    for i in range(0,len(tris),3):
        draw.polygon([pixel(vertices[k*3],vertices[k*3+2]) for k in tris[i:i+3]],fill=color)
for runway in json.loads((OUT/'runways.json').read_text())['runways']:
    draw.line([pixel(*project(runway['aLon'],runway['aLat'])),pixel(*project(runway['bLon'],runway['bLat']))],fill=(153,185,174),width=2)
image.save(OUT/'gps.png')
print('Baked offline GPS map')
