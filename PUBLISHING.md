# Publishing SCANsat to CKAN

CKAN does not host files. You host the download (GitHub Releases), a bot follows it to your
release, reads the KSP-AVC `SCANsat.version` inside for the version + KSP compatibility, and
generates the CKAN entry. This is already implemented for the SCANsat repo.

## One-time facts
- License: **BSD-3-clause** (see `LICENSE.txt`; the bundled part models are explicitly cleared for
  redistribution in derivative works with credit).
- Base: SCANsat `dev` (KSPModStewards). Update version **22.1.0** (`SCANsat.version.props`,
  imported by `Directory.Build.props`). Bump it there for each new release.
- Target: KSP **1.12.5** (min 1.12.3, from `SCANsat/SCANsat.csproj`).
- Hard dependency: **KSPTextureLoader** (CKAN identifier `KSPTextureLoader`). The Visual map loads
  `SCANSAT_BODY_TEXTURES` files through it, and the DLL carries a `KSPAssemblyDependency` on it, so
  it is in the netkan's `depends`. ModuleManager stays a dependency (the RSS cfg uses `:NEEDS`).
- The identity (`NAME: SCANsat`, URL, DOWNLOAD) lives in the `KSPVersionFile` item metadata
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
   .\build-release.ps1 # default -KspRoot is ..\Instance; any KSP 1.12.5 install with KSPTextureLoader works
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
   Create a Release on that tag; **attach both** `SCANsat-22.1.0.0.zip` and `SCANsat.version`.
3. **Submit to CKAN (releases are auto-detected by the bot, just give it 1-2 hours)**
