#!/usr/bin/env python3
"""Cache public-domain USGS aerial tiles and pinned GPL C172P visual assets."""
import concurrent.futures, json, math, pathlib, shutil, time, urllib.parse, urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[1]
CACHE = ROOT / 'Vendor/visual-upgrade'
OUT = ROOT / 'Assets/StreamingAssets/Visuals'
REV = '63ba85273b7f44637eb4683dcdc8f97af7821653'
SERVICE = 'https://imagery.nationalmap.gov/arcgis/rest/services/USGSNAIPImagery/ImageServer'

def fetch(url, dest):
    dest.parent.mkdir(parents=True, exist_ok=True)
    if dest.exists(): return
    for attempt in range(4):
        try:
            data = urllib.request.urlopen(url, timeout=100).read()
            if len(data) < 100 or data.startswith(b'{"error'): raise ValueError(data[:300])
            temp = dest.with_suffix(dest.suffix + '.part'); temp.write_bytes(data); temp.replace(dest)
            return
        except Exception:
            if attempt == 3: raise
            time.sleep(2 + attempt * 3)

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    tiles=[]
    for z in range(-20000, 8000, 4000):
        for x in range(-20000, 12000, 4000):
            tiles.append(dict(x=x,z=z,size=4000,resolution=2048,file=f'aerial/{x}_{z}.jpg'))
    # A finer layer around the departure airport for low-altitude flight.
    for z in range(-2000, 2000, 1000):
        for x in range(-2000, 2000, 1000):
            tiles.append(dict(x=x,z=z,size=1000,resolution=1536,file=f'aerial/lga_{x}_{z}.jpg'))
    def tile(t):
        lon=lambda x:-73.8726+x/(111319.49079327358*math.cos(math.radians(40.7772)))
        lat=lambda z:40.7772+z/111319.49079327358
        x,z,s=t['x'],t['z'],t['size']
        query=urllib.parse.urlencode(dict(bbox=','.join(map(str,[lon(x),lat(z),lon(x+s),lat(z+s)])),bboxSR=4326,imageSR=4326,size=f"{t['resolution']},{t['resolution']}",format='jpg',compressionQuality=92,adjustAspectRatio='false',f='image'))
        dest=CACHE/t['file'];fetch(SERVICE+'/exportImage?'+query,dest)
        target=OUT/t['file'];target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(dest,target)
        print(t['file'],dest.stat().st_size,flush=True)
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:list(pool.map(tile,tiles))
    (OUT/'aerial.json').write_text(json.dumps({'tiles':tiles}))
    fetch(f'https://api.github.com/repos/c172p-team/c172p/git/trees/{REV}?recursive=1',CACHE/'aircraft-tree.json')
    tree=json.loads((CACHE/'aircraft-tree.json').read_text())
    if tree['sha']!=REV:raise ValueError('Cached aircraft listing does not match the pinned revision')
    paths=['LICENSE','README.md','Thanks','Models/c172-common.ac','Models/c172p.xml']
    paths += [x['path'] for x in tree['tree'] if x['path'].startswith('Models/') and x['path'].count('/')==1 and x['path'].endswith('.png')]
    for p in paths:fetch(f'https://raw.githubusercontent.com/c172p-team/c172p/{REV}/{p}',CACHE/'c172p'/p)
    shutil.copyfile(CACHE/'c172p/LICENSE',OUT/'C172P-LICENSE.txt')
    shutil.copyfile(CACHE/'c172p/Thanks',OUT/'C172P-THANKS.txt')
    (OUT/'CREDITS.txt').write_text(f'Aerial imagery: USGS, USDA, The National Map; NAIP public-domain orthoimagery.\nSource: {SERVICE}\nAircraft visual model: c172p-team and contributors, GPL-2.0, revision {REV}.\nhttps://github.com/c172p-team/c172p\nOriginal assets are retained in Vendor/visual-upgrade/c172p; conversion: Tools/prepare_aircraft.py.\n')

if __name__ == '__main__':main()
