# Publishing SCANsat-GPU to CKAN

This fork ships as its own CKAN mod (`identifier: SCANsat-GPU`) that **provides** and
**conflicts** with `SCANsat`, i.e. a drop-in substitute you install *instead of* stock SCANsat.

CKAN does not host files. You host the download (GitHub Releases), and submit one small metadata
file (`SCANsat-GPU.netkan`) to the NetKAN repo; a bot follows it to your release, reads the KSP-AVC
`SCANsat.version` inside for the version + KSP compatibility, and generates the CKAN entry.

## One-time facts
- License: **BSD-3-clause** (see `LICENSE.txt`; the bundled part models are explicitly cleared for
  redistribution in derivative works with credit).
- Base: SCANsat `dev` (KSPModStewards) merged with Azrail09Code's `simplify_map_drawing`; this fork
  stamps **21.3.0** (`SCANsat.version.props`, imported by `Directory.Build.props`). Bump it there for
  each new fork release.
- Target: KSP **1.12.5** (min 1.12.3, from `SCANsat/SCANsat.csproj`).
- Hard dependency: **KSPTextureLoader** (CKAN identifier `KSPTextureLoader`). The Visual map loads
  `SCANSAT_BODY_TEXTURES` files through it, and the DLL carries a `KSPAssemblyDependency` on it, so
  it is in the netkan's `depends`. ModuleManager stays a dependency (the RSS cfg uses `:NEEDS`).
- The fork identity (`NAME: SCANsat-GPU`, URL, DOWNLOAD) lives in the `KSPVersionFile` item metadata
  in `SCANsat.csproj` and in the tracked `GameData/SCANsat/SCANsat.version` template; it is all in one
  commit so it can be dropped when offering the branch upstream.

## Local build
- KSPBuildTools 1.1.1 finds the KSP install through `KSPBT_GameRoot` and, by default, runs `ckan`
  against that install during the build to fetch dependency mods (CI relies on it). For a local build
  put both in the gitignored `<project>.csproj.user` files (SCANsat, SCANsat.Unity, SCANmechjeb):
  ```xml
  <KSPBT_GameRoot>C:/git/ksp/Instance</KSPBT_GameRoot>
  <KSPBT_InstallCKANDependencies>false</KSPBT_InstallCKANDependencies>
  ```
  The install needs `GameData/KSPTextureLoader` (and `GameData/MechJeb2` for SCANmechjeb).
- `dotnet build SCANsat.slnx -c Release` puts every DLL in `GameData/SCANsat/Plugins`.

## Each release
1. **Build + package:**
   ```powershell
   .\build-release.ps1            # default -KspRoot is ..\Instance; any KSP 1.12.5 install with KSPTextureLoader works
   ```
   Produces `Releases\SCANsat-GPU-21.3.0.0.zip` (GameData/SCANsat/... with the built DLLs, the RSS
   `SCANSAT_BODY_TEXTURES` cfg and the `scan_shaders.scan` bundle) and `Releases\SCANsat.version`.
   - Reminder: if you changed the shader, **rebuild the Unity asset bundle first**
     (Unity 2019.4.18f1 -> `SCANsat -> Build All Bundles`) and commit `scan_shaders.scan`, else the
     zip ships the old bundle.
   - Sanity check: drop the zip's `GameData/` into a clean KSP and confirm it loads
     (`[SCANsat] All SCANsat asset bundles loaded`).
2. **Tag + GitHub Release:**
   ```powershell
   git tag v21.3.0 ; git push origin v21.3.0
   ```
   Create a Release on that tag; **attach both** `SCANsat-GPU-21.3.0.0.zip` and `SCANsat.version`.
3. **Submit to CKAN (first release only; later releases are auto-detected by the bot):**
   - Fork `KSP-CKAN/NetKAN`, add `NetKAN/SCANsat-GPU.netkan` (copy of the one in this repo), open a PR.
   - Before submitting, open the current `SCANsat.netkan` in `KSP-CKAN/CKAN-meta` and copy its exact
     `depends`/`recommends`/`suggests` into yours so the fork matches upstream.
   - Optional: validate locally with `netkan.exe SCANsat-GPU.netkan` (from KSP-CKAN's NetKAN releases).

## Notes
- Maintainers may prefer you upstream the RAM fix to `KSPModStewards/SCANsat` rather than list a
  shadowing fork. As of the fork point, upstream's last commit was 2026-04-29 (light maintenance),
  so a PR there may sit a while.
- Easier alternative to the NetKAN PR: upload the zip to **SpaceDock** and enable its "index on CKAN"
  option, which feeds CKAN for you.
