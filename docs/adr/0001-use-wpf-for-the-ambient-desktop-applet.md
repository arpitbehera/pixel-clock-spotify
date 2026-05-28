# Use WPF for the Ambient Desktop Applet

Status: accepted

The applet will be a C# WPF desktop app targeting `.NET 8` on Windows 10, published framework-dependent. WPF is not the absolute lowest-overhead option, but it gives reliable Windows media-session access, monitor placement, DPI handling, bundled fonts, and a borderless always-on-top applet without bringing in a browser runtime.

**Considered Options**

- C# WPF: chosen for Windows integration reliability and implementation speed.
- Native Rust/Win32: lower potential memory footprint, but higher integration risk for WinRT media sessions and polished UI rendering.
- Electron/WebView-style UI: easier styling, but too heavy for the resource goal.
