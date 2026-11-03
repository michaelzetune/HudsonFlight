#!/usr/bin/env python3
"""Stream NYC's 2014 CityGML survey into offline, georeferenced Unity mesh chunks.
Survey geometry is authoritative for shape; facade openings remain an approximation.
"""
import array, io, json, math, pathlib, struct, zipfile
import numpy as np
import mapbox_earcut as earcut
from lxml import etree as ET
from pyproj import Transformer
from PIL import ImageColor
import shapefile
from shapely.geometry import Polygon, shape, mapping, box
from shapely.ops import transform, unary_union
from shapely.strtree import STRtree
ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/StreamingAssets/Visuals/survey';OUT.mkdir(parents=True,exist_ok=True)
G='{http://www.opengis.net/gml}';B='{http://www.opengis.net/citygml/building/1.0}'
SX=111319.49079327358*math.cos(math.radians(40.7772));SZ=111319.49079327358
projection=Transformer.from_crs(2263,4326,always_xy=True)
def project(lon,lat,z=None):return ((lon+73.8726)*SX,(lat-40.7772)*SZ)
def survey_project(x,y,z=None):return project(*projection.transform(x,y))
clip=box(-15500,-10000,800,5500)
landmarks=json.loads((OUT.parent/'landmarks/footprints.json').read_text())['buildings']
landmark_polys=[shape(m['footprint']).buffer(1.5) for m in landmarks if not m['bridge']]
landmark_tree=STRtree(landmark_polys)
osm=[];tags=[]
for e in json.loads((ROOT/'Vendor/scenery/buildings.json').read_text())['elements']:
 try:
  p=Polygon([project(v['lon'],v['lat']) for v in e.get('geometry',[])]).buffer(0)
  if p.area>10 and p.intersects(clip):osm.append(p);tags.append(e.get('tags',{}))
 except (ValueError,TypeError):pass
osm_tree=STRtree(osm)
tiles=json.loads((OUT.parent/'aerial.json').read_text())['tiles']
buckets={};records=[];matched=0;replaced=0

def style(poly,height,seed):
 global matched
 candidates=osm_tree.query(poly,predicate='intersects');best=None;area=0
 for i in candidates:
  a=poly.intersection(osm[i]).area
  if a>area:area=a;best=int(i)
 t=tags[best] if best is not None and area>poly.area*.2 else {}
 if 'building:colour' in t or 'building:material' in t:matched+=1
 mat=t.get('building:material','')
 glass=mat=='glass' or (not mat and height>110)
 kind=255 if glass else 0 if mat=='brick' else 85 if mat in ('stone','limestone','marble','plaster') else 170
 if not mat and height<75:kind=0 if seed%4==0 else 85
 palettes={0:['#795e50','#887268','#6f5046','#9c7c66'],85:['#b9b3a4','#a29c8f','#c0b7a6','#999589'],170:['#a5a4a0','#92999b','#b0aaa0','#a59c90'],255:['#71878e','#68858e','#829498','#6b7985']}
 color=t.get('building:colour',palettes[kind][seed%4])
 try:rgb=ImageColor.getrgb(color)[:3]
 except ValueError:rgb=ImageColor.getrgb(palettes[kind][seed%4])
 levels=t.get('building:levels','')
 try:floor=height/float(levels);floor=max(2.6,min(floor,4.5))
 except (ValueError,ZeroDivisionError):floor=3.4
 return (*rgb,kind),floor

