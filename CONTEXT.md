# Terminal Clock Spotify Applet

This context defines the user-facing language for the desktop applet and its placement/media concepts.

## Language

**Target Display**:
The monitor where the applet should appear. When the user says display 2, this means the monitor labeled `2` in Windows Display Settings when that label can be discovered.
_Avoid_: second enumerated screen, monitor index

**Ambient Desktop Applet**:
A small passive window that is present on the desktop without behaving like a normal application window. It does not appear in the taskbar or Alt-Tab and should not take focus when it starts.
_Avoid_: normal desktop app, console window

**Click-Through Mode**:
An optional **Ambient Desktop Applet** state where all pointer input passes through to windows behind it. Dragging and album-art playback control are unavailable while this mode is active.
_Avoid_: interactive overlay, partial click-through

**Pixel Font**:
The bundled retro bitmap-style typeface used to make the applet match the reference visual style. System fonts are fallbacks only.
_Avoid_: monospace font, terminal font

**Sharp Pixel Geometry**:
The applet's visual rule that borders, dividers, progress bars, placeholders, and album-art scaling use hard edges and square pixels. Rounded corners, antialias-heavy ornament, shadows, and blurred geometry are outside the visual language.
_Avoid_: rounded UI, soft glow, blurred chrome

**Dock Position**:
An allowed applet placement at the top-left or top-right corner of any detected display. Dragging moves the applet between Dock Positions rather than arbitrary screen coordinates.
_Avoid_: freeform window position

**Surrounding Window Layout**:
The partial-width exclusion area around a docked **Ambient Desktop Applet** when **Click-Through Mode** is inactive. Other windows may occupy space beside or below the applet but should not overlap it.
_Avoid_: full-width reserved strip, AppBar work area

## Example Dialogue

Developer: "Should the applet appear on the second screen returned by .NET?"

Domain expert: "No. Put it on the Target Display: the monitor labeled `2` in Windows Display Settings."

Developer: "Should users switch to it with Alt-Tab?"

Domain expert: "No. It is an Ambient Desktop Applet, so it should stay visible without becoming part of normal app switching."

Developer: "Can users click album art while Click-Through Mode is active?"

Domain expert: "No. Click-Through Mode passes all pointer input to windows behind the applet, so album-art playback control and dragging are unavailable."

Developer: "Can we just use Consolas?"

Domain expert: "Only as a fallback. The applet should use its bundled Pixel Font by default."

Developer: "Can users drag the applet anywhere?"

Domain expert: "No. Dragging should snap the applet to a Dock Position: top-left or top-right of a detected display."

Developer: "Should other windows stay below a full-width reserved strip?"

Domain expert: "No. Use the Surrounding Window Layout: windows may occupy space beside or below the applet but should not overlap it while Click-Through Mode is inactive."

Developer: "Should we round the panel corners or soften the progress bar?"

Domain expert: "No. Use Sharp Pixel Geometry throughout the applet."
