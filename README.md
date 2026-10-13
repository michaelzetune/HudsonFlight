# Hudson Flight

A native Apple Silicon macOS flight simulator prototype: Unity 6.3 LTS, C#, and JSBSim 1.3.2.

## Play

Open `~/Applications/Hudson Flight.app` (a copy also lives at `Build/Hudson Flight.app`).
Start on LaGuardia runway 04 with the engine running.
Hold **W** to release the initial brakes and advance the throttle. Use **A/D** to keep
the runway centerline; gently tap **Up** near 55 knots to lift the nose. Aim for
70–80 knots during the initial climb. The visible **Help** button, or **H**, shows
all controls and pauses the simulation.

| Controls | Action |
| --- | --- |
| Up / Down | Raise / lower nose |
| Left / Right | Bank left / right |
| A / D | Left / right rudder and ground steering |
| W / S | Increase / decrease throttle; setting holds |
| F / Shift+F | Lower / raise flaps (0, 10, 20, 30 degrees) |
| B, held | Wheel brakes |
| [ / ] | Nose-up / nose-down pitch trim |
| C | Cockpit / chase camera |
| Right mouse drag | Look around |
| Middle click | Center view |
| G | Toggle optional flight hints |
| H | Help |
| Esc | Pause |
| R | Restart while paused or after a crash |

The navigation panel cycles through runway ends at LGA, JFK, Teterboro, and Newark.
The GPS is north-up and centers on the aircraft; its gold line extends the selected
runway's final approach. Headings are **true**, as labeled on the instrument.
Clear daylight and calm wind are fixed. No network connection is required by the
game after scenery preparation.

## What is implemented

- JSBSim C172P flight dynamics at 120 Hz, including propulsion, stalls, flaps,
  control surfaces, landing-gear contact and wheel braking.
- Rate-limited keyboard control deflections, persistent throttle and pitch trim.
- Cockpit instruments, moving GPS map, airport markers, optional speed/approach hints.
- Collision and crash explanations, runway restart, and touchdown feedback for
  descent rate, runway alignment, and airspeed.
- Real runway endpoints and widths, NYC borough shorelines, selected Manhattan
  building footprints/heights, LGA airport geometry, and major bridge road decks.
- Native ARM64 player and native JSBSim bridge; Metal rendering; local scenery.

## Current limits

This is an early playable prototype, not Microsoft Flight Simulator visual fidelity.
The cockpit and aircraft use original simple geometry. Buildings are extruded
footprints, with simplified Empire State Building and One World Trade Center forms.
Bridges currently have simplified decks and supports. NYC land is flat; New Jersey's
shoreline is an approximate backdrop. The other airports have accurate runway
geometry but lack detailed terminals and taxiways. There is no ATC, traffic,
live weather, radio navigation, satellite imagery, or detailed cockpit switches.

The C172P model is upstream JSBSim's model, not a newly validated aircraft model.
Crash limits are gameplay choices (for example, a 650 ft/min touchdown threshold),
not published aircraft operating limits. Navigation data is for this game only.

## Build

Unity editor: `~/Applications/Unity/6000.3.26f1/Unity.app`.
The Unity CLI is installed at `~/.unity/bin/unity`. An active Unity license is
needed for editor builds; the standalone game doesn't need the editor.
On a fresh checkout, run `bash Tools/bootstrap.sh` to download the pinned native
dependency and scenery inputs and prepare the project-local build tools.

```sh
cd ~/Projects/HudsonFlight
bash Tools/build_game.sh
```

`HUDSON_UNITY` can override the editor executable; `HUDSON_CMAKE` can override
the CMake executable. The default CMake lives in `.tools-venv`.

To rebuild the offline scenery from cached source data:

```sh
.tools-venv/bin/python Tools/prepare_scenery.py
```

`Tools/fetch_scenery.py` downloads public inputs. If Overpass is unavailable,
`Tools/fetch_osm_regions.py` obtains smaller airport/bridge extracts directly from
OpenStreetMap. Downloads are cached in `Vendor/scenery`; the game reads only the
baked files under `Assets/StreamingAssets/Scenery`.

## Verification

```sh
python3 Tools/test_flight.py
HUDSON_CAPTURE_DIR="$PWD/Build/Captures" \
  "Build/Hudson Flight.app/Contents/MacOS/Hudson Flight" \
  --hudson-smoke -screen-width 1440 -screen-height 900 -screen-fullscreen 0 \
  -logFile "$PWD/Build/runtime-smoke.log"
```

The native tests exercise runway contact, a running engine, takeoff, repeated reset,
and pitch/roll/rudder/steering signs. The build verifies C# P/Invoke in the editor.
The opt-in player smoke harness saves cockpit, help, chase, Manhattan, landing, and
crash screenshots. It verifies engine/ground state, flies a test takeoff and landing,
checks touchdown feedback, deliberately crashes, verifies reset, and exits. Its
scripted test pilot is not part of normal play.

Latest full player verification on this M5 Mac (2026-10-12): native Metal rendering,
takeoff at 82 kt / 193 ft AGL, touchdown at 356 ft/min with 3° alignment error,
hard-impact crash at 4,168 ft/min, and a successful runway restart. Visual captures
are in `Build/Captures`; the run log is `Build/runtime-smoke.log`.

## Sources

- [JSBSim 1.3.2](https://github.com/JSBSim-Team/jsbsim/releases/tag/v1.3.2), LGPL-2.1:
  source and original license are retained in `Vendor/jsbsim-1.3.2`; the license
  also ships with the aircraft data in the app. Source archive SHA-256:
  `9d3f625ebf59be8b4aa3b6dd4e1db72712dc593685ef4e99f254cfd9603ebc01`.
- [NYC Department of City Planning borough boundaries](https://data.cityofnewyork.us/City-Government/Borough-Boundaries/gthc-hcne).
- [© OpenStreetMap contributors](https://www.openstreetmap.org/copyright), ODbL:
  building footprints/heights, airport surfaces, and bridge road geometry. Raw
  extracts and deterministic conversion tools are retained in the project.
- [OurAirports](https://ourairports.com/data/), public-domain runway data.

`Assets/StreamingAssets/Scenery/sources.json` records input checksums and the bake date.
The app is signed locally for this Mac; it is not notarized for public distribution.
