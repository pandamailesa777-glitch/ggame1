# Nightfall Protocol — Unity vertical slice

Unity 6000.5.9f1 project migrated from the native Android gameplay prototype.

Implemented in the first slice:

- XZ world with a tilted orthographic 2.5D camera;
- Amelia billboard using the existing canonical directional atlas;
- PixelLab zombie with eight directions and six-frame walk cycle;
- pooled enemies and projectiles without Rigidbody crowd physics;
- manual movement, automatic Holy Flame targeting and damage;
- enemy contact damage, XP, levels and a three-card upgrade pause;
- death and restart;
- landscape-oriented IMGUI HUD suitable for rapid MVP iteration.

Build from the repository root:

```powershell
.\build-unity.ps1 -Target Windows
.\build-unity.ps1 -Target Android
```

Android requires Unity Hub modules: Android Build Support, SDK & NDK Tools and OpenJDK.
