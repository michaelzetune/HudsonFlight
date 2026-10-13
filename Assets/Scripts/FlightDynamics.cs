using System;
using System.Runtime.InteropServices;

namespace HudsonFlight
{
    public sealed class FlightDynamics : IDisposable
    {
        const string Library = "HudsonFlight";
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr hf_create(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string root, double latitude, double longitude,
            double heading, double terrainFeet, double aglFeet, double speedKnots);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] static extern void hf_destroy(IntPtr handle);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] static extern int hf_set(IntPtr handle, string name, double value);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] static extern int hf_get(IntPtr handle, string name, out double value);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] static extern int hf_step(IntPtr handle, int count);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr hf_error();
        IntPtr handle;
        static Exception Error() => new InvalidOperationException(Marshal.PtrToStringAnsi(hf_error()));

        public FlightDynamics(string root, double latitude, double longitude, double heading,
                              double terrainFeet, double aglFeet = 4.7, double speedKnots = 0)
        {
            handle = hf_create(root, latitude, longitude, heading, terrainFeet, aglFeet, speedKnots);
            if (handle == IntPtr.Zero) throw Error();
        }
        public double Get(string name)
        {
            if (hf_get(handle, name, out double value) == 0) throw Error();
            return value;
        }
        public void Set(string name, double value)
        {
            if (hf_set(handle, name, value) == 0) throw Error();
        }
        public void Step(int count = 1) { if (hf_step(handle, count) == 0) throw Error(); }
        public void Dispose()
        {
            if (handle != IntPtr.Zero) { hf_destroy(handle); handle = IntPtr.Zero; }
            GC.SuppressFinalize(this);
        }
        ~FlightDynamics() { Dispose(); }
    }
}
