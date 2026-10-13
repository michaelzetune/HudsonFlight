#!/usr/bin/env python3
"""Fallback for an unavailable Overpass service: small public OSM API extracts."""
import pathlib, urllib.request
ROOT=pathlib.Path(__file__).resolve().parents[1]
CACHE=ROOT/'Vendor/scenery'
OUT=ROOT/'Assets/StreamingAssets/Scenery'
def download(name,url):
 p=CACHE/name
 if not p.exists():
  print('Downloading',name,flush=True)
  req=urllib.request.Request(url,headers={'User-Agent':'HudsonFlight-local-prototype/0.1'})
  with urllib.request.urlopen(req,timeout=120) as r: p.write_bytes(r.read())
 return p.read_bytes()
import xml.etree.ElementTree as ET
import json

def extract(name,bbox):
    content=download(name+'.osm','https://api.openstreetmap.org/api/0.6/map?bbox='+bbox)
    root=ET.fromstring(content)
    nodes={n.attrib['id']:{'lat':float(n.attrib['lat']),'lon':float(n.attrib['lon'])} for n in root.findall('node')}
    ways=[]
    for way in root.findall('way'):
        tags={t.attrib['k']:t.attrib['v'] for t in way.findall('tag')}
        if not ('aeroway' in tags or 'building' in tags or ('bridge' in tags and 'highway' in tags)):continue
        ids=[n.attrib['ref'] for n in way.findall('nd')]
        if not all(i in nodes for i in ids):continue
        ways.append({'id':int(way.attrib['id']),'type':'way','tags':tags,'geometry':[nodes[i] for i in ids]})
    return ways

airport=[]
for x in range(3):
 for y in range(2):
  west=-73.899+x*.017;south=40.764+y*.015
  airport.extend(extract(f'lga-{x}-{y}',f'{west},{south},{west+.017},{south+.015}'))
airport=list({w['id']:w for w in airport}.values())
(CACHE/'airport.json').write_text(json.dumps({'elements':airport}))
(OUT/'airport.osm.json').write_text(json.dumps({'elements':airport}))
bridges=[]
for name,bbox in [('gwb','-73.959,40.847,-73.942,40.855'),('queensboro','-73.965,40.751,-73.948,40.762'),('brooklyn','-74.005,40.700,-73.990,40.712'),('manhattan','-73.997,40.700,-73.982,40.718'),('williamsburg','-73.980,40.710,-73.960,40.719')]:
    ways=extract(name,bbox)
    bridges.extend(w for w in ways if 'bridge' in w['tags'] and 'highway' in w['tags'])
(CACHE/'bridges.json').write_text(json.dumps({'elements':list({w['id']:w for w in bridges}.values())}))
print('OSM fallback:',len(airport),'airport ways and',len(bridges),'bridge segments')
