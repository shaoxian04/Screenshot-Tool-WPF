# Screenshot Tool — Project Context

## Project Overview
A custom Windows screenshot tool built to replace the built-in Snipping Tool.
Triggered by a hotkey (Ctrl+Shift+A), it provides a region selection overlay, floating annotation toolbar, and copies the result to clipboard.

---

## Tech Stack
- **Language:** C#
- **Framework:** .NET 8 (WPF)
- **UI / Overlay:** WPF Windows (`WindowStyle="None"`, `AllowsTransparency="True"`)
- **Image processing:** `System.Drawing.Common` (GDI+) or `SkiaSharp`
- **Hotkey Listener:** `NHotkey.Wpf` or custom P/Invoke keyboard hooks
- **Scrolling Capture:** `P/Invoke` (User32 / GDI32) for window automation

---

## Core Features

### 1. Capture Modes
| Mode | Description |
|---|---|
| Region Selection | User drags to select area on screen |
| Full Screen | Captures entire screen |
| Active Window | Captures the currently focused window |
| Scrolling Capture | Separate mode; auto-scrolls and stitches screenshots |

### 2. Annotation Tools (post-capture)
| Tool | Behavior |
|---|---|
| Arrow | Click and drag to draw directional arrow |
| Line | Click and drag to draw straight line |
| Rectangle | Click and drag to draw rectangle shape |
| Circle / Ellipse | Click and drag to draw ellipse |
| Freehand Draw | Click and drag for freehand pen stroke |
| Text | Click on image → inline text cursor appears, type to place text |
| Highlight | Semi-transparent colored rectangle overlay |
| Blur | Applies gaussian blur to selected region |
| Pixelate | Applies pixelation effect to selected region |
| Step Numbering | Click to place auto-incrementing numbered circles (1, 2, 3...) |

### 3. Output
- Copy final annotated image to **clipboard only**
- No file saving (unless added later)

---

## UX & Interaction Design

### Hotkey Trigger
- User presses a configurable global hotkey (e.g. `Ctrl+Shift+S`)
- App wakes up and enters capture mode

### Capture Flow (Region / Full Screen / Active Window)
```
1. Hotkey pressed
2. Screen dims with semi-transparent dark overlay
3. User clicks and drags to select region
4. Snapshot is taken of the selected region
5. Overlay freezes (shows captured image underneath)
6. Floating toolbar appears near the selection (above or below, auto-positioned)
7. User selects annotation tools and annotates
8. User presses Enter OR clicks "Capture" button → image copied to clipboard
9. User presses Esc OR clicks "Exit" button → cancel, overlay dismissed
```

### Scrolling Capture Flow
```
1. User presses hotkey
2. Overlay appears — user clicks "Scroll Capture" icon in toolbar BEFORE selecting region
3. User selects the region to scroll-capture
4. Tool begins auto-scrolling, capturing and stitching frames
5. Stitched result opens in annotation window
6. User annotates → Enter → copied to clipboard
```

### Important UX Rules
- **No auto-capture on mouse release** — screenshot is only taken when user presses Enter or clicks the Capture button
- Toolbar floats **near the selection**, auto-positioned to avoid going off-screen
- Toolbar appears **after** region is selected (post-capture annotation)

---

## Toolbar Design

### Toolbar Buttons (left to right)
```
[Scroll Mode] | [Arrow] [Line] [Rect] [Circle] [Freehand] [Text] [Highlight] [Blur] [Pixelate] [Step#] | [Capture ✓] [Exit ✗]
```

- **Scroll Mode toggle** — only active before capture; switches to scrolling capture mode
- **Annotation tools** — activated after capture
- **Capture button (✓)** — confirms and copies to clipboard (same as Enter)
- **Exit button (✗)** — cancels and dismisses overlay (same as Esc)

### Toolbar Placement Rules
- Floats near the selected region
- Preferred position: just below the selection
- If not enough space below → appear above
- If selection is near right edge → right-align toolbar

---

## Text Tool — Inline Behavior
- User selects Text tool from toolbar
- User clicks anywhere on the captured image
- An inline text cursor appears at the click position (no popup dialog)
- User types; text renders live on the image
- User clicks elsewhere or presses Escape to deselect text box
- Text box can be repositioned by dragging

---

## Annotation State Management
- Each annotation is stored as an object (type, coordinates, style properties)
- Annotations are rendered on top of the captured image in a canvas/painter layer
- Undo (`Ctrl+Z`) should remove the last annotation object
- Annotations are flattened onto the image only at export (clipboard copy)

---

## File Structure (Suggested)
```
ScreenshotTool/
├── ScreenshotTool.sln
├── ScreenshotTool.csproj
├── App.xaml / App.xaml.cs       # Application entry point & hotkey management
├── MainWindow.xaml / .cs        # Transparent selection overlay
├── Views/
│   ├── FloatingToolbar.xaml     # The annotation toolbar
│   └── AnnotationCanvas.xaml    # Interactive drawing layer
├── Services/
│   ├── ScreenCaptureService.cs  # Capture logic (region, window, scrolling)
│   ├── ClipboardService.cs      # Native clipboard interaction
│   └── HotkeyService.cs         # Low-level hook management
├── Models/
│   └── AnnotationObject.cs      # Base class for arrows, text, shapes, etc.
└── Components/
    ├── ArrowRenderer.cs
    ├── BlurEffect.cs
    └── TextEditor.cs
```

---

## Dependencies
Install via NuGet:
- `NHotkey.Wpf` (Global hotkey management)
- `SkiaSharp.Views.WPF` (High-performance rendering)
- `System.Drawing.Common` (GDI+ access)

---

## Out of Scope (for now)
- File saving
- Cloud upload / share link
- Screen recording (GIF/MP4)
- OCR text extraction
- History/gallery
- Color picker
