#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
cmake_bin="${HUDSON_CMAKE:-$PWD/.tools-venv/bin/cmake}"
"$cmake_bin" -S Native -B Build/Native -DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_ARCHITECTURES=arm64
"$cmake_bin" --build Build/Native --target HudsonFlight --parallel 6
mkdir -p Assets/StreamingAssets/JSBSim/aircraft
cp -R Vendor/jsbsim-1.3.2/aircraft/c172p Assets/StreamingAssets/JSBSim/aircraft/
cp -R Vendor/jsbsim-1.3.2/engine Assets/StreamingAssets/JSBSim/
cp -R Vendor/jsbsim-1.3.2/systems Assets/StreamingAssets/JSBSim/
cp Vendor/jsbsim-1.3.2/COPYING Assets/StreamingAssets/JSBSim/COPYING
codesign --force --sign - Assets/Plugins/macOS/libHudsonFlight.dylib
