Version 20.4 - 2020-9-9
-----------------------

- Hotfix to prevent planetary overlay window from opening when it shouldn't

Version 20.3 - 2020-9-4
-----------------------

- New resource overlay quick setting controls
	- Adjust low resource concentration cutoff for all maps
	- Quickly adjust resource color settings
	- Display small resource overlay legend on maps
	
- Option to hide resources that are not present on a given celestial body from the resource selection menus
- Zoom map resource overlay calculates local min and max resource concentration values

- Fix numerous bugs related to custom resources and resource UI features

- Fix bug causing big map data source to not update properly

- Adjust part masses

- Option to disable visual maps for troubleshooting
	- GameData/SCANsat/PluginData/Settings.cfg set VisibleMapsActive = False

- Fix visual maps for new gas giants (KSP 1.10 and up)
- Fix bug when trying to draw biome map for planets with no biomes
- Add some missing localization fields for resource scanners

Version 20.2 - 2020-7-07
-----------------------

- Fix memory leak related to visual maps

- Add scanning coverage indicators to big map

- Add function to allow the zoom map to remember the last used zoom level

- Update French translations (don-vip)

- Reduced memory usage by visual maps
- Fixed several errors related to visual maps when using Kopernicus

- Allow vessels with old scanner parts to be loaded in VAB/SPH
- Fix error when PluginData folder for settings file has been deleted
- Clarified surface-in-daylight requirement for certain scanners
- Updated KSPedia entry to reflect newer features of big map, zoom map, and Breaking Ground surface feature detection

Version 20.1 - 2020-6-17
-----------------------

- Fix big map body selection menu with some Kopernicus planet packs (also fixes a few settings menus)

- Various adjustments to part tech tree position and scanner values

- Properly hide old versions of parts
- Minor UI bug fixes

Version 20.0 - 2020-5-16
-----------------------

- Warning: Save files cannot be downgraded to a previous version of SCANsat after updating to version 20.0 or higher

- Scanning part revamp
    - 15 new parts by Nertea
	- Multiple instruments for each scanner type
	- Old parts are soft-deprecated; they are still loaded but won't appear in the VAB/SPH part list
	
- New visual scan mode
    - Low and high resolution RGB color visual map scans
	- New science results for high resolution visual map scans
	
- Overhaul of scanner instrument types
    - Some scanners require the surface to be in daylight to function
    - New combination of scan types for each instrument
	- Resource scanning now handled mostly independently from stock resource scanning
	- All high resolution resource scans fall under a single scanner type
	- Updated contract types
	
- Other improvements
    - Added more scanner information to scanner right-click part menus
	- Added more scanner information to VAB/SPH part list info
	
- Bug fixes and other updates
    - Fix KSPedia entry for KSP 1.8 and above
	- Fix various Unity script issues
	- Fix RPM map display errors
	- Fix MechJeb landing site integration

- Changes from version 19.3
    - Fix KSPedia entry in KSP 1.9
    - Minor part balance updates
	- Resource scanning updates
	- Fix Breaking Ground surface feature detection

Version 19.3 - 2020-4-8
-----------------------

- Updated KSPedia entry

- Fix MechJeb integration

- Make low resolution resource scan less effective 

- Part texture and model updates
- Minor part balance updates
- Part attachment rule fixes

- Some minor UI updates

Version 19.2 - 2020-3-28
-----------------------

- Add all new scanner parts
- Old parts soft-deprecated

- Initial part balance
	- Tech tree
	- Part cost
	- Scanning variables
	
- Fix contract configurator integration

- Fix KSPedia

Version 19.1 - 2020-2-20
-----------------------

- Fix compatibility with Kopernicus

- Add new scanner info group in the right click menu of all scanner parts
- Fix stock resource scanner modules

- Fix scanning contracts (maybe?)


Version 19.0 - 2020-1-29
-----------------------

- Warning: Save files cannot be downgraded to a previous version of SCANsat after updating to version 19.0 or higher

- Update for KSP 1.8.1

- Visual scanning mode
     - New visual scanner part (Nertea)
     - Visual map mode
     - Visual scan science data

- Daylight scanner requirement
     - Some scanners require that the surface be in daylight to be scanned
     - Requirement can be disabled in the settings menu

- Overhaul of resource scanning
     - All high resolution resources are scanned through a single scanner type
     - Existing data will be converted to the new system

- Fix some Unity script initialization errors
- Fix RPM map display errors

Version 18.14 - 2019-10-28
-----------------------

- Update for KSP 1.8
- Fix big map control buttons
- Fix ground track indicators
- Fix BTDT anomaly viewer shaders

Version 18.13 - 2019-8-21
-----------------------

- Fixed an error related to Breaking Ground surface features in Sandbox mode

Version 18.12 - 2019-8-20
-----------------------

- Allow for specifying the color of the map view ground track indicators

- Fix some bugs related to Breaking Ground surface feature displays
- Auto refresh timer for the zoom map starts timing after the map has finished building, not before

Version 18.11 - 2019-7-3
-----------------------

- Allow for detection of Breaking Ground DLC surface features
    - Use the BTDT scanner to detect surface features
    - Surface features displayed on the zoom map in the area surrounding the active vessel
    - Can be viewed in the instruments window
    - Breaking Ground DLC must be installed for this feature to function

- Add auto-refresh toggle for the zoom map
    - Toggle between no refresh, 4 second refresh, and 8 second refresh

- Add French translations - by don-vip

- Fix a bug related to flags that was preventing the zoom map and big maps from functioning correctly
- Fixed a few Chinese localization file language tags

Version 18.10 - 2018-12-31
-----------------------

- Fix a bug that was preventing scanners with multiple sensor types from working correctly

Version 18.9 - 2018-12-5
-----------------------

- Performance improvements for background scanning

- Fix Metal Ore resource scanning support for Extraplanetary Launchpads

- Allow for deeper layers of sub-moons in all celestial body menus

Version 18.8 - 2018-10-25
-----------------------

- Update for KSP 1.5

- Additional Spanish translations by Fitiales