def emit(v,rings,roof,color,floor,tile):
 # CityGML rings are outward CCW. Mapping XY/Z to XZ/Y reverses handedness.
 rel=v-v[0];normal=np.sum(np.cross(rel[:rings[0]],np.roll(rel[:rings[0]],-1,axis=0)),axis=0)
 if np.linalg.norm(normal)<1e-7:return
 drop=int(np.argmax(np.abs(normal)))
 ids=earcut.triangulate_float64(np.ascontiguousarray(np.delete(rel,drop,axis=1)),np.cumsum(rings,dtype=np.uint32)).reshape(-1,3)
 if not len(ids):return
 expected=-normal
 cross=np.cross(v[ids[:,1]]-v[ids[:,0]],v[ids[:,2]]-v[ids[:,0]])
 # Some source roof rings are reversed; exterior roof surfaces face upward.
 bad=cross[:,1]<0 if roof else np.einsum('ij,j->i',cross,expected)<0
 ids[bad]=ids[bad][:,[0,2,1]]
 cx,cz=v[:,[0,2]].mean(axis=0)
 key=(int(cx//1000),int(cz//1000),tile['file'] if roof else '')
 b=buckets.setdefault(key,dict(v=array.array('f'),uv=array.array('f'),c=bytearray(),i=array.array('I')))
 offset=len(b['v'])//3;b['v'].extend(v.reshape(-1))
 if roof:
  uv=(v[:,[0,2]]-[tile['x'],tile['z']])/tile['size']
 else:
  direction=np.array([expected[2],0,-expected[0]]);length=np.linalg.norm(direction)
  direction=direction/length if length>1e-6 else np.array([1,0,0])
  width=1.55 if color[3]==255 else 2.7
  uv=np.stack([rel@direction/width,v[:,1]/floor],axis=1)
 b['uv'].extend(uv.reshape(-1));b['c'].extend(bytes(color)*len(v));b['i'].extend((ids+offset).reshape(-1))

archive=zipfile.ZipFile(ROOT/'Vendor/building-upgrade/DA_WISE_GML.zip')
for district in [10,12,13]:
 n=0
 stream=archive.open(f'DA_WISE_GMLs/DA{district}_3D_Buildings_Merged.gml')
 for _,element in ET.iterparse(stream,events=('end',),tag=B+'Building',huge_tree=True):
  surfaces=[]
  for boundary in element.findall(B+'boundedBy'):
   for surf in boundary:
    kind=ET.QName(surf).localname
    for polygon in surf.iter(G+'Polygon'):
     rings=[]
     for ring in polygon.iter(G+'posList'):
      a=np.fromstring(ring.text,sep=' ').reshape(-1,3)
      if len(a)>1 and np.allclose(a[0],a[-1],rtol=0,atol=.001):a=a[:-1]
      if len(a)>=3:rings.append(a)
     if rings:surfaces.append((kind,rings))
  name=element.findtext(G+'name','')
  # Free prior XML siblings: the source district can be several GB uncompressed.
  element.clear()
  while element.getprevious() is not None:del element.getparent()[0]
  if not surfaces:continue
  allv=np.concatenate([r for _,rings in surfaces for r in rings])
  x,z=survey_project(allv[:,0],allv[:,1]);world=np.stack([x,(allv[:,2]-np.min(allv[:,2]))*.304800609601+3,z],axis=1)
  cx,cz=world[:,[0,2]].mean(axis=0)
  if not(clip.bounds[0]<cx<clip.bounds[2] and clip.bounds[1]<cz<clip.bounds[3]):continue
  ground=[];offset=0
  for kind,rings in surfaces:
   for r in rings:
    if kind=='GroundSurface':ground.extend(world[offset:offset+len(r),[0,2]])
    offset+=len(r)
  if len(ground)<3:continue
  # Survey ground rings may contain multiple components; hull is used only for matching.
  from shapely.geometry import MultiPoint
  poly=MultiPoint(ground).convex_hull
  height=float(np.ptp(world[:,1]))
  if poly.area<12 or height<2.5:continue
  if any(poly.intersection(landmark_polys[i]).area>poly.area*.25 for i in landmark_tree.query(poly,predicate='intersects')):
   replaced+=1;continue
  seed=int(name.rsplit('_',1)[-1]) if name.rsplit('_',1)[-1].isdigit() else len(records)
  color,floor=style(poly,height,seed)
  candidates=[t for t in tiles if t['x']<=cx<=t['x']+t['size'] and t['z']<=cz<=t['z']+t['size']]
  tile=min(candidates,key=lambda t:t['size']) if candidates else None
  if tile is None:continue
  offset=0
  for kind,rings in surfaces:
   length=sum(map(len,rings));v=world[offset:offset+length];offset+=length
   if kind=='GroundSurface':continue
   emit(v,[len(r) for r in rings],kind=='RoofSurface',color,floor,tile)
  records.append(dict(name=name,x=round(cx,2),z=round(cz,2),height=round(height,2)))
  n+=1
  if n%5000==0:print('DA',district,n,'buildings',flush=True)
 print('FINISHED DA',district,n,'buildings',flush=True)

manifest=[]
for index,(key,b) in enumerate(sorted(buckets.items())):
 filename=f'chunk-{index:04d}.bin'
 with (OUT/filename).open('wb') as f:
  f.write(struct.pack('<4sII',b'HFSM',len(b['v'])//3,len(b['i'])))
  for field in ['v','uv','c','i']:f.write(b[field])
 manifest.append(dict(file=filename,texture=key[2],vertices=len(b['v'])//3,triangles=len(b['i'])//3))
(OUT/'survey.json').write_text(json.dumps({'chunks':manifest,'buildings':len(records),'facadeTagsMatched':matched,'landmarkReplacements':replaced},separators=(',',':')))
(OUT/'buildings.json').write_text(json.dumps(records,separators=(',',':')))
reader=shapefile.Reader(shp=io.BytesIO(archive.read('DeliveryArea.shp')),dbf=io.BytesIO(archive.read('DeliveryArea.dbf')))
areas=[]
for sr in reader.iterShapeRecords():
 if any(str(v) in ('DA10','DA12','DA13','10','12','13') for v in sr.record):areas.append(transform(survey_project,shape(sr.shape.__geo_interface__)))
coverage=unary_union(areas).intersection(clip)
assert not coverage.is_empty,reader.fields
(OUT/'coverage.json').write_text(json.dumps(mapping(coverage)))
(OUT/'CREDITS.txt').write_text('NYC 3D Building Model, NYC DoITT / City of New York. 2014 aerial survey. LoD 1.5 city geometry with selected LoD 2 landmarks.\nhttps://data.cityofnewyork.us/City-Government/3-D-Building-Model/tnru-abg2\nhttps://raw.githubusercontent.com/CityOfNewYork/nyc-geo-metadata/main/Metadata/Metadata_3DBuildingModel.md\nhttps://maps.nyc.gov/download/3dmodel/DA_WISE_GML.zip\nEPSG:2263 US survey feet converted to local meters. Building base elevations flattened to match this simulator terrain.\nRoof imagery: USGS NAIP (public domain); photographed roofs can differ from survey geometry dates.\nFacade materials/colors where available: OpenStreetMap contributors, ODbL https://www.openstreetmap.org/copyright . Window patterns and unrecorded materials are approximations.\nNewer FlightGear landmarks replace overlapping survey structures.\n')
print('SURVEY:',len(records),'buildings;',matched,'recorded facade styles;',replaced,'landmark replacements;',sum(m['triangles'] for m in manifest),'triangles;',len(manifest),'chunks',flush=True)
