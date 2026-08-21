# Nightfall Protocol — sprite style guide

## Canonical rendering

- Semi-realistic, hand-painted 2.5D mobile-game character art; not photorealistic and not chibi.
- Orthographic-looking three-quarter top-down view, camera about 35° above the ground.
- Full body, centered, readable silhouette, 8% transparent padding, no floor or scenery.
- Cool night key light from upper left; restrained warm industrial rim light.
- Materials and faction colors must remain readable at a displayed height of 80–140 px.
- Transparent PNG, straight alpha, sRGB. No text, UI, border, watermark, baked shadow or motion blur.

## Directional and animation contract

- Production default in provider prompts: S, SW, W, NW, N, NE, E, SE.
- Packed engine row order is E, SE, S, SW, W, NW, N, NE (matching `atan2(y, x)`). The PixelLab packer performs this conversion automatically.
- One animation per sheet. Rows are directions; columns are frames. Every cell is square.
- File name: `<entity>__<animation>__<1|4|8>dir__<fps>fps__<loop|once>.png`.
- Recommended enemy cell: 256×256; boss cell: 384×384 or 512×512.
- Minimum set: `move`, `attack`, `hit`, `death`. Bosses may add named telegraphs and phase transitions.
- Identity, equipment, palette, proportions, scale and ground-contact point must not drift between frames.

## Provider policy

- Scenario is preferred for final enemy and boss appearance and consistent art direction.
- PixelLab is preferred for rapid directional consistency and animation exploration. Its output must still satisfy this non-pixel-art project style before production import.
- Amelia, Sam and Zike are protected assets. Never regenerate them unless explicitly requested; their current images are references for future animation only.
- If generation is unavailable, finish gameplay with procedural placeholders. Replacement through Sprite Factory must require no gameplay-code changes.
