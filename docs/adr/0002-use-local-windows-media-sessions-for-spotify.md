# Use Local Windows Media Sessions for Spotify

Status: accepted

The applet will read Spotify now-playing data from Windows local media sessions instead of the Spotify Web API. This avoids OAuth, a Spotify developer app, network polling, and background web integration, at the cost of depending on what the Spotify desktop app exposes through Windows media-session APIs.

**Considered Options**

- Windows media sessions: chosen for low resource use and local-only operation.
- Spotify Web API: rejected for v1 because it adds OAuth, network dependency, token handling, and a developer-app setup burden.