Version 18.7 - 2018-7-27
-----------------------

- Update for KSP 1.4.5

- Fix lat/long display coordinates


Version 18.6 - 2018-4-30
-----------------------

- Update for KSP 1.4.3

- Significant performance improvement for the background scanning algorithm
     - Particularly noticeable when many scanners are active and time warp is at maximum

- Reduce garbage allocation
     - Particularly related to the information shown when the mouse is over a map window
	 - Also reduced when the Background Scanning tab of the settings window is open
	 - Fix a bug that caused increased garbage allocation when waypoint icons were turned on for the maps
	 
- Some UI related performance improvements

- Fix some typos in science results localization files
- Fix German language tags in localization files
- Updates to Chinese translations

Version 18.5 - 2018-3-14
-----------------------

- Update for KSP 1.4

- Update shaders and asset bundles to Unity 2017
- Fix a rounding in error in the scanning coverage that was blocking contract offers
- Anomaly icons show as grey until they are scanned with the BTDT

Version 18.4 - 2018-2-8
-----------------------

- Performance fixes for scanning coverage check

Version 18.3 - 2018-2-7
-----------------------

- Complete Portuguese translations
- Updates to localization files
	- Parts, manufacturer/flag and contract localization
	
- Calculate scanning coverage based on approximation of actual spherical surface area instead of simple rectangular area
	- Affects contract completion and science data collection

- Fix bugs related to some resources for the big map
- Fix bugs in Overdrive compatibility
- Fix long resource and planet names in drop down menus


Version 18.2 - 2017-11-22
-----------------------

- Overhaul of all SCANsat contracts
     - Contracts now differ based on difficulty
     - Easy contracts only generate for home system bodies and ask for one scan
     - Medium and High difficulty contracts generate for non-home system bodies and ask for a group of scans
     - Contracts have several dependencies
            - Unlocking the required parts
            - Not having already finished all of the required scans for the target
            - Target bodies are selected based on your progression

- Improvements made to slope maps
     - Slope maps now account for map scale or zoom factor
     - Significantly improved usefulness for the zoom map

- Fix some potential bugs when survey contract waypoints are updated
- Fix bug in background scanning toggles in the settings window
- Some minor UI performance tweaks
- Add a KSPedia button to the top of the settings window
     - It will try to open the SCANsat page, but this only works if KSPedia has already been opened at least once


Version 18.1 - 2017-10-11
-----------------------

- Update for KSP 1.3.1

- Terrain color palettes defined in new config file
     - Custom color palettes can be added by editing the file or using MM patches

- Fix bug with big map celestial body list
- Fix bug with narrow band scanner requirement when more than one resource is in use
- Slight change in how anomaly information is reported by the BTDT
- Remove unneeded ASET RPM panel Module Manager patches
- Fix SCANsat agency title

Version 18.0 - 2017-07-8
-----------------------

- Update for KSP 1.3

- Complete UI overhaul:
	- All aspects of the SCANsat UI have been replaced with the new Unity UI
		- This results in a significant reduction in performance impact and garbage creation, particularly when several SCANsat windows are open at the same time
		- UI scaling is integrated with stock UI scaling (master scale) – separate scaling for SCANsat only is also available
		- All text is rendered with TextMesh Pro, resulting in clearer text, particularly at small and large scales, and better compatibility with other languages
		- New option is available to switch between KSP and the old, Unity style UI styling
	- New stock toolbar menu
		- In the flight scene, the stock toolbar button will now spawn a small menu with buttons for all of the SCANsat windows
		- Enable or disable this option in the settings window
	- New settings window
		- Features multiple pages for different groups of settings

- Zoom map overhaul:
	- It now uses an orthographic map projection to eliminate distortion near the map center
		- This is similar to the polar map projection, but can be centered anywhere
	- Further separated from the big map
		- Menu to set the resource selection (if more than one is installed) 
		- Menu to set the map type (altimetry, biome, slope)
		- Toggles for all of the map overlays
	- Three different map size options
		- Full map with top and bottom control bars
		- Medium map with only top control bar
		- Compact map with no control bar
	- Option to reset the map to the current vessel position
	- Option to lock the map to the current vessel position
		- Will re-center the map on the vessel position whenever it is refreshed, zoomed, etc…
	- Buttons to move the map center in all four directions
	- Fixes problems with the “Require Narrow Band Scanner” resource scanning requirement
		- Should function correctly with both stock scanning and SCANsat-style resource scanning

- Localization:
	- Localization support for most aspects of SCANsat
		- Part descriptions, science results, part right-click menu fields, tooltips, settings window help tooltips, and some of the UI text
		- Planet names, biome names, and resources names are all localized
		- Some aspects of the UI, particularly the settings window, are not localized
		- The KSPedia entry is not localized
	- Spanish by Delthiago98
	- Chinese by Summerfirefly
	- Russian by Ser and RarogCmex
	- German by h0yer

- New Features:
	- Stock waypoint system integration
		- Big map and zoom map can be used to set stock waypoints
		- Click the waypoint button in the lower-right corner of the map to activate waypoint mode
			- Select a point on the map and left-click there to assign the waypoint location
			- Edit the waypoint name using the text input field
			- Click set to generate the waypoint
		- Optional MechJeb integration
			- MechJeb integration toggled through the settings window
			- Click the MJ button instead of set to use the waypoint as a MechJeb landing site
			- Requires MechJeb installed, and a functional and upgraded MechJeb core on the current vessel
	- Options for map generation speed
		- A slider in the settings window allows for three map speed options
			- 1 – One row of all map types will be drawn per frame
			- 2 – Two rows of all map types will be drawn per frame
			- 3 – Two to four rows of the maps will be drawn per frame, depending on the map type
	- Color Management Window updates
		- Integrated into the new settings window
		- Text input fields are available for most values with a slider
		- More biome options for the biome colors used and the white borders
		- New HSV color picker for all of the color management tabs
			- Uses a standard, saturation vs brightness (value) square color picker, with a hue slider to the right
			- Displays the current selection in RGB, HSV, and Hex color values
			- Allows for manual input of RGB values, using either 0-1, or 0-255 values
	- Updated KSPedia entry
		- Reflects all changes made for version 18
		- Several new pages with information about the different map windows
	- Miscellaneous new features
		- Scanning width now properly accounts for latitude, resulting in scans of even width at all latitudes
		- New settings file; generated in the GameData/SCANsat/PluginData folder after first running KSP
			- Contains most of the options found in the settings window
		- Settings windows options for resetting specific sets of scanning data for an individual celestial body, or all bodies
		- Biome map legend
			- Displays all of the biome colors as sections on the map legend
			- The zoom map legend shows only the biomes present in the map window
		- Map legend tooltips 
			- Displays altitude or biome name when the mouse is over the map legend
		- Day/night map terminator overlay for all maps
			- Toggled with the sun and moon icon
		- New part module specifically for handling science experiments
			- Module: SCANexperiment
		- Celestial body list on the big map is ordered by distance to the sun
			- The current body is displayed at the top of the list
			- Moons are grouped together with their parent
		- New anomaly icon, a question mark with an open circle at the bottom
			- The open circle denotes the anomaly position

