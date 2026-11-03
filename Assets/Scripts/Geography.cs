using System;
using UnityEngine;

namespace HudsonFlight
{
    public static class Geography
    {
        public const double OriginLat = 40.7772, OriginLon = -73.8726;
        const double MetersPerDegree = 111319.49079327358;
        static readonly double LongitudeScale = Math.Cos(OriginLat * Math.PI / 180) * MetersPerDegree;
        public static Vector3 Position(double latitude, double longitude, double altitudeMeters = 0) =>
            new Vector3((float)((longitude-OriginLon)*LongitudeScale), (float)altitudeMeters,
                        (float)((latitude-OriginLat)*MetersPerDegree));
        public static double Latitude(Vector3 point) => OriginLat + point.z/MetersPerDegree;
        public static double Longitude(Vector3 point) => OriginLon + point.x/LongitudeScale;
    }
    [Serializable] public class Runway
    {
        public string airport, name;
        public double aLat, aLon, bLat, bLon;
        public float width, elevation;
        public Vector3 A => Geography.Position(aLat,aLon,elevation);
        public Vector3 B => Geography.Position(bLat,bLon,elevation);
        public float Heading => Mathf.Repeat(Mathf.Atan2((B-A).x,(B-A).z)*Mathf.Rad2Deg,360);
        public bool Contains(Vector3 point, float margin=0)
        {
            var direction=(B-A).normalized;
            var offset=point-A;
            float along=Vector3.Dot(offset,direction);
            float cross=Vector3.Dot(offset,Vector3.Cross(Vector3.up,direction));
            return along>=-margin && along<=Vector3.Distance(A,B)+margin && Mathf.Abs(cross)<=width/2+margin;
        }
    }
    [Serializable] public class RunwayCollection { public Runway[] runways; }
    [Serializable] public class SceneryMesh { public string name, kind; public float[] vertices; public int[] triangles, styles; public float[] color; }
    [Serializable] public class SceneryCollection { public SceneryMesh[] meshes; }
}
