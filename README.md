# Hudson Flight

A native Apple Silicon macOS flight simulator prototype: Unity 6.3 LTS, C#, and JSBSim 1.3.2.

## Play

Open `~/Applications/Hudson Flight.app` (a copy also lives at `Build/Hudson Flight.app`).
Start on LaGuardia runway 04 with the engine running.
Hold **W** to release the initial brakes and advance the throttle. Use **A/D** to keep
the runway centerline; gently tap **Up** near 55 knots to lift the nose. Aim for
70–80 knots during the initial climb. The visible **Help** button, or **H**, shows
all controls and pauses the simulation.

Keyboard assistance starts **on**. Left/right arrows request a bank up to 30°;
releasing them gently levels the wings and compensates for the propeller's roll.
Assistance also helps hold the takeoff heading and coordinate airborne turns.
Press **T** for direct, unassisted control. Pitch, power, flaps, brakes, stalls,
and landing technique remain under your control. Assistance moves the simulated
control surfaces; it never overrides JSBSim's position or attitude.

The scene fills the entire window at any aspect ratio. Instruments and GPS float
over it on translucent shaded cards. **Tab** hides these cards; **G** hides hints.

| Controls | Action |
| --- | --- |
| Up / Down | Raise / lower nose |
| Left / Right | Bank left / right |
| A / D | Left / right rudder and ground steering |
| W / S | Increase / decrease throttle; setting holds |
| F / Shift+F | Lower / raise flaps (0, 10, 20, 30 degrees) |
| B, held | Wheel brakes |
| [ / ] | Nose-up / nose-down pitch trim |
| T | Toggle keyboard bank/rudder assistance (on by default) |
| Tab | Show / hide instrument overlays |
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
- Rate-limited keyboard controls, optional wings-level/rudder assistance, persistent throttle and pitch trim.
- Cockpit instruments, moving GPS map, airport markers, optional speed/approach hints.
- Collision and crash explanations, runway restart, and touchdown feedback for
  descent rate, runway alignment, and airspeed.
- Real runway endpoints and widths, NYC borough shorelines, selected Manhattan
  building footprints/heights, LGA airport geometry, and major bridge road decks.
- Native ARM64 player and native JSBSim bridge; Metal rendering; local scenery.
- Public-domain USGS/USDA aerial photography over 32 × 28 km of NYC, sampled at
  approximately 2 m/pixel, plus approximately 0.65 m/pixel imagery around LGA.
- Detailed, textured C172P visual model from c172p-team, including glazing,
  landing gear, struts, antennas, and a spinning propeller.
- 79 individually authored, textured NYC landmarks, including Empire State, Chrysler,
  One World Trade Center, One Vanderbilt, Hudson Yards, newer Midtown supertalls,
  the Statue of Liberty, Brooklyn Bridge, and George Washington Bridge.
- 92,857 surveyed buildings across Manhattan and western Queens: actual rooflines,
  setbacks, and courtyards, with aerial roof textures and recorded OSM facade
  colors/materials where available.
- Varied masonry/glass facades, rooftop mechanical structures, textured pavement,
  daylight sky, atmospheric haze, soft shadows, and sky reflections.

## Current limits

