#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p Vendor Build
if [[ ! -d .tools-venv ]]; then python3 -m venv .tools-venv; fi
if command -v uv >/dev/null; then
  uv pip install --python .tools-venv/bin/python -r Tools/requirements.txt
else
  .tools-venv/bin/python -m pip install -r Tools/requirements.txt
fi
if [[ ! -f Vendor/jsbsim-v1.3.2.tar.gz ]]; then
  curl -fL --retry 2 https://github.com/JSBSim-Team/jsbsim/archive/refs/tags/v1.3.2.tar.gz -o Vendor/jsbsim-v1.3.2.tar.gz
fi
echo '9d3f625ebf59be8b4aa3b6dd4e1db72712dc593685ef4e99f254cfd9603ebc01  Vendor/jsbsim-v1.3.2.tar.gz' | shasum -a 256 -c -
if [[ ! -d Vendor/jsbsim-1.3.2 ]]; then tar -xzf Vendor/jsbsim-v1.3.2.tar.gz -C Vendor; fi
.tools-venv/bin/python Tools/fetch_scenery.py || .tools-venv/bin/python Tools/fetch_osm_regions.py
.tools-venv/bin/python Tools/fetch_visual_assets.py
.tools-venv/bin/python Tools/fetch_building_assets.py
.tools-venv/bin/python Tools/prepare_aerial_ground.py
.tools-venv/bin/python Tools/prepare_aircraft.py
.tools-venv/bin/python Tools/prepare_landmarks.py
.tools-venv/bin/python Tools/prepare_city_survey.py
.tools-venv/bin/python Tools/prepare_scenery.py
bash Tools/build_native.sh
echo 'Dependencies and scenery are ready. Run bash Tools/build_game.sh with an activated Unity 6000.3.26f1 editor.'