- Bug Fixes:
	- Fix some floating point errors that cause the big map to break at certain map sizes
	- Fix shader bugs preventing the BTDT anomaly readout in the Instruments window from working
	- Fix a science point exploit when collecting the same scan data from two different vessels
	- Fix a potential error with RPM maps with resource overlays
	- Fix errors in Module Manager patches for resource scanners used by other mods
	- Changes from version 17.9
		- More KSPedia pages
		- Fix an error with setting the UI scale of the zoom map


Version 16.11 - 2016-11-4
-----------------------

- Update for KSP 1.2.1

- Disable window dragging for big map and zoom map when in IVA
- Most resource scanners now use the stock Narrow-Band for high resolution scanning
     - The "Require Narrow-Band" option in the resource settings is slightly broken by this
	 - Disable that option if using lots of CRP resources

Version 16.10 - 2016-10-24
-----------------------

- Update Toolbar wrapper
- Fix Unity initialization issues
- Fix error in ground track display
- Fix hiding windows in KSC scene

Version 16.9 - 2016-10-13
-----------------------

- Update for KSP 1.2 final
- Update to new resource consumption system
- Fix intermittent background scanning gaps
- Only show the manual map size input field in the flight scene

Version 16.8 - 2016-9-23
-----------------------

- Fix not loading background scanning vessels
- Add support for displaying new vessel types
- Detect new anomaly types
- Garbage reduction and performance improvements
     - Primarily in the background scanning mechanism


Version 16.7 - 2016-9-13
-----------------------

- Update for KSP 1.2

Version 16.6 - 2016-8-18
-----------------------

- Fix compatibility with Kerbalism

Version 16.5 - 2016-8-10
------------------------

- Fix bug in electricity usage that was preventing scanning

Version 16.4 - 2016-8-9
------------------------

- Update contracts for Contract Configurator 1.15+ (severedsolo)
- Switch to using ModuleResource and RESOURCE nodes for all power consumption (NathanKell)
- Calculate the local min and max terrain for RPM maps
- Hidden map fill cheat option
    - Add "cheatMapFill = True" to the SCANcolors.cfg file
- Fix potential Toolbar related errors (bssthu)

Version 16.3 - 2016-6-27
------------------------

- Adds new resource types and Module Manager scanner configs for KSP Interstellar Extended Version 1.9
     - Deprecated unused He-3 and Thorium resource types

- Updated for Mechjeb 2.5.8


Version 16.2 - 2016-6-21
------------------------

- Update for KSP 1.1.3

- Updates and minor changes to KSPedia entry
- Add terrain height multipliers for use with Module Manager when using planet rescaling mods (Sigma Dimensions)
- Rename and clarify the background scanning toggle button
- Clarify background scanning status in context menus and scanning indicators
- Implement a silent science collection method (KOS support for not opening the results window)
- Add new terrain color palettes and change some default palettes
- Allow planetary resource configs to be loaded if no global config is defined
- Some GUI performance and garbage creation optimizations


Version 16.1 - 2016-5-2
------------------------

- Update for KSP 1.1.2

- Update API for Kerbalism support
- Fixed an error that prevented the UI from opening after exiting the Mission Control building

Version 16.0 - 2016-4-19
------------------------

- Update for KSP 1.1 final release
- Update for MechJeb 2.5.7

- KSPedia Entry
    - 17 New KSPedia pages covering SCANsat basics and features
	
- Add science experiment for low resolution resource scan (M700 scanner)
    - Adjust other science reward amounts
	
- Add window scaling function
    - Adjust scale in the Settings window

- Misc and Bug Fixes
	- Add target selection button to big map
	- Add new RPM storage module; Module Manager config edited to add this module to any part with an internal space
	- Make scanners easier to turn off when they run out of power
	- Fix error that prevented orbit lines from crossing the East/West border in zoom maps
	- Window reset button will now reset all window positions and scale
	- Fix bug in instruments window resource readout
	- Make sure new save files apply all SCANsat default values and options
	- Fixed potential error with ground tracks
	- Fixed potential error with .csv exporter


Version 14.9 - 2016-3-6
------------------------

- Add new zoom map buttons to small map, big map and toolbar
- Improved zoom map functionality
    - When zoom map is first opened it targets the active vessel
	- Added re-sync to vessel button in the top-right corner

- Added slope cutoff slider to color management window
    - Adjusts the cutoff between the two color pairs
	- Lower cutoff to make slope map better in the zoom map

- Color Management window button removed from small map and big map in flight scene
    - Added Color Management window button to the top of the settings menu

- Added GeoEnergy resource support for PathFinder
- When a new planet is detected SCANsat tries to determine its highest elevation point
    - Used for planets that don't have a terrain config defined in the SCANcolors.cfg file; ie Kopernicus planets
	- Max elevation point used to set the max height used for SCANsat maps
	- Max elevation sometimes returns values too high; adjust values in the color management window
- Some changes in how data is loaded from the save file; no end-user effect
- Fixed error that prevented SCANsat contracts from generating before the player left the Kerbin system

