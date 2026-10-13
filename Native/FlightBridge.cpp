#include <FGFDMExec.h>
#include <algorithm>
#include <cmath>
#include <memory>
#include <string>

// C ABI: no C++ exceptions, strings, or ownership cross the P/Invoke boundary.
namespace {
thread_local std::string error;
struct Flight { JSBSim::FGFDMExec fdm; };
template<class F> int guard(F action) {
  try { action(); error.clear(); return 1; }
  catch (const std::exception& e) { error = e.what(); }
  catch (...) { error = "Unknown JSBSim error"; }
  return 0;
}
}
extern "C" {
const char* hf_error() { return error.c_str(); }
void* hf_create(const char* root, double latitude, double longitude,
                double heading, double terrainFeet, double aglFeet, double speedKnots) {
  std::unique_ptr<Flight> flight;
  if (!guard([&] {
    if (!root || !std::isfinite(latitude) || !std::isfinite(longitude))
      throw std::runtime_error("Invalid initial conditions");
    flight = std::make_unique<Flight>();
    auto& f = flight->fdm;
    f.SetDebugLevel(0);
    f.SetRootDir(SGPath(root));
    f.SetAircraftPath(SGPath("aircraft"));
    f.SetEnginePath(SGPath("engine"));
    f.SetSystemsPath(SGPath("systems"));
    f.Setdt(1.0 / 120.0);
    if (!f.LoadModel("c172p")) throw std::runtime_error("Cannot load c172p aircraft data");
    f.SetPropertyValue("ic/lat-geod-deg", latitude);
    f.SetPropertyValue("ic/long-gc-deg", longitude);
    f.SetPropertyValue("ic/psi-true-deg", heading);
    f.SetPropertyValue("ic/terrain-elevation-ft", terrainFeet);
    f.SetPropertyValue("ic/h-agl-ft", aglFeet);
    f.SetPropertyValue("ic/vc-kts", speedKnots);
    f.SetPropertyValue("fcs/mixture-cmd-norm", 1);
    f.SetPropertyValue("fcs/throttle-cmd-norm", 0.12);
    if (!f.RunIC()) throw std::runtime_error("JSBSim initial conditions failed");
    f.SetPropertyValue("propulsion/set-running", -1);
  })) return nullptr;
  return flight.release();
}
void hf_destroy(void* handle) { delete static_cast<Flight*>(handle); }
int hf_set(void* handle, const char* property, double value) {
  return guard([&] {
    if (!handle || !property || !std::isfinite(value)) throw std::runtime_error("Invalid property write");
    static_cast<Flight*>(handle)->fdm.SetPropertyValue(property, value);
  });
}
int hf_get(void* handle, const char* property, double* result) {
  return guard([&] {
    if (!handle || !property || !result) throw std::runtime_error("Invalid property read");
    *result = static_cast<Flight*>(handle)->fdm.GetPropertyValue(property);
    if (!std::isfinite(*result)) throw std::runtime_error("Non-finite simulation state");
  });
}
int hf_step(void* handle, int count) {
  return guard([&] {
    if (!handle || count < 1 || count > 120) throw std::runtime_error("Invalid simulation step");
    auto& f = static_cast<Flight*>(handle)->fdm;
    for (int i = 0; i < count; ++i)
      if (!f.Run()) throw std::runtime_error("JSBSim stopped");
  });
}
}
