#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
editor="${HUDSON_UNITY:-$HOME/Applications/Unity/6000.3.26f1/Unity.app/Contents/MacOS/Unity}"
bash Tools/build_native.sh
"$editor" -batchmode -nographics -quit -projectPath "$PWD" -executeMethod HudsonFlight.EditorBuild.Build -logFile "$PWD/Build/unity-build.log"
codesign --force --deep --sign - "Build/Hudson Flight.app"
echo "Built: $PWD/Build/Hudson Flight.app"