Version 14.8 - 2016-2-10
------------------------

- Fix RPM and Kopernicus incompatibility
- Add new CRP resources and configs

Version 14.7 - 2016-1-16
------------------------

-Fix Contract config error

Version 14.6 - 2016-1-15
------------------------

- Fix Kopernicus compatibility (Thanks ThomasKerman)
- Allow mouse-over info and overlay tooltips to fall back to low resolution resource data in some cases
- Add a warning when stock resource scanning is disabled without having Module Manager installed
- Fixed some issues affecting scanner power usage and scanning altitude indicators
- Allow for included contracts to be disabled through the ScanSatOfficial Contract Type (Thanks DBT85)
- Fix a bug that was preventing loading of the color config file when additional planets are installed
- Add MetalOre as a SCANsat resource and MetalOre scanner modules to EPL scanner parts

Version 14.5 - 2015-12-22
------------------------

- Misc and Bug Fixes
	- Fix some .csv export related issues
	- Allow for disabling the stock resource scan threshold
	- Fix ocean depth indicator
	- Fix multi spectral sensor normal map
	- Fix some potential issues when using multiple different sensors of the same type
	- Add new survey waypoints as they are generated

Version 14.4 - 2015-11-14
------------------------

- New export functions
	- Option to sse a true grey-scale color scheme
	- Option to export a .csv file with coordinates and terrain height for each pixel
	- Option to manually specify the big map width
	
- Stock resource scan threshold
	- Active when stock resource scanning is disabled
	- Set a SCANsat resource scanning threshold level above which the stock resource scan will apply for each celestial body

- Misc and Bug Fixes
	- Fix science data return on transmission failure
	- Zoom maps use variable terrain hieght ranges based on the local min and max terrain height
	- Add an ocean depth indicator to the instruments window
	- Add a distance-to-landing-target indicator on instruments window when within 15km
	- Fix drawing the terrain height behind the biome map
	- Fix some errors in ground track drawing
	

Version 14.3 - 2015-11-10
------------------------
- Update for KSP 1.0.5

- In-Game Help Function
	- Help section for Settings window, Resource Settings window, and Color Selection window
	- Activate help mode with the question mark button in the top right
	
- Localization project
	- All help function strings are included in a config file
	- Local translations can be added to this file to replace the English text

- Misc and Bug Fixes
	- Fix bug while switching vessels from the small map vessel list
	- Fix planetary overlay tooltips while in the tracking station
	- Fix a potential loading error
	- Fix bug generating terrain height maps on planets with PQS errors; prevents endless NRE spam
	- Fix some problems with the background terrain height map generator
	- Power usage works correctly up to 10000X time warp, instead of 1000X
	- Remove debug log messages during planetary overlay map generation
	- Change the default anomaly marker and close box icons to standard text

Version 14.2 - 2015-8-29
------------------------

- Performance and RAM usage improvements
	- Storage modules for each planet use less RAM
	- Planetary overlays use less RAM
	- Biome overlay texture size can be changed independently of resource size
	- Planetary overlays generated on a different thread when possible
		- This reduces or prevents the slight freeze or hiccup when generating a new overlay
		
- Raster Prop Monitor
	- Additional functions added to RPM panels
	- The standard MFDs use the four buttons on the left for resource and anomaly/waypoint overlays
	- ALCOR pods also have additional functions where possible
	- Increased map rendering performance by reducing map texture size
		- Map texture size and resource overlay quality can be adjusted in the RPM configs
	- Prevent the non full-screen panels from drawing maps underneath opaque sections of the display (ie the left hand ALCOR panel)
	- A Module Manager patch is required for full ALCOR MFD functionality
		
- Parts
	- Slight reduction in size of SAR and MultiSpectral scanners
	- Proper use of MODEL node scaling; fixes KIS inventory size problems (thanks Kerbas_ad_astra)
	- Slightly increased scanning width and altitude parameters for M700 (clamshell) resource scanner
	
- Contract Configurator Contracts included
	- Based on a slightly modified version of severedsolo's SCANsatLite pack
	
- SCAN agency and manufacturers group added

- Big map and KSC map
	- Resource overlays now use the same quality settings as the planetary overlays
	- Resource mouse-over info now follows the same narrow-band scanner restrictions as planetary overlay tooltips
	- Slope added to mouse-over info for terrain scanned with high-resolution terrain scanner
	
- Small map
	- Biome maps available on the small map
		- The vessel info readout will display the current biome (if scanned) for each vessel
	- The altitude readout for each vessel is now dependent on scanning coverage
	
- Instruments UI
	- New rules for the altitude and slope readout
		- Below 1000m full height-above-terrain and slope info is given regardless of scanning coverage
		- Above 1000m altitude the readout is based on low or high resolution terrain scanning; slope is only given for high resolution scans
		
- Color Management Window
	- Planet selection boxes available for terrain and resource color configs
	- Fix possible bugs created by adding and deleting mod planets
	
- Misc and bug fixes
	- Resource overlay colors properly use the assigned abundance ranges
	- Added a resource overlay control window toolbar button
	- Update the active vessel in the instruments window
	- Various minor performance improvements
	- Various minor UI tweaks

Version 14.1 - 2015-7-15
------------------------

- Across the board performance improvement for map rendering

- Fixed a bug causing performance drop for greyscale maps

- Fixed a bug preventing landed resource scanners from working


Version 14.0 - 2015-7-5
------------------------

- Updated for KSP 1.0.4

- Updated for MechJeb 2.5.2

- Resource Scanning System Overhaul
------------------------

