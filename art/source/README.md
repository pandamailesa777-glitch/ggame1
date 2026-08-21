# Sprite Factory source drop

Drop externally generated PNG sheets here (or in a temporary delivery folder) and run:

```powershell
.\tools\import-sprites.ps1 -SourceDirectory .\art\source
```

Naming convention:

`<entity>__<animation>__<1|4|8>dir__<fps>fps__<loop|once>.png`

Example: `vampire__move__8dir__10fps__loop.png`.

Each sheet uses direction rows and animation-frame columns. Frames must be square, every row must have the same frame count, and direction order is E, SE, S, SW, W, NW, N, NE for 8-direction sheets.
