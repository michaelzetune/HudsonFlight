#!/usr/bin/env python3
"""Download public NYC geography once; the game never needs a network connection."""
import csv, io, json, pathlib, urllib.request, urllib.parse, time

ROOT = pathlib.Path(__file__).resolve().parents[1]
CACHE = ROOT / 'Vendor/scenery'
OUT = ROOT / 'Assets/StreamingAssets/Scenery'
CACHE.mkdir(parents=True, exist_ok=True)
OUT.mkdir(parents=True, exist_ok=True)

def download(name, url, data=None):
    path = CACHE / name
    if not path.exists():
        print('Downloading', name, flush=True)
        req = urllib.request.Request(url, data=data, headers={'User-Agent': 'HudsonFlight-local-prototype/0.1'})
        with urllib.request.urlopen(req, timeout=180) as response:
            content = response.read()
        path.write_bytes(content)
    return path.read_bytes()

runways = csv.DictReader(io.StringIO(download('runways.csv', 'https://davidmegginson.github.io/ourairports-data/runways.csv').decode()))
airports = {'KLGA': ('LaGuardia', 21), 'KJFK': ('John F. Kennedy', 13), 'KTEB': ('Teterboro', 9), 'KEWR': ('Newark Liberty', 18)}
rows = []
for r in runways:
    if r['airport_ident'] not in airports or r['closed'] == '1': continue
    if not all(r.get(k) for k in ['le_latitude_deg','le_longitude_deg','he_latitude_deg','he_longitude_deg']): continue
    rows.append(dict(airport=r['airport_ident'], name=r['le_ident']+'/'+r['he_ident'],
        aLat=float(r['le_latitude_deg']), aLon=float(r['le_longitude_deg']),
        bLat=float(r['he_latitude_deg']), bLon=float(r['he_longitude_deg']),
        width=float(r['width_ft'] or 150)*.3048, elevation=airports[r['airport_ident']][1]*.3048))
(OUT/'runways.json').write_text(json.dumps({'runways': rows}))

boroughs = download('boroughs.geojson', 'https://data.cityofnewyork.us/resource/gthc-hcne.geojson')
(OUT/'boroughs.geojson').write_bytes(boroughs)
print('Runways:', len(rows), 'borough bytes:', len(boroughs), flush=True)

queries = {
 'buildings': '[out:json][timeout:120];way[building][height](40.695,-74.025,40.81,-73.935);out geom;',
 'airport': '[out:json][timeout:25];(way[aeroway](40.762,-73.90,40.795,-73.855);way[building](40.762,-73.90,40.795,-73.855););out geom;',
 'bridges': '[out:json][timeout:25];way[bridge][highway~"motorway|trunk|primary|secondary"](40.68,-74.045,40.86,-73.90);out geom;'
}
for name, query in queries.items():
    body = urllib.parse.urlencode({'data': query}).encode()
    try:
        content=download(name+'.json', 'https://overpass-api.de/api/interpreter?'+urllib.parse.urlencode({'data':query}))
        json.loads(content)
        (OUT/(name+'.osm.json')).write_bytes(content)
        print(name, len(content), 'bytes', flush=True)
    except Exception as exc:
        print(name, 'download failed:', exc, flush=True)
        raise
