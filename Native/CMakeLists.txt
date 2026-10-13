cmake_minimum_required(VERSION 3.18)
project(HudsonFlightNative LANGUAGES C CXX)
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)
set(BUILD_PYTHON_MODULE OFF CACHE BOOL "" FORCE)
set(BUILD_DOCS OFF CACHE BOOL "" FORCE)
add_subdirectory(../Vendor/jsbsim-1.3.2 jsbsim EXCLUDE_FROM_ALL)
add_library(HudsonFlight SHARED FlightBridge.cpp)
target_link_libraries(HudsonFlight PRIVATE libJSBSim)
set_target_properties(HudsonFlight PROPERTIES
  LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/../Assets/Plugins/macOS"
  MACOSX_RPATH ON)
