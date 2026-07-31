### [**SCANsat**][top]: Real Scanning, Real Science, Warp Speed!
[![][shield:support-ksp]][KSP:developers]&nbsp;
[![][shield:ckan]][CKAN:org]&nbsp;
[![][shield:license-bsd]][SCANsat:rel-license]&nbsp;
[![][shield:license-mit]][SCANsat:dev-license]&nbsp;
[![][shield:license-cc-by-sa]][SCANsat:dev-license]&nbsp;
> ![scan your planetoid like the big boys do][bigmap-scan-10000x]
> ###### **Example SAR scan of Kerbin at 1000x and then 10,000x warp**

[![][shield:support-ksptl]][ksptl:release]&nbsp;
[![][shield:support-rpm]][RPM:release]&nbsp;
[![][shield:support-mm]][mm:release]&nbsp;
[![][shield:support-toolbar]][toolbar:release]&nbsp;
[![][shield:support-karbonite]][karbonite:release]&nbsp;
[![][shield:support-usi]][usi:release]&nbsp;
[![][shield:support-kopernicus]][kopernicus:release]&nbsp;
[![][shield:support-ccfg]][cconfig:release]&nbsp;
[![][shield:support-mechjeb]][mechjeb:release]&nbsp;

**Table of Contents**
------------------------------------------

* [0. People, Facts, and FAQs][0]
  * [a. FAQs][0a]
  * [b. Video Overview][0b]
* [1. Installation and Interoperability][1]
  * [a. Installation][1a]
  * [b. GameData Layout][1b]
  * [c. Texture Configurations][1c]
  * [d. Other Add-Ons][1d]
* [2. Types of Scans][2]
  * [a. Native SCANsat][2a]
* [3. Resource Scanning][3]
  * [a. Stock Scanning Mode][3a]
  * [b. SCANsat Scanning Mode][3b]
  * [c. Resource Settings][3c]
* [4. Basic Usage][4]
  * [a. FAQ: Finding a Good Altitude][4a]
  * [b. Mismatched Scanners][4b]
  * [c. Biome Map][4c]
  * [d. Ground Track Indicators][4d]
* [5. Big Map][5]
  * [a. Big Map Options][5a]
* [6. Planetary Overlay][6]
  * [a. Terrain and Biome Overlays][6a]
  * [b. Overlay Control Window][6b]
* [7. Zoom Map][7]
  * [a. Hi Def Map][7a]
  * [b. Target Selection][7b]
  * [c. MechJeb Landing Guidance][7c]
* [8. Instrument Window][8]
* [9. Raster Prop Monitor][9]
* [10. Parts and Sensors Types][10]
  * [a. RADAR][10a]
  * [b. SAR][10b]
  * [c. Multi][10c]
  * [d. BTDT][10d]
  * [e. MapTraq (deprecated)][10e]
* [11. (Career Mode) Research and Development][11]
  * [a. Minimum Scan for Science (30%)][11a]
  * [b. Getting Maximum Science][11b]
  * [c. Contracts][11c]
* [12. Color Management][12]
  * [a. Terrain Colors and Options][12a]
  * [b. Biome Colors and Options][12b]
  * [c. Resource Colors and Options][12c]
* [13. Background Scanning][13]
* [14. Time Warp][14]
* [15. Settings Menu][15]
* [16. Note: Data Sources][16]

**WARNING**:

This add-on is a **work**-in-**progress**.

This means you should expect that it **may not work**, and you should be unsurprised if it **does not progress**.

Disclaimer aside, this add-on is widely used and it **usually** works just fine.

### [:top:][top] 0. People, Facts, and FAQs
------------------------------------------

#### Maintainers 
The current maintainer is:
  + [DMagic][DMagic] \<<david.grandy@gmail.com>\>

Maintainers are the people who you should complain to if there is something wrong.

