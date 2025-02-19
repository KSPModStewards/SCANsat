# Environment Setup

Create a `SCANsat.props.user` file in the root directory using the template below.
Set the `<KSPRoot>` property to the KSP install you wish to develop on.
This file should be excluded via .gitignore and not committed.
This is used by [KSPBuildTools](https://github.com/kspmoddingLibs/kspbuildTools/).

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <KSPRoot>C:\Program Files (x86)\Steam\steamapps\common\KSP Stripped</KSPRoot>
  </PropertyGroup>
</Project>
```

Using CKAN, install MechJeb2 in your development instance (because `SCANmechjeb` depends on it).

# Compiling the mod

You should now be able to open `SCANsat.sln` and build it.
The compiled DLLs should be copied to `GameData/SCANsat/Plugins` automatically.
These should be excluded from .gitignore and never committed.
You can then manually copy this directory to your KSP install, or ideally set up a directory junction in your KSP install to point at this directory.

## Notes on HSVPicker

`SCANsat.Unity` contains code derived from https://github.com/judah4/HSV-Color-Picker-Unity/.
This library or something derived from it seems to also have been added to KSP itself at some point.
This can lead to compilation issues because there are conflicting names depending on the `using` statements in effect.
All of the types from this library in the SCANsat codebase should be in a SCANsat namespace.
Disambiguating `using` statements should be used to make sure that SCANsat code is using the version of the types that are contained in its own assembly:

```cs
using ColorValues = SCANsat.Unity.HSVPicker.Enum.ColorValues;
```

# Unity Project

SCANsat uses several Unity assetbundles which contain shaders, icons, and UI prefabs.
The `SCANsat.Unity` assembly contains the scripts that are embedded in these prefabs.
This assembly is loaded by KSP and is also a dependency in the Unity project.
After compiling the SCANsat mod, copy `SCANsat.Unity.dll` from the `GameData/SCANsat/Plugins` directory to `Unity/SCANsat/Assets/Plugins`.
Then open the project with Unity 2019.4.18f1.
Next, select `Scansat->Build All Bundles`.
This will dump a bunch of files into `Unity/SCANsat/AssetBundles`.
Copy the 4 that have a `.scan` file extension into `GameData/SCANsat/Resources`.

# Future work, bugs, caveats, etc

I do not currently know how to compile the KSPedia bundles, and it looks like there are some missing image files.

The Unity project in general is a bit of a mess.  There are a lot of assetbundles defined but I have no idea what they're for.  They don't seem to be used by the mod.

Still need to set up versioning with KSPBuildTools and the github workflows.