This is an early playable prototype, not Microsoft Flight Simulator visual fidelity.
The aircraft now uses a detailed community model. The live instruments remain
screen overlays rather than fully interactive 3D cockpit instruments. The city combines
NYC survey geometry, individually textured community landmarks, and extruded
footprints outside the survey coverage; it is not citywide photogrammetry. The
survey reflects 2014 buildings, with newer landmark models replacing selected
structures. Ordinary building windows are generated, and facade colors/materials
are approximate where no recorded tags exist. Roof imagery and survey capture
dates can differ. Bridges other than Brooklyn and George Washington retain
simplified decks and supports. NYC land is flat; the New Jersey shoreline uses
NJDEP's 2002 coastline, so newer waterfront construction can differ. The other airports have accurate runway
geometry but lack detailed terminals and taxiways. There is no ATC, traffic,
live weather, radio navigation, or detailed cockpit switches. Aerial imagery is
photographic, with its capture-date shadows and flat terrain; it is not live imagery.

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
.tools-venv/bin/python Tools/fetch_visual_assets.py
.tools-venv/bin/python Tools/fetch_building_assets.py
.tools-venv/bin/python Tools/prepare_aerial_ground.py
.tools-venv/bin/python Tools/prepare_aircraft.py
.tools-venv/bin/python Tools/prepare_landmarks.py
.tools-venv/bin/python Tools/prepare_city_survey.py
.tools-venv/bin/python Tools/prepare_scenery.py
```

`Tools/fetch_scenery.py` downloads public inputs. If Overpass is unavailable,
`Tools/fetch_osm_regions.py` obtains smaller airport/bridge extracts directly from
OpenStreetMap. Downloads are cached in `Vendor/scenery`; the game reads only the
baked files under `Assets/StreamingAssets/Scenery` and `Assets/StreamingAssets/Visuals`.
The additional NYC survey archive is approximately 874 MiB; the converter streams
selected districts without extracting the entire multi-gigabyte archive. Prepared
survey meshes use compact binary chunks. The game does not download anything.

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

Version 0.2 additionally checks assisted takeoff (1.2° maximum runway heading
error, 0.1° bank), recovery from a commanded 32.2° left bank to 0.8° after release,
both cameras, transparent overlays, hidden instruments, and a resized viewport.
Updated captures and logs are under `Build/Captures-v2` and `Build/runtime-v2.log`.
Final rendering checks (including instrument and GPS line geometry at non-unit UI
scales) are in `Build/Captures-final` and `Build/runtime-final-visual.log`.
The Manhattan rendering sample averaged 60 FPS over 60 frames at 1898 × 1250 on
this M5; this is a short sample, not a benchmark of every area or flight condition.

Version 0.3 adds 92,857 surveyed buildings and 79 individually textured landmarks.
`Tools/check_city_assets.py` validates coordinates, textures, landmark heights,
mesh indices, and upward roof faces. The full player run passed assisted/manual
takeoff, wing-level recovery, landing, crash and restart, plus roof collision tests
at Empire State (447.4 m including the local base) and One World Trade Center
(544.2 m including the local base). The Manhattan sample averaged 60.0 FPS over
60 frames at 1600 × 1000 on this M5. Full-run evidence is in `Build/runtime-v3.log`;
installed-app rendering checks and close views are in `Build/runtime-v3-final.log`
and `Build/Captures-v3-final`. The installed app averaged 57.5 FPS in its final
60-frame sample after the shoreline correction. These are short samples, not
comprehensive benchmarks.

## Sources

- [NJDEP Coastline 2002](https://mapsdep.nj.gov/arcgis/rest/services/Features/Land_CAFRA_coast/MapServer/10):
  Hudson River west bank and harbor shorelines. This game was developed using
  New Jersey Department of Environmental Protection Geographic Information
  System digital data, but this secondary product has not been verified by NJDEP
  and is not state-authorized or endorsed.

- [NYC 3D Building Model](https://data.cityofnewyork.us/City-Government/3-D-Building-Model/tnru-abg2),
  City of New York / DoITT, based on the 2014 aerial survey. Districts 10, 12, and 13
  are clipped to the local scenery coverage. EPSG:2263 coordinates are converted
  from US survey feet to local meters; base elevations are flattened to match
  the simulator terrain. See `Tools/prepare_city_survey.py` and the shipped credits.
- [FlightGear scenery contributors](https://scenery.flightgear.org/objects?country=us),
  GPL-2.0-or-later: individual NYC landmark meshes and facade textures, downloaded
  from the official TerraSync CDN with per-file SHA-1 verification. Copyright
  remains with each original author. Original models, textures, placement files,
  conversion scripts, and license accompany the converted assets under
  `StreamingAssets/Visuals/landmarks`. Historical Twin Towers models are excluded.

- [JSBSim 1.3.2](https://github.com/JSBSim-Team/jsbsim/releases/tag/v1.3.2), LGPL-2.1:
  source and original license are retained in `Vendor/jsbsim-1.3.2`; the license
  also ships with the aircraft data in the app. Source archive SHA-256:
  `9d3f625ebf59be8b4aa3b6dd4e1db72712dc593685ef4e99f254cfd9603ebc01`.
- [NYC Department of City Planning borough boundaries](https://data.cityofnewyork.us/City-Government/Borough-Boundaries/gthc-hcne).
- [© OpenStreetMap contributors](https://www.openstreetmap.org/copyright), ODbL:
  building footprints/heights, airport surfaces, and bridge road geometry. Raw
  extracts and deterministic conversion tools are retained in the project.
- [OurAirports](https://ourairports.com/data/), public-domain runway data.
- [USGS/USDA NAIP aerial imagery](https://imagery.nationalmap.gov/arcgis/rest/services/USGSNAIPImagery/ImageServer),
  public domain, cached for offline use. See `Tools/fetch_visual_assets.py` for
  bounds, sampling resolution, and the source service.
- [c172p-team C172P](https://github.com/c172p-team/c172p), GPL-2.0,
  pinned to `63ba85273b7f44637eb4683dcdc8f97af7821653`. Converted visual assets,
  original AC3D model and textures, license, and contributor acknowledgments ship
  under `StreamingAssets/Visuals/aircraft`; `Tools/prepare_aircraft.py` contains
  the conversion. The flight dynamics remain the original JSBSim model.

`Assets/StreamingAssets/Scenery/sources.json` records input checksums and the bake date.
The app is signed locally for this Mac; it is not notarized for public distribution.
