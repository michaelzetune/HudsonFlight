#!/usr/bin/env python3
"""Clip the aerial photography to shoreline geometry, exposing animated waterways."""
import json,pathlib,math
import numpy as np
import mapbox_earcut as earcut
from shapely.geometry import shape,Polygon,box
from shapely.ops import transform,unary_union
ROOT=pathlib.Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/StreamingAssets/Visuals'
SX=111319.49079327358*math.cos(math.radians(40.7772));SZ=111319.49079327358
def project(lon,lat,z=None):return ((lon+73.8726)*SX,(lat-40.7772)*SZ)
land=[transform(project,shape(f['geometry'])).simplify(1.2,preserve_topology=True) for f in json.loads((ROOT/'Vendor/scenery/boroughs.geojson').read_text())['features']]
from shorelines import new_jersey_land
land.append(transform(project,new_jersey_land()));land=unary_union(land).buffer(0)
meshes=[]
for tile in json.loads((OUT/'aerial.json').read_text())['tiles']:
 x,z,size=tile['x'],tile['z'],tile['size'];p=land.intersection(box(x,z,x+size,z+size))
 parts=[p] if p.geom_type=='Polygon' else list(getattr(p,'geoms',[]))
 verts=[];uv=[];tris=[]
 for part in parts:
  if part.geom_type!='Polygon' or part.is_empty:continue
  rings=[list(part.exterior.coords)[:-1]]+[list(r.coords)[:-1] for r in part.interiors]
  v=np.array([p for r in rings for p in r],dtype=np.float64);offset=len(verts)//3
  ids=earcut.triangulate_float64(v,np.cumsum([len(r) for r in rings],dtype=np.uint32)).reshape(-1,3)
  for a,b,c in ids:
   ab=v[b]-v[a];ac=v[c]-v[a]
   if ab[0]*ac[1]-ab[1]*ac[0]>0:b,c=c,b
   tris += [offset+int(a),offset+int(b),offset+int(c)]
  for xx,zz in v:verts.extend([round(xx,3),3.07 if size<4000 else 3.05,round(zz,3)]);uv.extend([(xx-x)/size,(zz-z)/size])
 if tris:meshes.append(dict(name=tile['file'],texture=tile['file'],vertices=verts,uv=uv,triangles=tris))
(OUT/'aerial-ground.json').write_text(json.dumps({'meshes':meshes},separators=(',',':')))
print('AERIAL SHORELINES:',len(meshes),'tiles;',sum(len(m['triangles'])//3 for m in meshes),'triangles')

(OUT/'SHORELINE-CREDITS.txt').write_text('NJDEP Coastline 2002\nhttps://mapsdep.nj.gov/arcgis/rest/services/Features/Land_CAFRA_coast/MapServer/10\nThis game was developed using New Jersey Department of Environmental Protection Geographic Information System digital data, but this secondary product has not been verified by NJDEP and is not state-authorized or endorsed.\nNYC shorelines: NYC Department of City Planning borough boundaries.\n')