- Replaces or augments most aspects of the stock surface resource scanning system

	- New option available to disable stock scanning
		- Disables the orbital survey
		- Replaces the resource concentration readout from scanners with SCANsat modules
		- Replaces the resource map found on Narrow-Band scanners with a modified SCANsat zoom map
		- Right-click menu resource concentration readout dependent on SCANsat scanning coverage 
		- Stock resource scanning data can be erased for each planet (does not affect resource biome scanning data)

	- Stock resource parts adapted for SCANsat resource scanning
		- Module Manager required for SCANsat resource scanning
		- M700 (clamshell) scanner used for low-resolution resource scanning; covers all resources
			- A one-time patch on updating will deactivate all resource scanners to prevent existing M700 scanners from being used as they were in version 12
		- Narrow-band scanners used for high-resolution, individual resource scanning

	- Planetary map overlay
		- New window to control resource, terrain, and biome map overlays
		- Biome overlays limited to stock color scheme for now
		- Tooltips for overlays when the mouse cursor is over the planet
		- Overlay window buttons added to the big map and small map windows; also added to the Toolbar menu
		- Select between all available resources, terrain maps, and biome maps

	- Resource settings window
		- New window to control resource options and planetary overlay quality
		- Biome lock, instant scan, and narrow-band scanner options are the same as in previous versions
		- The instant scan option is automatically disabled when Disable Stock Resources is activated
		- Both SCANsat and stock resource scanning data for the current planet can be reset
		- Interpolation and map overlay size can be configured
		- Biome maps use twice the map height as resource maps (512*256 for resources; 1024*512 for biomes by default)
		- Coverage Transparency option shows scanned areas in grey if there are no resources present; useful for determining what areas have already been scanned
		
	- SCAN High Definition Camera
		- New part module to replace the stock hi def camera, found on the narrow-band scanner
		- Opens a modified SCANsat zoom window
		- Only active when stock resource scanning is disabled

- Other Updates
------------------------
		
- SCANsat big map
	- Resource overlay now matches the planetary overlay more closely
	- Interpolation is used to generate the resource map overlay; this greatly speeds up big map rendering speed
	- Zoom map uses more accurate interpolation method for resource overlays
	- Biome maps can be generated without the white border
	
- Add ground track display for scanning vessels while in map mode
	- Scanning width is displayed for the widest FOV
	- Width is only accurate at the equator
	
- Terrain height map databases generated in the background
	- Maps generated once per game session for each planet
	- These maps are used to generate the small map and the terrain planetary overlay
	- Performance impact during map generation is negligible
	- Greatly speeds up small map rendering and terrain overlays
	- An accelerated map generation method is used if the small map is open or a terrain overlay is selected from a planet which doesn't have its terrain map already created
	
- A resource selection drop down has been added to the zoom map
	- Can be used to change the seleceted resource or disable the overlay
	
- Instrument window resource readout
	- Resource abundance at the current location is displayed on the instruments window
	- When more than one resource is present buttons along the side allow for switching the resource readout
	- Narrow-band scanner restrictions apply if that option is selected in the resource settings window


- Bug Fixes and Misc Features
------------------------

	- Waypoint location correct when waypoint coordinates are stored with nonsense values (ie. -120 S...)
	- Active vessel properly updates for the big map orbit display
	- Prevent pole scanning overlap at the north pole
	- Fixed slope calculation over water
	- Fix bug when applying terrain color config to non-clamped color scheme
	- Fix bug with orbit overlays in zoom map
	- Fix bug allowing scanners to continue scanning without power
	- Fix another bug with scanning near the poles
	- Fix a bug causing duplicate grid lines to be drawn on the space center map

	- Highlight active selection in drop down menus
	- Stop animations when they reach their endpoints - may prevent FAR from getting stuck recalculating the vessel surface
	- Various UI tweaks and updates

- Updates from Version 13.4

	- Fix ground tracks being displayed on the wrong planet
	- Fix bug in overlay control window while a planet's terrain map is being generated
	

Version 12.1 - 2015-5-30
------------------------

- Updated for MechJeb 2.5.1

- Added SCANsat sensor type and color configs for He-3
     - SCANsat sensortype = 512

- Fix a bug while adding resource scanners in the editor
- Add resource scanner action groups
     - Only work when the scanner is deployed
	 
- Fix MapTraq texture location


Version 12 - 2015-5-3
------------------------

- Updated for KSP 1.0.2

Parts:
- All part textures converted to DDS format
- Part textures reduced in size; total RAM saving of ~20MB
- RADAR scanner moved to Basic Science tech node
- Part drag and temperature properties adjusted

Zoom Map:
- Entirely new zoom map window

- The window is a now a separate object, created by right-clicking somewhere on the big map
	- Can be dragged and re-sized independently of the big map

- Window Controls
	- Zoom in and out buttons are available on the top row
	- Right click within the zoom map to zoom in and re-center the map
	- Left click to zoom out
	- Middle click, or Modifier Key (ALT) + Right-click to re-center without changing the zoom level
	- Zoom lever indicated along the top row; click the indicator to re-sync the zoom map with the big map

- Overlay Options
	- Vessel orbit, waypoints, and anomaly locations can be toggled independently of their settings on the big map
	- Resource overlays will be shown in the zoom map
	
- Landing Waypoint Selection
	- The target selection icon in the upper-left can be selected to activate target selection mode
	- Click anywhere in the zoom map to select a target site
	- The site will be marked with a target icon on the zoom map, the big map, and as an overlay on the planet surface in map mode
	- Targets are persistent; one can be selected for each planet
	- click within the zoom window, but outside of the map itselft to cancel target selection and clear any existing targets
	
- MechJeb Landing Guidance
	- When MechJeb is installed the target selection mode can be switched to interact with the MechJeb Landing Guidance Module
	- A MechJeb core must be on your current vessel and the Landing Guidance Module must be unlocked in the R&D center
	
Stock Resource Integration:
- The new stock resource system has been integrated into SCANsat

- Multiple modes of operation are available

- Default Mode
	- When scanning a planet using the stock Orbital Survey Scanner instrument all SCANsat resource maps for that planet will be filled in
	- These are available for display on the big map
	
	- The biome lock is active, giving only rough estimates of resource abundance until surface surveys are conducted
	
	- The zoom window can only show resource overlays if a vessel with a narrow-band scanner is in orbit around the planet and its inclination is high enough to cover the area shown in the zoom map

- SCANsat Mode
	- In this mode SCANsat map overlays are decoupled from the stock scanning system
	- All resources must be scanned using normal SCANsat methods to be shown on the map overlay
	