Complaints in various forms are prioritized as follows:

  1. [Pull Requests][SCANsat:pulls] are given the **highest** priority possible. ~ 24 hour response
  2. [Issues][SCANsat:issues] are given *higher* priority than other complaints. ~ 2 day response
  3. [E-Mails][SCANsat:email] will be answered as soon as possible (it's a forwarded list) ~ 3 day response
  4. [Forum Posts][SCANsat:rel-thread] are given a medium priority. ~ 1 week response
  5. [Forum Private Messages](http://forum.kerbalspaceprogram.com/private.php) are given a low priority. We might forget!
  6. [Reddit Posts and PMs][KSP:reddit] are the lowest priority. We often lurk and don't login!

If you submit a well-reasoned pull request, you may even trigger a new release!

#### Authors
The current authors include:
  + [technogeeky][technogeeky] \<<technogeeky@gmail.com>\>
  + [DMagic][DMagic] \<<david.grandy@gmail.com>\>

Past authors include:
  + [damny][damny] \<<missing-in-action@nowhere-to-be-found.com>\>

As of August 2014, the vast majority of code is damny's and DMagic's; and technogeeky and is slowly helping out here and there.

#### Contributors

In addition to the authors, the following people have contributed:
  + (Models, Graphics, Textures) [Milkshakefiend][Milkshakefiend]
  
  + (Science results text) [madsailor][madsailor]
  
  + (Contracts) [severedsolo][severedsolo]

#### Licenses

For licensing information, please see the [included LICENSE.txt][SCANsat:rel-license] file.

[Source Code][SCANsat:rel-source] is available, as some licenses may require.

### [:top:][top] 0a. FAQs

  * For some basic scanning strategies, as well as a description of resource scanning, check out the [SCANsat wiki][SCANsat:wiki]

  * What does SCANsat do?
    * It allows you to scan planetary bodies for terrain, biome, and resource information and generate various kinds of maps.
  * How does SCANsat affect gameplay?
    * It allows you to see surface details from orbit from an interactive, zoom-able map. This will help you plan your missions (for example, landing near a divider between two or three biomes) and provide critical information you need to attempt a safe landing (for instance, the slope map will help you avoid treacherous hills)
  * Will this version break my existing scans from older versions of SCANsat?
    * **No!** This version is completely backwards compatible, and you current scanning state (which is stored in persistent.sfs) will be safe and sound. Nevertheless, you should make a backup copy of your game before upgading any mod.
  * Do I need to attach a part to my vessel to use SCANsat?
    * **No, but...**. You can view existing maps from any vessel, but you need to attach a scanner to add new data to the maps.
  * What does the "field of view" mean?
    * When a sensor is at or above its "best" altitude (but below its maximum altitude) the field of view is half of the width of the swath mapped by the instrument, if it were in orbit around Kerbin. In other words, a field of view of 5° would map swathes which are 1/36th (10°) of the planetary surface wide. The field of view is scaled for smaller bodies so that more of the surface is covered.
  * What does the "best" altitude mean?
    * At or above the best altitude, the sensor will operate with its listed field of view. Below this altitude the sensor suffers a linear penalty. A 10° FOV instrument with a best altitude of 500km would only have a 5° FOV at 250km.
  * [Career Mode] Does SCANsat give us science points?
    * **Yes!** For each type of map, if you scan at least 30% of the surface, you can transmit that Data for partial science points; up until the maximum value at 95% map coverage.
  * [Career Mode] Is it integrated into the tech tree?
    * **Yes!** **[This link][8]** tells you which nodes unlock which parts in the tech tree.
  * [Contracts] Does SCANsat offer contracts to complete?
    * **Yes/No.** Contracts are currently only supported through [**third-party addons**][6d].
  * Can you add <some feature or change> to SCANsat?
    * **Probably!** First, check the issues page to see if it's already been requested. If not, add a new issue. Even better, attempt to add the feature yourself and submit a pull request. We'll catch the bugs for you!

### [:top:][top] 0b. Video Overview

##### SCANsat overview and review by: [TinyPirate][tinypirate]
   * Watch this quick video on the features and functions of SCANsat
> [![][tinypirate-video-screen]][tinypirate-video]



### [:top:][top] 1. Installation and Interoperability 
------------------------------------------

#### [:top:][top] 1a. Installation 

  1. Download the latest [SCANsat package][SCANsat:rel-dist-github] from the releases section of this GitHub Repo
  2. Extract the included package and put the SCANsat folder in your KSP installation's GameData folder.

#### [:top:][top] 1b. GameData Layout 

![][SCANsat:gamedata]

#### [:top:][top] 1c. Texture Configurations

S.C.A.N. was originally designed to load all visual and normal textures into RAM directly from the game, as this doesn't require any additional configuration steps for planet packs to integrate directly with the mod. This, however, has led to performance issues with large scale textures and planet packs (notoriously Real Solar System scale packs like Sol).

To get around this, support has been added for memory mapping external textures through KSPTextureLoader. This works by defining the relative path to the height, normal and color textures for each planet, and defining them in a config file as shown below (recommend making one for each body titled "BodyName_SCANsat.cfg"): 

```
SCANSAT_BODY_TEXTURES
{
    name = Mercury  // Use name of body in Kopernicus
    heightMap = GameData/Sol-Textures/PluginData/01_Mercury/Kopernicus/Mercury_Height.dds
    normalMap = GameData/Sol-Textures/PluginData/01_Mercury/Kopernicus/Mercury_Normal.dds
    colorMap  = GameData/Sol-Textures/PluginData/01_Mercury/Kopernicus/Mercury_Color.dds
}
```

With these paths defined, the CPU is able to pull data directly out of the texture from the file system without loading it into RAM. Planet pack makers are encouraged to use this new interface with their packs as this directly specifies the textures SCANsat will display in game.

#### [:top:][top] 1d. Other Add-Ons 

S.C.A.N. is proud to collaborate with other KSP mods and modding teams. Following is a table of all of the mods, add-ons, or software that we interoperate with.

**Built Using** | **Supported By**
:---: | :---:
[**MechJeb**][mechjeb:release] | [![Support for MKS][usi:logo]][usi:release]
[**Blizzy78's Toolbar**][toolbar:release] | [![Support for Karbonite][karbonite:logo]][karbonite:release]
[**ModuleManager**][mm:release]  | [![Support for ALCOR][alcor:logo]][alcor:release]
[**RasterPropMonitor**][rpm:release]  | 
[**KSPTextureLoader**][ksptl:release]  | 

 **MM**, **RPM**, **MechJeb** and **Toolbar** are all **soft** dependencies. This means your experience with SCANsat will be enhanced if you are using these mods, but they are not necessary.

**KSPTextureLoader** is a built dependency, however, as all planets that have external texture files are strongly recommended to use memory mapped textures, with RAM cached variants only being a fallback. This ensures that no errors occur with shader configs and that the exact texture desired ends up displayed in game.

**SCANsat** is built against the following mods:
  * [x] [![][shield:support-ksptl]][ksptl:release]
  * [x] [![][shield:support-mm]][mm:release]
  * [x] [![][shield:support-rpm]][rpm:release]
  * [x] [![][shield:support-alcor]][alcor:release]
  * [x] [![][shield:support-toolbar]][toolbar:release] 
  * [x] [![][shield:support-mechjeb]][mechjeb:release]


### [:top:][top] 2. Types of Scans 
------------------------------------------

SCANsat supports several different kinds of scans (as opposed to
scanning modules or parts).

  * **RadarLo**: Basic, Low-Resolution RADAR Altimetry (b&w, limited zoom)
  * **RadarHi**: Advanced, High-Resolution RADAR Altimetry (in color, unlimited zoom)
  * **Slope**: Slope Data converted from RADAR data
  * **Biome**: Biome Detection and Classification (in color, unlimited zoom)
  * **VisualLo**: Basic, Low-Resolution Visual Imaging (b&w, limited zoom)
  * **VisualHi**: Full Color, High Resolution Visual Imaging
  * **Anomaly**: Anomaly Detection and Labeling
  * **Resource**: Scan for chemical or mineral resource on the surface.

Other parts and add-ons are free to include one or more of these kinds of scans. In general,
we would request that similar (same order of magnitude) scanning parameters and limitations are used
on custom parts, but this is not a requirement.

#### [:top:][top] 2a. SCANsat Scans

**SCANsat** can scan for a few basic types of data. All of these (non-resource) scans are shown as indicators on the Small Map. For more information about basic SCANsat scanning checkout the [wiki][SCANsat:wiki]

> ![][small-newMap1]

Data Type | Scan Type | Scan Indicator
:--- | :--- | :---:
Altimetry | **RADAR** | **LO**
Altimetry | **SAR** | **HI**
Biome | **Biome** | **MULTI**
Anomaly | **Anomaly** | **MULTI**
Anomaly | **Been There, Done That(tm)** | **BTDT**

* The **Slope** map is generated from either **HI** or **LO** data.
* The **Biome** scan only works for bodies that have biomes. For vanilla KSP, this means all planets except the sun and Jool.
* **Anomalies** are things the builds of KSC, hidden easter eggs, etc...
* The **Anomaly** data scans for anomalies from orbit, while
* **BTDT** shows a camera view of an anomaly once you are near it
* The **Biome** and **Anomaly** scans are combined into the multi-spectral scanner; indicated by **MULTI**
   
### [:top:][top] 3. Resource Scanning
------------------------------------------

**SCANsat** will scan celestial bodies for resources using the new stock resource system. For a full description of both stack and SCANsat resource scanning methods check out the [SCANsat wiki][SCANsat:wiki].

#### [:top:][top] 3a. Stock Scanning Mode

With default resource scanning options enabled the SCANsat resource map will automatically update as soon as a **stock resource scan** is completed.
> ![][resource-instant]

No further resource scanning is necessary while using stock scanning mode.

#### [:top:][top] 3b. SCANsat Scanning Mode

There are two levels of **SCANsat Scanning Mode**:
* Disable **Instant Scanning** in the **Resource Settings Window**
  * This will prevent SCANsat maps from being updated based on stock scanning coverage
  * All stock scanning parts will continue to work as normal
  * SCANsat resource scanning essentially works alongside stock scanning in this mode
* Disable **Stock Scanning** in the **Resource Settings Window**
  * This will prevent all stock scanning functions
  * All stock scanning modules will use replacement SCANsat modules
  * These include:
    * The planetary resource overlays (these are available in all scanning modes)
    * The resource abundance readouts for the right-click menu
	* The fixed-position resource map found on the Narrow-Band scanner
  * This **does not** affect the stock biome scanning mechanic; resource abundance accuracy is still affected by biome scanning status, the same as with stock resource scanning

Resource scans are initiated in the same way as any other scan. In this case they use the stock **M700 Orbital Survey Scanner**.
> ![][resource-scanner]

Resource scanning proceeds the same way as standard SCANsat scanning instruments do. The grey scale color option generally works best when viewing resource overlays.
> ![][resource-bigmap]

The resource system can be enabled through the **SCANsat** Big Map:
> ![][resource-walkthrough]

The **M700 Orbital Survey Scanner** provides a low-detail resource map for all available surface resources. For high detail maps a specific scanner is needed for each different resource. For stock Ore the **M4435 Narrow-Band Scanner** is used; high detail scanner modules are added to other parts for addon resources as needed.

The examples below show the difference between high (top) and low (bottom) resolution resource scans
> ![][resource-bigmap-hires-full-biome]

> ![][resource-bigmap-lores-full-biome]

Zoom map resource overlays require that a vessel with a narrow-band scanner be present in orbit and at an inclination high enough to cover the area in the zoom map.
> ![][resource-zoom-map-covered]

If a vessel with a narrow-band scanner is not present, or its inclination is not high enough, the zoom map will not display the resource overlay. The same restrictions apply to **overlay tooltips** and the **instrument window resource readout**.
> ![][resource-zoom-map-uncovered]

Resource overlays will work in IVA, too:
> ![][resource-iva]

#### [:top:][top] 3c. Resource Settings

A number of options are available in the **Resource Settings Menu** for SCANsat resource scanning; this window has an in-game help function activated by selecting the **?** icon in the top-right corner.

> ![][resource-settings]

* **Instant Resource Scan**
   * When this option is active all resources will be fully scanned for a given planet using the stock **Orbital Survey Scanner** instrument
   * When disabled the SCANsat resource overlays will need to be generated using the method described above
   * When **Disable Stock Scanning** is activated the **Instant Scan** option will automatically turn off
* **Resource Biome Lock**
   * With this option active, biomes will need to be scanned from the surface to obtain accurate resource abundance reading on SCANsat maps
   * When disabled all resource abundance values will be fully accurate, with no need for ground surveys
* **Requires Narrow Band Scanner**
   * With this active the zoom map will only display resource overlays when a suitable **Narrow-Band Scanner** is in orbit around the planet, and its orbit covers the region showed in the zoom map
   * **Instruments Window** resource readout will require a narrow-band scanner on the current vessel within its maximum altitude limits
   * **Planetary Overlay Tooltips** will check for any suitable vessel in orbit around the planet before showing resource abundance
   * When disabled the zoom map, instruments window, and overlay tooltips will display resource values regardless of Narrow-Band scanner status
* **Disable Stock Scanning** 
   * Disables the stock orbital survey
   * Replaces the resource concentration readout from scanners with SCANsat modules
   * Right-click menu resource concentration readout dependent on SCANsat scanning coverage
* **Stock Scan Threshold**
   * Sets a threshold value for SCANsat M700 resource scans when stock scanning is disabled; can be disabled
   * Once this value is reached a default, stock resource scan for that planet will be conducted
   * Enter a new value, from 0-100 in the text box
* **Reset SCANsat Resource Coverage**
   * This button will erase all resource scanning data for the current planet
   * Regular SCANsat data will not be affected
   * A confirmation window will appear upon clicking the button
* **Reset Stock Resource Scanning**
   * Stock resource scanning data can be erased for each planet (does not affect resource biome scanning data)   
* **SCAN Planet Overlay Quality Settings**
   * **Interpolation** settings can be increased or decreased to increase the accuracy of overlays
   * **Overlay Map Size** can be adjusted for higher quality, but slower maps
   * **Coverage Transparency** adjusts the transparency of a grey overlay for areas that have been scanned but contain no resources, this makes it easier to track resource scanning progress
   
### [:top:][top] 4. Basic Usage
------------------------------------------

Put scanner part on rocket, aim rocket at sky, launch. If your rocket is not pointing at the sky, you are probably not going to map today, because most sensors only work above 5 km.

You can start scanning by selecting a SCANsat part's context menu, enabling the part. Here, you will find a **small map**.

#### [:top:][top] 4a. FAQ: Finding a Good Altitude

Watch the data indicators on the small map to determine how well your scanners are performing. The right-click context menus also contain indicators for the proper scanning altitude.

###### too high
Solid ORANGE means you're too high (and therefore no data is being recorded):
> ![][small-toohigh-v10]

###### too low
Flashing ORANGE/GREEN means you're too low (and therefore you have a FOV penalty):
> ![][small-toolow-v10]

###### just right
Solid GREEN means you're in an ideal orbit. Notice the larger swath width on the right:
> ![][small-justright-v10]

#### [:top:][top] 4b. Mismatched Scanners

In these examples, the SAR and Multi sensors are not very well matched. Because the SAR sensors is ideal above 750km, and becuase it has a large field of view penalty if it's down near the ideal for Multi (250km), these sensors probably should not be used on the same scanner.

BIO and ANOM are ideal, but HI is not! | HI is ideal, but BIO and ANOM are off!
---|---
![][small-mismatch1] | ![][small-mismatch2]

SAR (HI) has thin swaths due to low alt. | Multi and RADAR have similar ideal swaths
--- | ---
![][small-scan-color] | ![][small-scan-bw]

The mapping interface consists of a small-ish map of the planet, as far as it has been scanned in your current game. It scans and updates quickly and shows positions of the active vessel, as well as other scanning vessels in orbit around the same planet. Orbital information is also provided. For a slower but more detailed view, see the **[big map][4]**.

Note that the indicators flash blue when the gray-scale color option is selected on the big map.

Be sure to remember to pack enough batteries, radioisotope generators, and solar panels. If you forget, you'll run out of electricity, you'll stop recording data, and you'll see useless static:

###### Static! Oh no, adjust the rabbit ears!
> ![][small-static]

#### [:top:][top] 4c. Biome Map
> ![][small-biome]

The **Biome/Terrain** button in the top right corner can be used to toggle between the standard terrain map and a biome map
* When in biome mode, the current biome of each vessel will be displayed if it has been scanned

#### [:top:][top] 4d. Ground Track Indicators
> ![][ground-track]

When activated in the settings menu, ground track indicators are drawn for vessels around the current planet. 
* The indicators are drawn only if at least one sensor on board the vessel is within scanning altitude
* The indicator width on the surface represents the scanning FOV at the equator
* The scanner with the widest FOV on a vessel is used to create the indicator 

### [:top:][top] 5. Big Map
------------------------------------------

> ![A Big Big Map][bigmap-anim-v2]

A bigger map can be rendered on demand. Rendered maps are automatically
saved to GameData/SCANsat/PluginData. Note that position indicators for
vessels or anomalies are not visible on exported images (but they may be a future release).

You can mouse over the big map to see what sensors have data for the location, as well as terrain elevation, and other details.

Right-clicking on the big map shows a magnified view around the position where you clicked. Mouse operations work inside this magnified view just like they work outside, meaning the data displayed at the bottom window applies to your position inside the magnified view, and right-clicking inside it will increase magnification. This can be useful to find landing spots which won't kill your kerbals.

#### [:top:][top] 5a. Big Map Options

There are four drop-down menus along the top of the big map. These control, from left to right: 

* The map projection type - Rectangular, KavrayskiyVII:, or Polar
* The map type - Altimetry, Slope, or Biome
* The resource to overlay on the map
* The planet to display

The icon in the center of the upper row regenerates the map.

The toggle icons along the left side of the map control the various overlays and the color mode.

The four buttons in the bottom-left open and close the other SCANsat windows.

The camera icon in the lower-right exports a copy of the map.

The re-size icon in the lower-right corner can be dragged to re-size the map.

### [:top:][top] 6. Planetary Overlay
------------------------------------------

> ![][resource-planet-hires-full-biome]

A separate window is used to control planetary overlay maps. These maps are drawn directly over the surface of the current planet; they use the same system as the stock resource overlays and will replace those maps if both are opened at the same time. Map coverage is dependent upon SCANsat data; only scanned areas will be displayed on the map.
> ![][resource-planet-in-progress]

#### [:top:][top] 6a. Terrain and Biome Overlays
> ![][terrain-planet-overlay]

SCANsat terrain maps can be displayed with the overlay window. These maps use the same height map data as the small SCANsat map and generate very quickly.

> ![][terrain-planet-overlay-tooltip]

Tooltips can be activated to show data for the location under the current mouse position while in map mode.

SCANsat biome coverage can also be displayed using the overlay window.
> ![][biome-planet-overlay]

#### [:top:][top] 6b. Overlay Control Window
> ![][resource-overlay-window]

Each available resource can be selected for the planetary overlay maps, along with biome maps and terrain maps. Biome maps are currently limited to the stock color scheme without borders.

### [:top:][top] 7. Zoom Map
------------------------------------------

> ![][bigmap-zoom-open]

A separate, small map can be opened from the big map by right-clicking somewhere within the big map. This new window will be centered on the mouse cursor's location and zoomed in by a factor of 10. Icons on the zoom map can be used to zoom in or out, to a minimum of 2X zoom. The terrain coloring limits will are modified to use the local minimum and maximum terrain height within the zoom map's field of view; the full color spectrum will always be used in zoom maps.

The zoom scale and map center can be controlled by clicking within the zoom map.
* Left-click to zoom out and re-center the map at the mouse cursor.
* Right-click to zoom in and re-center the map at the mouse cursor.
* Middle-click or Modifier Key (usually Alt on Windows) + Right-click will re-center the map without changing the scale.

The vessel orbit overlay, waypoint icons, and anomaly locations can be toggle on and off independently of the big map settings.

The **zoom map** also features mouse-over information for the cursor location similar to that shown on the big map.

Different map types, resource overlays and polar projections are all applied to the **zoom map** as well.
> ![][zoommap-in]

#### [:top:][top] 7a. Hi Def Map
> ![][hidef-map-open]

Parts with the stock ModuleHiDefCamera (i.e. the Narrow-Band Scanner) have a special SCANsat module that opens a modified zoom map.
* This map features a low detail, grey-scale terrain map
* It is fixed on the vessel's current position; re-syncing the map will re-center it on your current position, and clicking within the map will only change the zoom level, not the map center location
* Zoom levels are restricted; the default is from 8X to 20X zoom
* All available resources can be viewed using this map and selected using the **Resource Drop Down Menu**

> ![][hidef-map-comp-1]

The **Hi Def** map shows all standard SCANsat map overlays

> ![][hidef-map-comp-2]

The **Hi Def** map is limited to show only low quality terrain maps that are not dependant on SCANsat coverage

#### [:top:][top] 7b. Target Selection
> ![][zoommap-scansat-landing]

The **zoom map** features an option to select and display a target site for each planet. Toggle **Target Selection Mode** by clicking on the target icon in the upper left, then select a sight 
in the zoom map window. The icon will be displayed, along with standard, FinePrint waypoints, in the zoom window and the big map. 

While in map view the target site will be overlayed on the planet's surface; shown as a matching, four-arrow green icon.

To clear an existing target, activate **Target Selection Mode** by clicking the target icon, then click somewhere inside of the zoom map window, but outside of the map itself.
> ![][zoommap-clear-target]

#### [:top:][top] 7c. MechJeb Landing Guidance
> ![][zoommap-mechjeb-settings]

If MechJeb is installed and an additional option is available in the settings menu to activate **MechJeb Landing Guidance Mode**

> ![][zoommap-mechjeb-landing]

The **zoom map** can be used in the same way described above to select a landing site for **MechJeb's Landing Guidance** module. The current vessel must have a MechJeb core
and the MechJeb Landing Guidance module must be unlocked in the R&D Center. 

Landing sites selected through MechJeb will automatically show up as a waypoint on SCANsat maps.


### [:top:][top] 8. Instrument Window
------------------------------------------

> ![][instruments-small]

The instruments window provides a readout of several types of data based on current scanning coverage.

* **Location** Shows the vessel's current coordinates; not dependent on scanning coverage
* **Target Dist.** Shows the vessel's distance to the SCANsat or MechJeb landing target; only shown when within 15km
* **Waypoint** Shows if the vessel is inside of a current FinePrint waypoint; not dependent on scanning coverage
* **Altitude** Shows the vessel's current altitude above the terrain; shows the current terrain altitude when landed; shows ocean depth when splashed down
* **Slope** Shows a highly localized slope based on a 3X3 grid centered 5m around the vessel
* **Biome** Shows the biome that the current vessel is in or over
* **Resource** Shows the resource abundance at the current location; when more than one resource is available buttons along the sides allow for switching between these resources
* **Anomaly** Shows the nearest anomaly and its distance from the vessel
* **BTDT Anomaly** Shows detailed information and a crude image about the nearest anomaly; scroll the mouse wheel when positioned over the anomaly window to switch between different structures if more than one is found

> ![][instruments-btdt]

### [:top:][top] 9. Raster Prop Monitor
------------------------------------------

> ![][rpm-multi]

SCANsat includes support for IVA maps through [Raster Prop Monitor][rpm:release]. Standard RPM multi-function displays and those using the [ASET props][alcor:release] are supported.

> ![][rpm-mfd-labeled]

> ![][rpm-ALCOR-left]

> ![][rpm-ALCOR-right]

### [:top:][top] 10. Parts and Sensor Types
------------------------------------------

| **Part** | **Scan Type** | **FOV** | Altitude (**Min**) | (**Ideal**) | (**Max**) 
|:--- | --- | --- | --- | --- | --- |
| [RADAR Altimetry Sensor][5a] | **RadarLo** / **Slope**| 5 | 5000 m | 5000 m | 500 km
| [SAR Altimetry Sensor][5b] | **RadarHi** | 2 | 5000 m | 750 km | 800 km 
| [Multispectral Sensor][5c] | **Biome** **ANOM** | 4 | 5000 m | 250 km | 500 km 
| [Been There Done That®][5d] | **Anomaly** | 1 | 0 m | 0 m | 2 km
| [MapTraq® (deprecated)][5e] | **None** | N/A | N/A | N/A | N/A 

#### [:top:][top] 10a. The RADAR Altimetry Sensor
> ![RADAR][vab-radar]
#### [:top:][top] 10b. The SAR Altimetry Sensor
> ![SAR][vab-sar]
#### [:top:][top] 10c. The Multispectral Sensor
> ![Multi][vab-multi]
#### [:top:][top] 10d. Been There Done That
> ![BTDT][vab-btdt]
#### [:top:][top] 10e. MapTraq (deprecated)
> ![MapTraq][vab-maptraq] 


### [:top:][top] 11. (Career Mode) Research and Development
------------------------------------------

The **RADAR Altimetry** sensor can be unlocked in **Basic Science**.

The **SAR Altimetry** sensor can be unlocked in **Advanced Science Tech**.

The **Multispectral** sensor can be unlocked in **Advanced Exploration**.

The **BTDT** sensor can be unlocked in **Field Science**.

##### [:top:][top] 11a. Minimum Scan for Science
Once you scan at least 30% of a particular map, you can use **Analyze Data** to get delicious science:

> ![30% is your minimum][science-min]

##### [:top:][top] 11b. Getting Maximum Science
Between 30% and 100%, you will get a number of science points proportional to the percentage. Really,
the upper cutoff is 95% in case you didn't scan the whole map.

> ![Scan 95% to get all science][science-max]

##### [:top:][top] 11c. Contract Support
Career mode contracts are supported through third party addons.

* [Contract Configurator Forum Thread][cconfig:release]

SCANsat includes Contract Configurator contracts for scanning each planet.

### [:top:][top] 12. Color Management
------------------------------------------

> ![][color-window]

SCANsat provides multiple options for map color configurations and terrain level changes; this window has an in-game help function activated by selecting the **?** icon in the top-right corner.

The color management window can be accessed from the big or small map with the color palette icon, or from the toolbar menu.

##### [:top:][top] 12a. Terrain Colors and Options
On the left are the various color palettes available; there are four different styles that can be selected from the drop down menu. 
Palettes can be customized by changing the number of colors in the palette, reversing the order, or making the palette use discrete
color transitions, rather than the smooth gradient used by default.

Changes to the color palette are reflected after selecting **Apply**, the big and small maps will automatically refresh using the newly selected color palette.
Note that only the altimetry map is affected by color palette selection.
> ![][color-palette-switch]

There are several terrain height options available as well.
* The **Min** and **Max** height sliders can be used to set the lower and upper cutoff values for the terrain height-to-color algorithm.
* The **Clamp** option can be used to set a cutoff below which only the first two colors in the selected
palette will be used for the terrain height-to-color algorithm. This is especially useful on planets where there is an ocean, as it makes the transition
from ocean to solid terrain more pronounced

> ![][color-clamp-terrain]

All stock KSP planets have default color palette and terrain height values appropriate for the planet's terrain. Standard default values are used
for any addon planets.

##### [:top:][top] 12b. Biome Colors and Options
Biome map colors and options can be controlled in the **Biome** tab of the window
* The end-point colors can be selected using the HSV color-picker; the value slider controls the brightness of the color
* Terrain transparency is controlled with a slider
* Stock style biome maps can be used in place of SCANsat's custom colors
* The white border around biome edges can be disabled

> ![][color-biome]

Biomes can also be displayed using the stock color maps.

> ![][color-biome-stock]

##### [:top:][top] 12c. Resource Colors and Options
Resource overlays can also be adjusted, using the **Resource** tab
* Resource colors are selected in the same manner as biome colors
* Upper and lower resource cutoff values can be adjusted with the sliders; use fine control mode for small adjustments
* Each resource can be adjusted separately and the values can be applied to the current planet or all planets
* Most planets share the same resource value settings; it is easiest to set values for all planets then set the values individually where needed (ie water has a higher value on Kerbin than elsewhere)
* These settings also affect the **Planetary Overlay** colors

> ![][color-resource]

### [:top:][top] 13. Background Scanning
------------------------------------------

![Note the background scanning (non-active vessels are scanning)][small-scan]

Unlike some other KSP scanning systems, SCANsat allows scanning with multiple
vessels.  All online scanners scan at the same time during any scene where time progresses; no active SCANsat
parts are necessary. 

### [:top:][top] 14. Time Warp
------------------------------------------

SCANsat does not interpolate satellite paths during time warp; nevertheless, due to the relatively large field of view
of each sensor, it's still possible to acquire data faster by time warping. The maximum recommended time warp speed
is currently **10,000x**. Scanning at this warp factor should allow identical scanning performance 
(in terms of [swath width](http://en.wikipedia.org/wiki/Swath_width)) as scanning at *1x*.

As an example of speed, here is a BigMap rendering of a scan at **100x**:
> ![this is pretty peaceful][bigmap-scan-100x]

And this is a BigMap rendering of the same orbit, but later in the scan. 
It starts at **1000x** and then speeds up to **10,000x**:
> ![this makes my OCD happy][bigmap-scan-10000x]

Notice that the only gaps in coverage are those at the poles (ie, the selected inclination was not high enough to capture the poles).

### [:top:][top] 15. Settings Menu
------------------------------------------

> ![][settings-window]

The settings menu has a various general options; this window has an in-game help function activated by selecting the **?** icon in the top-right corner.
* The **Resource Settings Window** can be opened
* The marker used for **Anomalies** can be specified
* **Background scanning** can be controlled for each planet
* **Background scanning** resolution can be lowered for better performance (watch for short pauses when several scanners are active at very high timewarp; reducing the scanning resolution can help with this)
* **Ground Track** indicators can be toggled for the current vessel or for all vessels at the current planet
* Toggles control the availability of the **Stock App Launcher** button, and the **Tooltips** for various icons on other windows and **Planetary Overlay Tooltips**
* If MechJeb is installed an additional option is available to toggle the MechJeb Landing Guidance interface
* If the windows are ever dragged off screen there is an option to **Reset All Windows** to their default positions
* **Scanning Data** can be deleted for any or all planets; a confirmation box will appear when these options are selected
* **Time Warp Resolution** affects the scanning frequency of background scanning. Decreasing this value may result in gaps in maps while scanning at high time warp, but may alleviate some performance issues.
* The numbers under the **Time Warp Resolution** indicate the following
    * **Vessels:** The number of vessels with any active SCANsat sensors present
	* **Sensors:** The total number of SCANsat sensors on all vessels; note that all combination sensors are separated into their invidual components, i.e. the Multi-Spectral scanner consists of two sensors, Biomes and Anomalies.
	* **Passes:** The number of scanning passes recorded per second, this number can easily be in the tens of thousands at high time warp with multiple vessels and sensors active.
* There are several options related to map exports
	* **Use Grey Scale** This option uses a true, black-to-white, grey scale for the black and white maps, it affects maps in-game and exports
	* **Set Map Width** This allows for manually setting the size of the big map; values between 560 and 8192 are acceptable
	* **Export .csv File** This allows for a .csv file to be exported along with the map texture; this file includes the pixel row, column, latitude, longitude, terrain height

### [:top:][top] 16. Note Concerning Data Sources
------------------------------------------

All data this mod shows you is pulled from your game as you play. This
includes:
  * terrain elevation
  * biomes
  * anomalies

SCANsat can't guarantee that all anomalies will be found; in particular, some are so close
to others that they don't show up on their own, and if the [developers][KSP:developers] want to be
sneaky then they can of course be sneaky.

------------------------------------------






[technogeeky]: http://forum.kerbalspaceprogram.com/members/110153
[DMagic]: http://forum.kerbalspaceprogram.com/members/59127
[damny]: http://forum.kerbalspaceprogram.com/members/80692
[Milkshakefiend]: http://forum.kerbalspaceprogram.com/members/72507
[Olympic1]: http://forum.kerbalspaceprogram.com/members/81815
[madsailor]: http://forum.kerbalspaceprogram.com/members/123944
[tinypirate]: http://forum.kerbalspaceprogram.com/members/79868
[severedsolo]: http://forum.kerbalspaceprogram.com/members/82445

[KSP:developers]: https://kerbalspaceprogram.com/index.php
[KSP:reddit]: http://www.reddit.com/r/KerbalSpaceProgram

[vab-radar-thumb]: http://i.imgur.com/PrRIcYvs.png 
[vab-sar-thumb]: http://i.imgur.com/4aTTVfWs.png
[vab-multi-thumb]: http://i.imgur.com/byIYXP9s.png
[vab-maptraq-thumb]: http://i.imgur.com/Skrqc8Cs.png
[vab-btdt-thumb]:  http://i.imgur.com/zUmj6USs.png

[vab-radar]: http://i.imgur.com/PrRIcYv.png
[vab-sar]: http://i.imgur.com/4aTTVfW.png
[vab-multi]: http://i.imgur.com/byIYXP9.png
[vab-maptraq]: http://i.imgur.com/Skrqc8C.png
[vab-btdt]:  http://i.imgur.com/zUmj6US.png

[science-min]: http://i.imgur.com/kEj4fz0.gif
[science-max]: http://i.imgur.com/eMtIL5H.gif

[small-scan]: http://i.imgur.com/uVP6Ujs.gif
[small-scan-bw]: http://i.imgur.com/0AbDwKL.gif
[small-scan-color]:  http://i.imgur.com/dlRckBl.gif
[small-static]: http://i.imgur.com/oPN2qIR.gif
[small-nodata]: http://i.imgur.com/0ArIcqj.png

[small-toolow]: https://i.imgur.com/fTDLvw0.gif
[small-toohigh]: https://i.imgur.com/a8YKkXH.gif
[small-justright]: https://i.imgur.com/Oft4xXP.gif
[small-mismatch1]: https://i.imgur.com/fNztoUN.gif
[small-mismatch2]: https://i.imgur.com/aQtTGvV.gif
[small-newMap1]: http://i.imgur.com/qS542xo.gif
[small-toolow-v10]: http://i.imgur.com/USsvSSs.gif
[small-toohigh-v10]: http://i.imgur.com/i7rGDIj.gif
[small-justright-v10]: http://i.imgur.com/y7mHvEF.gif
[small-biome]: http://i.imgur.com/4kZMofq.png

[bigmap-scan-10000x]: http://i.imgur.com/VEPL3oN.gif
[bigmap-scan-100x]: http://i.imgur.com/bcht47p.gif
[bigmap-anim]: http://i.imgur.com/kxyl8xR.gif
[bigmap-anim-v2]: http://i.imgur.com/wUMToq6.gif
[bigmap-zoom-open]: http://i.imgur.com/7egRRTU.gif

[zoommap-in]: http://i.imgur.com/tTCYDfP.gif
[zoommap-scansat-landing]: http://i.imgur.com/ILqRfne.gif
[zoommap-mechjeb-landing]: http://i.imgur.com/nE0BlA8.gif
[zoommap-mechjeb-settings]: http://i.imgur.com/xOQ7ooj.png
[zoommap-clear-target]: http://i.imgur.com/YffxdNs.gif

[resource-iva]: http://i.imgur.com/iRo4kSA.png
[resource-walkthrough]: http://i.imgur.com/KS4FTh0.gif
[resource-scanner]: http://i.imgur.com/mY0fFjr.gif
[resource-bigmap]: http://i.imgur.com/JYKG6f5.gif
[resource-settings]: http://i.imgur.com/hgZFXIO.png
[resource-zoom-map-covered]: http://i.imgur.com/7YuYMGW.png
[resource-zoom-map-uncovered]: http://i.imgur.com/cJ9JtdW.png
[resource-instant]: http://i.imgur.com/mfIMBEP.gif

[resource-planet-hires-limited-biome]: http://i.imgur.com/yhv2bsq.png
[resource-planet-hires-full-biome]: http://i.imgur.com/rwy77M3.png
[resource-planet-lores-limited-biome]: http://i.imgur.com/tR3eTzL.png
[resource-planet-lores-full-biome]: http://i.imgur.com/kPcO2H7.png
[resource-planet-in-progress]: http://i.imgur.com/0JBSFbB.png
[biome-planet-overlay]: http://i.imgur.com/YofnSEI.png
[biome-planet-in-progress]: http://i.imgur.com/AwQfBgq.png
[terrain-planet-overlay]: http://i.imgur.com/unxGS3j.png
[terrain-planet-overlay-tooltip]: http://i.imgur.com/kXsL20Q.png

[resource-bigmap-hires-limited-biome]: http://i.imgur.com/y0jitxK.png
[resource-bigmap-hires-full-biome]: http://i.imgur.com/fCdXTIq.png
[resource-bigmap-lores-limited-biome]: http://i.imgur.com/iHCBfes.png
[resource-bigmap-lores-full-biome]: http://i.imgur.com/TIR1xv5.png

[rpm-multi]: http://i.imgur.com/nXzPtRE.png
[rpm-mfd-labeled]: http://i.imgur.com/hgFnw2y.png
[rpm-ALCOR-left]: http://i.imgur.com/9NxK4d6.png
[rpm-ALCOR-right]: http://i.imgur.com/7GnAdDN.png

[ground-track]: http://i.imgur.com/casTBeW.gif

[resource-overlay-window]: http://i.imgur.com/n68InYq.png

[hidef-map-open]: http://i.imgur.com/xjEDybF.gif
[hidef-map-comp-1]: http://i.imgur.com/ha7ho9b.png
[hidef-map-comp-2]: http://i.imgur.com/DtNmVHi.png

[color-window]: http://i.imgur.com/RQVjq6g.png
[color-palette-switch]: http://i.imgur.com/0XdMGSy.gif
[color-clamp-terrain]: http://i.imgur.com/8dgFLGj.gif
[color-biome]: http://i.imgur.com/NdA1DVY.gif
[color-resource]: http://i.imgur.com/9NR8gvP.gif
[color-biome-stock]: http://i.imgur.com/T14sFzl.png

[instruments-small]: http://i.imgur.com/tpjveyn.gif
[instruments-btdt]: http://i.imgur.com/tybbDap.gif

[settings-window]: http://i.imgur.com/5vqEUhX.png

[tinypirate-video-screen]: http://img.youtube.com/vi/UY7eBuReSYU/0.jpg
[tinypirate-video]: https://www.youtube.com/watch?v=UY7eBuReSYU

[top]: #table-of-contents
[0]: #top-0-people-facts-and-faqs
[0a]: #top-0a-faqs
[0b]: #top-0b-video-overview
[1]: #top-1-installation-and-interoperability
[1a]: #top-1a-installation
[1b]: #top-1b-gamedata-layout
[1c]: #top-1c-texture-configurations
[1d]: #top-1d-other-add-ons
[2]: #top-2-types-of-scans
[2a]: #top-2a-scansat-scans
[3]: #top-3-resource-scanning
[3a]: #top-3a-stock-scanning-mode
[3b]: #top-3b-scansat-scanning-mode
[3c]: #top-3c-resource-settings
[4]: #top-4-basic-usage
[4a]: #top-4a-faq-finding-a-good-altitude
[4b]: #top-4b-mismatched-scanners
[4c]: #top-4c-biome-map
[4d]: #top-4d-ground-track-indicators
[5]: #top-5-big-map
[5a]: #top-5a-big-map-options
[6]: #top-6-planetary-overlay
[6a]: #top-6a-terrain-and-biome-overlays
[6b]: #top-6b-overlay-control-window
[7]: #top-7-zoom-map
[7a]: #top-7a-hi-def-map
[7b]: #top-7b-target-selection
[7c]: #top-7c-mechJeb-landing-guidance
[8]: #top-8-instrument-window
[9]: #top-9-raster-prop-monitor
[10]: #top-10-parts-and-sensor-types
[10a]: #top-10a-the-radar-altimetry-sensor
[10b]: #top-10b-the-sar-altimetry-sensor
[10c]: #top-10c-the-multispectral-sensor
[10d]: #top-10d-been-there-done-that
[10e]: #top-10e-maptraq-deprecated
[11]: #top-11-career-mode-research-and-development
[11a]: #top-11a-minimum-scan-for-science
[11b]: #top-11b-getting-maximum-science
[11c]: #top-11c-contract-support
[12]: #top-12-color-management
[12a]: #top-12a-terrain-colors-and-options
[12b]: #top-12b-biome-colors-and-options
[12c]: #top-12c-resource-colors-and-options
[13]: #top-13-background-scanning
[14]: #top-14-time-warp
[15]: #top-15-settings-menu
[16]: #top-16-note-concerning-data-sources

[shield:license-bsd]: http://img.shields.io/:license-bsd-blue.svg
[shield:license-mit]: http://img.shields.io/:license-mit-a31f34.svg
[shield:license-cc-by-sa]: http://img.shields.io/badge/license-CC%20BY--SA-green.svg
 
[shield:jenkins-dev]: http://img.shields.io/jenkins/s/https/ksp.sarbian.com/jenkins/SCANsat-dev.svg
[shield:jenkins-rel]: http://img.shields.io/jenkins/s/https/ksp.sarbian.com/jenkins/SCANsat-release.svg
[shield:support-ksp]: http://img.shields.io/badge/for%20KSP-v1.12.x-bad455.svg
[shield:support-rpm]: http://img.shields.io/badge/works%20with%20RPM-v0.28.x-a31f34.svg
[shield:support-ksptl]: https://img.shields.io/badge/requires%20KSPTextureLoader-1.0.34+-84e0d7.svg
[shield:support-mm]: http://img.shields.io/badge/works%20with%20MM-v3.x-40b7c0.svg
[shield:support-toolbar]: http://img.shields.io/badge/works%20with%20Blizzy's%20Toolbar-1.7.x-7c69c0.svg
[shield:support-alcor]: http://img.shields.io/badge/works%20with%20ALCOR-0.9-299bc7.svg
[shield:support-kspi]: http://img.shields.io/badge/works%20with%20Interstellar-1.x-a62374.svg
[shield:support-usi]:http://img.shields.io/badge/works%20with%20USI-0.50.x-34c566.svg
[shield:support-karbonite]: http://img.shields.io/badge/works%20with%20Karbonite-0.8.x-ff8c00.svg
[shield:support-epl]: http://img.shields.io/badge/works%20with%20EPL-5.5.x-ff8c00.svg
[shield:support-ccfg]: https://img.shields.io/badge/works%20with%20Contract%20Configurator-1.20.x+-yellowgreen.svg
[shield:ckan]: https://img.shields.io/badge/CKAN-Indexed-brightgreen.svg
[shield:support-mechjeb]: http://img.shields.io/badge/works%20with%20MechJeb-2.5.8-lightgrey.svg
[shield:support-kopernicus]: http://img.shields.io/badge/works%20with%20Kopernicus-1.2.x-ff8c00.svg

[shield:gittip-tg-img]: http://img.shields.io/gittip/technogeeky.png
[shield:gittip-tg]: https://www.gittip.com/technogeeky/
[shield:github-issues]: http://img.shields.io/github/issues/technogeeky/SCANsat.svg

[CKAN:org]: http://ksp-ckan.org/

[SCANsat:organization]: https://github.com/S-C-A-N
[SCANsat:logo]: http://i.imgur.com/GArPFFB.png
[SCANsat:logo-square]: http://i.imgur.com/GArPFFB.png?1
[SCANsat:issues]: https://github.com/S-C-A-N/SCANsat/issues
[SCANsat:pulls]: https://github.com/S-C-A-N/SCANsat/pulls
[SCANsat:imgur-albums]: https://scansat.imgur.com
[SCANsat:best-orbits-table]: https://www.example.com
[SCANsat:email]: mailto:SCANscansat@gmail.com
[SCANsat:gamedata]: http://i.imgur.com/cS1Lu5w.jpg
[SCANsat:wiki]: https://github.com/S-C-A-N/SCANsat/wiki

[SCANsat:dev-readme]: https://github.com/S-C-A-N/SCANsat/tree/dev/#table-of-contents
[SCANsat:rel-readme]: https://github.com/S-C-A-N/SCANsat/#table-of-contents

[SCANsat:rel-thread]: http://forum.kerbalspaceprogram.com/threads/80369
[SCANsat:dev-thread]: http://forum.kerbalspaceprogram.com/threads/96859

[SCANsat:dev-source]: https://github.com/S-C-A-N/SCANsat/tree/dev
[SCANsat:rel-source]: https://github.com/S-C-A-N/SCANsat

[SCANsat:dev-jenkins]: https://ksp.sarbian.com/jenkins/job/SCANsat-dev/
[SCANsat:rel-jenkins]: https://ksp.sarbian.com/jenkins/job/SCANsat-release/
[SCANsat:ket-jenkins]: https://ksp.sarbian.com/jenkins/job/SCANsat-Kethane/
[SCANsat:orsx-jenkins]: https://ksp.sarbian.com/jenkins/job/SCANsat-OpenResourceSystem/

[SCANsat:dev-license]: https://github.com/S-C-A-N/SCANsat/blob/dev/SCANsat/LICENSE.txt
[SCANsat:rel-license]: https://github.com/S-C-A-N/SCANsat/blob/release/SCANsat/LICENSE.txt

[SCANsat:rel-dist-curseforge]: http://kerbal.curseforge.com/ksp-mods/www.example.com-SCANsat
[SCANsat:rel-dist-curseforge-zip]: http://kerbal.curseforge.com/ksp-mods/www.example.com-SCANsat/files/latest

[SCANsat:rel-dist-github]: https://github.com/S-C-A-N/SCANsat/releases
[SCANsat:rel-dist-github-zip]: https://github.com/S-C-A-N/SCANsat/releases/download/www.example.com/SCANsat.zip

[SCANsat:rel-dist-kerbalstuff]: http://beta.kerbalstuff.com/mod/www.example.com/SCANsat
[SCANsat:rel-dist-kerbalstuff-zip]: http://beta.kerbalstuff.com/mod/www.example.com/SCANsat/download/www.example.com

[SCANsat:dev-dist-curseforge]: https://www.example.com
[SCANsat:dev-dist-curseforge-zip]: https://www.example.com

[SCANsat:dev-dist-github]: https://github.com/S-C-A-N/SCANsat/releases/tag/www.example.com
[SCANsat:dev-dist-github-zip]: https://github.com/S-C-A-N/SCANsat/releases/download/www.example.com/SCANsat.zip

[SCANsat:dev-dist-kerbalstuff]: http://beta.kerbalstuff.com/mod/www.example.com/SCANsat
[SCANsat:dev-dist-kerbalstuff-zip]: http://beta.kerbalstuff.com/mod/www.example.com/SCANsat/download/www.example.com

[IAT]: http://forum.kerbalspaceprogram.com/threads/75854
[IAT:kerbin-system]: http://forum.kerbalspaceprogram.com/threads/75854?p=#1
[IAT:inner-systems]: http://forum.kerbalspaceprogram.com/threads/75854?p=#2
[IAT:duna-dres]: http://forum.kerbalspaceprogram.com/threads/75854?p=#3
[IAT:jool-system]: http://forum.kerbalspaceprogram.com/threads/75854?p=#4
[IAT:eeloo]: http://forum.kerbalspaceprogram.com/threads/75854?p=#7
[IAT:earth-system]: http://forum.kerbalspaceprogram.com/threads/75854?p=#9

[karbonite:release]: http://forum.kerbalspaceprogram.com/threads/89401
[karbonite:logo]: http://i.imgur.com/PkewuRD.png

[kethane:release]: http://forum.kerbalspaceprogram.com/threads/23979
[kethane:logo]: http://i.imgur.com/u952LjP.png?1

[usi:release]: http://forum.kerbalspaceprogram.com/threads/79588
[usi:logo]: http://i.imgur.com/aimhLzU.png

[alcor:release]: http://forum.kerbalspaceprogram.com/threads/54925
[alcor:logo]: http://i.imgur.com/7eJ3IFC.jpg

[ctt:logo]: http://i.imgur.com/li2tNgE.png

[mm:release]: http://forum.kerbalspaceprogram.com/threads/55219

[epl:release]: http://forum.kerbalspaceprogram.com/threads/59545

[ctt:release]: http://forum.kerbalspaceprogram.com/threads/100385

[kspi:release]: http://forum.kerbalspaceprogram.com/threads/43839

[toolbar:release]: http://forum.kerbalspaceprogram.com/threads/60863

[rpm:release]: https://github.com/FirstPersonKSP/RasterPropMonitor/releases

[ksptl:release]: https://github.com/Phantomical/KSPTextureLoader/releases

[cconfig:release]: http://forum.kerbalspaceprogram.com/threads/101604

[ccfgSCANsat:release]: http://forum.kerbalspaceprogram.com/threads/108097

[mechjeb:release]: http://forum.kerbalspaceprogram.com/threads/124336

[kopernicus:release]: http://forum.kerbalspaceprogram.com/threads/114649
