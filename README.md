# Screenshot Tool (WPF)

A high-performance Windows-based screenshot application built with C# and .NET 8 (WPF).

## Features
- **Region Capture:** Click and drag to select any part of the screen.
- **Annotation Tools:** 
  - **Arrow:** Point out important details.
  - **Rectangle:** Highlight areas.
  - **Pen:** Freehand drawing.
  - **Blur:** Obscure sensitive information with a mosaic effect.
  - **Text:** Add inline text with adjustable font sizes.
- **Smart Manipulation:** Click any object to move or resize it.
- **Color Selection:** Choose from multiple colors for your annotations.
- **Instant Clipboard:** Copy flattened images directly to your clipboard with `Enter`.
- **High DPI / Multi-Monitor Support:** Works seamlessly across laptop screens and external monitors with different scaling.

## Shortcuts
- **Capture:** `Ctrl + Shift + A`
- **Done/Copy:** `Enter`
- **Cancel/Exit:** `Esc`
- **Undo:** `Ctrl + Z`
- **Delete Object:** `Delete` (when an object is selected)

## Technical Stack
- **Framework:** .NET 8.0 (WPF)
- **Graphics:** SkiaSharp (high-performance rendering)
- **System Hooks:** Native Win32 RegisterHotKey for global triggers.

## Getting Started
1. Clone the repository.
2. Ensure you have the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.
3. Run the project:
   ```bash
   dotnet run --project ScreenshotTool
   ```
