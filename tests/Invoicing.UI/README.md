# Native Step 9 UI verification

After building the Windows project and invoicing libraries, run `python tests/Invoicing.UI/build-preview.py` with Python and Visual Studio's C# compiler available.

The script compiles the actual Designer files and Step 9 UI partial classes with inert collaborators. It removes original Designer data bindings/event hooks, replaces original form constructors, uses synthetic data, and loads the compiled application's resources without executing its entry point. Save/recovery/transport stubs throw if invoked. The production global-setting builders and new per-type change handlers are exercised directly.

It checks Ilyda/iTimologia selection, provider-specific POS mode lists and mapping, unsupported-mode rejection after switching provider, historical credential reference retention, masked credential fields, and transmission/signing controls. Selecting a provider clears and disables fiscal-device signing. Twenty native renders are saved under ignored `output/`, including F4 waiting for **Επανάληψη πληρωμής**. The retry button is exercised with an inert dispatch spy to verify immediate dispatch without confirmation and duplicate-click prevention; incomplete responses keep it disabled. Inspect those images for placement/clipping. These are offline native control renders, not screenshots of a running production installation.
