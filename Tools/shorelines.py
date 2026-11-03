"""Shared shoreline geometry for terrain, photographic ground, and GPS."""
import json,pathlib
from shapely.geometry import shape,box,Point,Polygon
from shapely.ops import unary_union,polygonize
ROOT=pathlib.Path(__file__).resolve().parents[1]
def new_jersey_land():
    source=json.loads((ROOT/'Vendor/scenery/nj-coastline.geojson').read_text())
    bounds=box(-74.3,40.55,-73.91,40.91)
    lines=[shape(f['geometry']).intersection(bounds) for f in source['features']]
    parts=list(polygonize(unary_union([*lines,bounds.boundary])))
    mainland=next(p for p in parts if p.covers(Point(-74.10,40.75)))
    shell=Polygon(mainland.exterior)
    # Keep offshore islands; keep inland water holes empty; omit the ocean polygon.
    islands=[p for p in parts if not p.intersects(bounds.boundary) and not shell.covers(p.representative_point())]
    return unary_union([mainland,*islands])
