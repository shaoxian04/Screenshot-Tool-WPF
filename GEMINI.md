# Screenshot Tool Project Context

## Project Overview
A custom Windows-based screenshot application designed to replace the built-in Snipping Tool. It provides high-performance region capture, scrolling capture, and a rich set of post-capture annotation tools (arrows, shapes, text, blurring, etc.), with the final output being copied directly to the clipboard.

## Technical Stack
- **Language:** C#
- **Framework:** .NET 8 (WPF)
- **UI Framework:** Windows Presentation Foundation (WPF)
- **Graphics:** SkiaSharp or GDI+ (`System.Drawing.Common`)
- **System Integration:**
  - `NHotkey.Wpf`: Global hotkey management (default: `Ctrl+Shift+A`).
  - `P/Invoke`: Low-level Windows API access for screen capture and window automation.

## Project Structure (Planned)
The project follows a modular .NET architecture:
- `App.xaml`: Application lifecycle and hotkey initialization.
- `MainWindow.xaml`: Transparent full-screen overlay for selection.
- `Views/`: XAML components for the floating toolbar and annotation canvas.
- `Services/`: Logic for capturing, clipboard management, and hotkey hooks.
- `Models/`: Data structures for annotation objects (vector-based).

## Getting Started

### Prerequisites
- **Windows 10/11**
- **Visual Studio 2022** (with ".NET desktop development" workload) or **VS Code** (with C# Dev Kit).
- **.NET 8 SDK** or newer.

### Setting Up
1. Create a new WPF project:
   ```bash
   dotnet new wpf -n ScreenshotTool
   ```
2. Add necessary NuGet packages:
   ```bash
   dotnet add package NHotkey.Wpf
   dotnet add package SkiaSharp.Views.WPF
   ```

### Running the Application
To run the project from the terminal:
```bash
dotnet run
```

## Development Conventions
- **Annotation State:** Store annotations as a list of vector objects. Redraw the canvas on every change to support `Ctrl+Z` (Undo).
- **UX Rules:**
  - No auto-capture on mouse release; requires explicit "Capture" click or `Enter`.
  - Floating toolbar should auto-position relative to the selected region using WPF's `Popup` or a separate `Window`.
  - Inline text editing using `TextBox` overlays on the canvas.
- **Code Style:** Follow Microsoft's C# Coding Conventions. Use `PascalCase` for methods/properties and `_camelCase` for private fields.

## TODO / Roadmap
- [ ] Initialize WPF project structure.
- [ ] Implement global hotkey hook via `NHotkey`.
- [ ] Create `MainWindow` with `AllowsTransparency="True"`.
- [ ] Build the selection logic and image capture service.
- [ ] Implement the annotation canvas with basic shapes and undo support.