FinePrint Waypoint Integration:
- Waypoints generated by FinePrint contracts can be displayed on SCANsat maps

- A new waypoint icon is available on the big map and zoom map

- The waypoint name is shown when the mouse-cursor is over it

- The SCANsat Instruments Window displays the name of a waypoint when your current vessel is within range

- Also works with custom waypoints added through nightingale's Waypoint Manager

Color Management Updates:
- Color options for slope, biome and resource maps are now available
	- An HSV color selection wheel is available for custom color selection
	- Click the mouse on the color wheel to select a color hue and saturation level
	- Adjust the value (brightness) slider to the right

- Slope maps use two sets of colors, for high and low slope values

- Biome map end-point colors can be adjusted
- Stock biome map colors can be used in place of SCANsat colors
- The terrain transparency for biome maps can be adjusted

- Resource end-point colors can be adjusted
- Resource cut-off values can be adjusted for each planet
- Resource overlay transparency can be adjusted

External Color Config File:
- All color options are saved to an external file 
	- GameData/SCANsat/Resources/SCANcolors.cfg
	- Each tab in the Color Management Window has a save option, this will update the values in the config file
	
- Terrain and Resource color options are saved for each planet; values for addon planets can be added as well

- Values in the config file serve as default values
	- New save files will use these values
	- Existing save files can reset color values to these defaults using the Color Management Window

Bug Fixes and Miscellaneous Updates:
- Docking, un-docking, decoupling, breaking your vessel etc... while active SCANsat sensors are onboard will not result in spurious sensor activity anymore

- Includes a fixed Active Texture Management config file to prevent SCANsat icons from being altered

- Window positions made persistent
	- The big map position is saved and will remain persistent through different game sessions
	- Other window positions are only persistent during a single game session

- Fixed a bug with data resets in the tracking center

- Fixed Instruments Window slope calculation at high latitudes
- Instruments Window now displays terrain altitude while the vessel is on the surface

- Various UI tweaks and fixes

- Remove Community Tech Tree support

Updates From Version 11.6:
- Updates to Zoom Map resource overlay restriction
	- Less frequent checks for suitable vessels in orbit
	- Fix bug when checking parts with multiple narrow-band scanner modules
	
- Fix MechJeb integration

- Fix Blizzy toolbar icons

Version 10 - 2015-1-29
---------------------

User Interface:
- Complete replacement of SCANsat user interface

- Stock App Launcher Button included
	- Toggles the SCANsat small map, which can be used to open all other windows
	- Toggles the KSC map in the Space Center or Tracking Station scenes
	- Can be toggled on/off in the settings menu
	- Does not replace Blizzy78's Toolbar buttons

- New SCANsat Big Map
	- Text buttons replaced with drop down menus and icon toggles
	- Maps for different planets can be selected at any time
	- Replaced longitude/latitude texture grid overlay with simple line drawing overlay
	
- New KSC and Tracking Station window
	- Fixed-size version of the big map
	
- Improvements to big map elevation data caching
	- Reduced memory usage
	- Faster map rendering when switching projection types
	
- New Color Management window
	- Used to change color palettes for elevation maps
	- Used to set various terrain settings
	- All settings are saved for each planet
	
- Improved SCAN instruments window
	- Altitude above terrain is shown correctly while in time warp
	- Localized slope is indicated based on a 3X3 grid centered 5m around the vessel
		
Resources:
- Resource overlay and selection is controlled entirely through the big map
	- Resource selection is handled through a drop down menu found along the top of the map
	- Resource overlays are toggled by the resource icon on the lower left of the map
	
- Updated to support Regolith 1.4
	- Regolith Biome lock settings can be toggled in the SCANsat settings window

- SCANsatKethane not functional

- ORSX support removed
	
Code Base:
- Significant internal code changes and rearrangement

- Anything not relying entirely on the public API methods in SCANUtil will likely break
	
Miscellaneous and Bug Fixes:
- Maps exported to GameData/SCANsat/PluginData folder; the location where they should have been	
- Science results text added courtesy of Olympic1
- Uranium resource name changed to Uraninite to support the Community Resource Pack
- SETI Rebalance project compatibility added for Community Tech Tree
- Prevent scanning coverage from temporarily reporting 100%, which disrupted some SCANsat contracts
- From v9; Fixed automatic camera movement while the KSC map was active in the tracking station
- From v9; Fix potential error with stock toolbar when changing scenes


Version 8.1 - 2014-12-19
---------------------

- Updated for KSP 0.90

- Fixes for biome related issues due to 0.90 changes

- Fixes for orbit renderer in early career mode; no orbit overlay on 1st tier tracking station

- All parts use .mbm textures

- Community Tech Tree support added
	- All parts rebalanced for cost and tech tree position

Version 8 - 2014-10-8
---------------------

- Updated for KSP 0.25

Resource Scanning:
- Planetary resource overlay for big map
	- Supports ORSX resources
	- All resources types can be scanned in the background; no need to remain in control of a vessel while scanning
	- All resource scanning data is persistent; no different from standard SCANsat sensors
	- Resource types controlled through the SCANsat settings menu
	- Resources scanner types defined through included config file	
	- Support for Modular Kolonization System and Karbonite are provided in their respective packages
	
- ORSX resources
	- Resources displayed on the big map
	- Overlay colors can be defined in the included resource scanner config file

