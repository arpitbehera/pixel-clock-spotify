# Terminal Clock Spotify Applet

This context defines the user-facing language for the desktop applet and its placement/media concepts.

## Language

**Target Display**:
The monitor where the applet should appear. When the user says display 2, this means the monitor labeled `2` in Windows Display Settings when that label can be discovered.
_Avoid_: second enumerated screen, monitor index

**Ambient Desktop Applet**:
A small passive window that is present on the desktop without behaving like a normal application window. It does not appear in the taskbar or Alt-Tab and should not take focus when it starts.
_Avoid_: normal desktop app, console window

**Pixel Font**:
The bundled retro bitmap-style typeface used to make the applet match the reference visual style. System fonts are fallbacks only.
_Avoid_: monospace font, terminal font

**Sharp Pixel Geometry**:
The applet's visual rule that borders, dividers, progress bars, placeholders, and album-art scaling use hard edges and square pixels. Rounded corners, antialias-heavy ornament, shadows, and blurred geometry are outside the visual language.
_Avoid_: rounded UI, soft glow, blurred chrome

**Dock Position**:
An allowed applet placement at the top-left or top-right corner of any detected display. Dragging moves the applet between Dock Positions rather than arbitrary screen coordinates.
_Avoid_: freeform window position

## Example Dialogue

Developer: "Should the applet appear on the second screen returned by .NET?"

Domain expert: "No. Put it on the Target Display: the monitor labeled `2` in Windows Display Settings."

Developer: "Should users switch to it with Alt-Tab?"

Domain expert: "No. It is an Ambient Desktop Applet, so it should stay visible without becoming part of normal app switching."

Developer: "Can we just use Consolas?"

Domain expert: "Only as a fallback. The applet should use its bundled Pixel Font by default."

Developer: "Can users drag the applet anywhere?"

Domain expert: "No. Dragging should snap the applet to a Dock Position: top-left or top-right of a detected display."

Developer: "Should we round the panel corners or soften the progress bar?"

Domain expert: "No. Use Sharp Pixel Geometry throughout the applet."
