#!/usr/bin/env python3
"""Cache official NYC CityGML and FlightGear's GPL-2.0+ NYC landmark sources."""
import concurrent.futures, hashlib, pathlib, urllib.request, urllib.parse
ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'Vendor/building-upgrade'
BASE='https://terrasync.b-cdn.net/'

def fetch(url,path,sha1=None):
    path.parent.mkdir(parents=True,exist_ok=True)
    if path.exists() and (not sha1 or hashlib.sha1(path.read_bytes()).hexdigest()==sha1):return
    request=urllib.request.Request(url,headers={'User-Agent':'HudsonFlight-offline-scenery/0.3'})
    with urllib.request.urlopen(request,timeout=120) as response,open(str(path)+'.part','wb') as f:
        while data:=response.read(1024*1024):f.write(data)
    temp=pathlib.Path(str(path)+'.part')
    if sha1 and hashlib.sha1(temp.read_bytes()).hexdigest()!=sha1:raise ValueError('Checksum mismatch: '+str(path))
    temp.replace(path)

def main():
    query=dict(f='geojson',where='1=1',geometry='-74.3,40.55,-73.91,40.91',geometryType='esriGeometryEnvelope',inSR=4326,outSR=4326,outFields='OBJECTID',returnGeometry='true',spatialRel='esriSpatialRelIntersects')
    fetch('https://mapsdep.nj.gov/arcgis/rest/services/Features/Land_CAFRA_coast/MapServer/10/query?'+urllib.parse.urlencode(query),ROOT/'Vendor/scenery/nj-coastline.geojson')
    fetch('https://maps.nyc.gov/download/3dmodel/DA_WISE_GML.zip',OUT/'DA_WISE_GML.zip')
    for bucket in ['w074n40','w075n40']:
        folder='Objects/w080n40/'+bucket
        index=OUT/'flightgear'/folder/'.dirindex'
        fetch(BASE+folder+'/.dirindex',index)
        files=[line.split(':') for line in index.read_text().splitlines() if line.startswith('f:')]
        print(bucket,len(files),'files,',round(sum(int(f[3]) for f in files)/1e6,1),'MB',flush=True)
        def one(row):fetch(BASE+folder+'/'+row[1],index.parent/row[1],row[2])
        with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:list(pool.map(one,files))
    print('NYC survey and landmark source downloads complete',flush=True)

if __name__=='__main__':main()