Parts:
- MapTraq deprecated
	- Part still exists but is not available in the VAB/SPH
	- Part functionality is completely removed
	- Module Manager configs for adding the MapTraq SCANsat module should be removed (having the module shouldn't hurt though)
	
- Scanning altitude indicator is present for all SCANsat scanners, including resource scanners
	- Displayed in the right-click context menu
	
- Improved Multi-Spectral scanner
	- Improved clickability
	- More efficient model; 60% reduction in triangle count
	
- Rescaled the BTDT to be much smaller

- Part cost balancing

BackEnd Changes:
- Background scanning will function in every scene where time passes
	- Flight, Map, Tracking Station and Space Center
	- Can be turned off from the SCANsat settings menu
- SCANsat parts not required for background scanning during flight
- SCANsat parts not required for toolbar icons and functional maps
	- Maps and menus don't work outside of the flight/map scenes
	
- Persistent scanning data storage has been altered
	- Requires a one-time, automatic conversion from the old to the new method
	- There have been no complaints as of yet, but users upgrading from SCANsat v6.x may want to make backups of any existing save files
	
- ModStatistics removed
	
Bug Fixes:
- Science can be gathered from all planets
	- Reduced science return for planets without terrain/biomes
- Active scanners no longer play through their animation on startup; they start fully deployed
- Toolbar icons fallback to place holder textures if the default SCANsat icons are moved/altered/deleted
- Prevent small map from opening improperly in non-toolbar version
- Prevent debug spam on EVA science collection
- Improved support for command pods/cockpits using Raster Prop Monitor
	- Possibly prevent problems such as issue #63

	
Version 6.1 - 2014-07-19
-----------------------------

- Update for KSP v 0.24

- Folder structure significantly changed; you must delete any old SCANsat installations

- SCANsatRPM integrated into standard SCANsat
     - You must delete any SCANsatRPM folders; do not install SCANsatRPM from any source
	 
- ModStatistics Added

- Some minor bug fixes
- SCANsat flag added
- Initial part cost balancing
	
	
Version 6.0 - 2014-05-17 (frozen by tg)
------------------------

* Update for KSP version 0.23.5. This version of SCANsat now requires KSP 0.23.5.

Major Changes:

+ [NEW] Big Map Caching. When not scanning, rendering of the Big Map is much, much faster. [Thanks! DMagic!]

* [KNOWN ISSUE] Sometimes the cache is not properly updated, resulting in strange looking Big Maps.
				Use the map resize button ( [\\], right hand size), resize the map a bit.
				This will reset the cache, and fix whatever is wrong.

+ [NEW] [OPTIONAL] Toolbar support. Strongly suggested, because the experience
	is better and more consistent with the Toolbar. [thanks DMagic!]

- [REMOVED] Removed the old expanding/contracting floating SCANsat button. Replaced instead with
	either a Toolbar toolbar or with nothing, but maps become visible upon starting a scan. [Thanks Dmagic!]

+ [DOCS] New README docuemntation is in Markdown format, see: https://github.com/S-C-A-N/SCANsat [tg]

+ [DOCS] New documentation in the form of imgur albums, see: https://scansat.imgur.com/ [tg]

+ [INFO] Future releases can be found here: https://github.com/S-C-A-N/SCANsat/releases/latest [tg]

+ [INFO] Source code can be found here: https://github.com/S-C-A-N/SCANsat [tg]

Minor Changes:

+ [NEW] Added support for showing the vessel icons for Asteroids in orbit around the same
			mainBody. [tg]

+ [NEW] Include SCANsatRPM by default in packaging. [tg]

+ [NEW] S.C.A.N. logo [thanks K3 | Chris!]

- [REMOVED] Non-animated parts no longer show an 'Extend' option in EDITOR. [DMagic]

+ [COMPATABILITY] "Change access modifiers to allow for more control from RPM". [thanks Dmagic!]

+ [BUGFIX] The Multispectral scanner should now be easier to highlight and click. [tg]

+ [BUGFIX] Scanning high inclinations with wide-enough FOV scanners
	(ie, Multi, RADAR, but not SAR) will no longer reveal the opposite polar region. [thanks DMagic!]
	[example orbit]:  Minmus 250.000x250.000km @ 91.0 degrees


Internal (Developer) Changes:

The following changes are only important for SCANsat or other module developers:

+ [dev] version number hardcoded, now "1.0.6.0" until version 6.1 ("1.0.6.1") or 7.0 ("1.0.7.0"). [tg]
+ [dev] Support for changing what you define as 'sea level'. Compiled to be default (ie, 0). [tg]
- [dev] Removed all unused declerations [tg]
+ [dev] Abstracted some lat/long functions to lambdas. [tg]
+ [dev]	switched to Windows line endings. [tg]
- [dev] removed windows commands in .csproj and .sln. [tg]
+ [dev] added unix commands. [tg]
+ [dev] added debugging and profiling support. Includes dummy executable. [tg]
+ [dev] [requires] that the SCANsat.dll be put directly in place (ie, GameData\SCANsat\.)
			for debugging to work [tg]


Build 5 - 2013-12-18
--------------------

- Update for KSP version 0.23. This version of SCANsat now requires KSP 0.23.

- Fixes for minor incompatibilities with Unity 4.2.2.

- Fixes for science changes in 0.23. Unfortunately, it's still necessary
  to analyze multiple times to get the full amount of science.

- Removed the deprecated slope scanner part from the distribution. This breaks
  savefile compatibility for savefiles that still use this part. If this is
  an issue, simply keep the "Scanner 4" directory from the existing SCANsat
  installation.

- Replaced models for altimetry and biome sensors with much better ones made 
  by Milkshakefiend. Original forum thread: 
  http://forum.kerbalspaceprogram.com/threads/49233-WIP-Parts-from-my-garden-shed-SCANsat-Antennae

  Note: This breaks savegame compatibility in the sense that affected parts
  look different and may be attached badly. If this is an issue, simply don't
  overwrite the existing parts when updating, or copy them back after the
  update.

- Parts with animations can be extended and retracted in the editor.
  This doesn't affect their state at launch.
   
- The big map should not open outside the screen anymore. (SirJodelstein)

- Added a button to reset window positions to settings panel. (Lalwcat)

- Fixed a bug that could sometimes prevent power consumption from being 
  turned off during high time warp.

- Biome maps now use the proper API function to determine the biome at a 
  given location, which has been fixed in 0.23 to not spam debug output
  anymore.

- Action names are configurable in the part config file. Included parts
  have been updated. (OrtwinS)

- The instruments window got its own close button. (DMagic)



Build 4 - 2013-11-10
--------------------

- Ground track should not fail anymore on certain escape trajectories.

- The SAR sensor has been replaced with a multispectral sensor, and the
  high resolution altimetry sensor has been replaced with a SAR sensor.
  (Naming change that doesn't affect gameplay.)

- All sensors had their minimum, maximum, and optimal altitudes as well
  as their fields of view adjusted slightly. These parameters are now
  configurable in the part.cfg files.
  
- A sensor's field of view degrades linearly below the optimal altitude, 
  and remains constant between optimal and maximum altitude. Parameters
  are scaled based on planet radius and SoI size, so you can still map
  Gilly.
  
- The sensor changes affect the save format. Existing saves should still
  work, but sensors will use a default set of parameters until the vessels
  they're on become active for the first time after the update.
 
- To balance the now greater need to set up a reasonable orbit for mapping,
  the temporal resolution of the scanning process is now adaptive and 
  will increase during time warp to reduce spottiness of the resulting
  tracks.

- Turning off sensors no longer closes the SCANsat UI, but the small map
  and vessel list as well as map overlays are only displayed when some 
  device on board can provide that information.
  
- If you run out of power, the minimap shows static and a lot less
  information is displayed in most windows. (OrtwinS)

- Flags on the big map have their own toggle button now. (Thourion)

- The minimap shows the area covered by the active sensors on your current
  vessel as a percentage.
 
- The "Forget Map" button has been relocated to a new settings window.
  A new button "Reset all data" is also available that resets all data
  for all celestials.
 
- The anomaly marker and the close widget character are now configurable
  in the new settings window. (drtedastro)

- Scanning can be restricted to the SoI your current vessel is in with the
  "Scan all active celestials" option in the new settings window. 
  
- Scanning can also be disabled for individual celestials. For your
  convenience, completion is shown as a percentage for each celestial.
  (Includes Altimetry, Biome, and Anomaly scans.)
  
- The temporal resolution of scanning while time warp is active can be
  configured in the new settings window. Higher resolution means more
  locations along a satellite's orbit are sampled. 
  
- Some sensors had experimental instrument readouts added that can be
  enabled via a button. Vessels equipped with a MapTraq device can also
  access some of this information if it has previously been recorded.
  The BTDT's instrument display reacts to the scroll wheel, although 
  that's only really relevant at the KSC...

- If a part has an animation set via the animationName property, the
  plugin will attempt to play it for activation and deactivation of the
  part. 

- Dragging the minimized main window doesn't maximize it anymore. (Thourion)

- If the main window is minimized or maximized close to the edge of the 
  screen, it will try to stay at that edge. (Thourion)

- A colour legend is available for height maps. (OrtwinS)

- Parts have been moved around in the tech tree a bit.

- Science data can now be collected from parts for applicable fields, based
  on how much terrain of the planet the vessel is currently in orbit around 
  has been covered. This science data must be transmitted using a suitable
  communications device like other science experiments; however, recovery is
  not possible at this time. (OrtinS, Draft, BananaDealer)



Build 3 - 2013-11-02
--------------------

- Map overlays are rendered in a more efficient way. (DMagic, OrtwinS)

- The polar orthographic projection doesn't choke on the antimeridian 
  anymore. (DMagic)

- Greyscale mode changes more colors to barrier free colors.

- The big map is now resizable and its size and position are saved in the 
  persistance file.

- Map markers now use the stock KSP icons, if applicable. (Sochin)

- More text has black outlines now to make it more readable.

- Map markers and the ground track are now visible in the zoom box. (Ortwins)

- Polar projection now displays handy S and N markers. (Thourion, OrtwinS)

- All parts now consume electrical charge. The amounts are not finalized yet
  but can be configured in the part.cfg files.

- Slope detection has been folded into altimetry. (OrtwinS)

- Existing slope sensors have become broken, and a small explosive charge has
  been activated remotely for your convenience. You can trigger it via the
  right-click menu to get rid of the part on existing vessels.

- If there are maneuver nodes on the active orbit during the time span for 
  which the ground track is rendered, the first maneuver node's position will
  be indicated on the ground track along with the ground track for one period
  of the resulting orbit.

- The GUI style should not flip between Unity and KSP style. (BananaDealer)

- Rendered maps are not exported automatically anymore. Instead, there's a
  button in the big map window that exports the current map without any of
  the overlays. Exported images are still saved in the same location. (MOARdV)

- Flags are now visible on the map.

- Double clicking a ship name in the small window now switches to that ship.

- The vessel list in the small window can now be toggled, and the window can
  be minimized to an icon only. (OrtwinS)
  
  

Build 2 - 2013-10-31
--------------------

- BTDT scanner now correctly works when the vessel is below 2000m above ground,
  instead of when the anomaly is below 2000m above sea level. (DMagic)

- Main UI now correctly shows the altitude of the vessel above ground, instead 
  of the terrain elevation above sea level below the vessel.
  
- Big map mouseover text now displays latitude and longitude in the correct 
  order. (DMagic)
  
- Geographical coordinates are now displayed in DMS. (Sochin, GhostChaser)

- Left-clicking inside the zoom box now zooms back out. (OrtwinS)

- Big map now shows a projection of the active vessel's current orbit from one
  orbital period in the past to one orbital period in the future. (Sochin)
  
- Big map now shows predicted equatorial crossings for the next few dozen orbits.

- Big map can now be rendered using the Kavrayskiy VII projection. (OrtwinS)

- Big map can now be rendered using a Polar Orthographic projection. (OrtwinS)

- A red scanning line indicates big map rendering progress. (Thourion)

- When using greyscale, text labels are displayed in cyan and orange. (sharpspoonful)

- Text labels on maps now have a black outline. (OrtwinS)

- The UI doesn't go AWOL anymore if you switch vessels in map view. (Tutman, DMagic)

- The small map doesn't paint elevations <-1500m red in greyscale mode anymore.

- Areas not covered by all active sensors on your active vessel now appear
  darker on the small map. 

- There's a switchable dot line grid on the big map. (OrtwinS)

- Map markers can be toggled in big map. (DMagic, OrtwinS)

- The zoom box acquired a close button. (Ralathon, DMagic, OrtwinS)
