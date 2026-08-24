# Nightfall Protocol UI Style Guide

## Foundation

- Runtime UI uses a `Canvas` with `CanvasScaler.ScaleWithScreenSize`.
- Reference resolution: `1920 × 1080`, match width/height: `0.5`.
- Every screen is parented to `SafeAreaRoot`; no critical control may be parented directly to the Canvas.
- Decorative frames are visual layers. Content always lives in a separate padded container.
- Landscape targets: 16:9, 20:9, 21:9 and 2560 × 1440.

## Type scale

All runtime text uses TextMeshPro and the Russo One dynamic font asset.

| Token | Size | Auto-size range | Use |
| --- | ---: | ---: | --- |
| Title | 44 | 30–44 | Screen titles |
| Heading | 28 | 20–28 | Card and modal headings |
| Button | 22 | 17–22 | Primary/secondary controls |
| Body | 18 | 14–18 | Descriptions and stats |
| Secondary | 15 | 12–15 | Supporting information |
| Counter | 13 | 11–13 | Cooldowns and compact counters |

- Body copy wraps inside a layout-controlled width.
- Buttons use one line and ellipsis; names use at most two lines.
- Auto-size never goes below the minimum listed above.
- Detailed art must have an opaque or gradient text surface above it.

## Spacing and sizing

- Base spacing unit: `8 px` at reference resolution.
- Panel padding: `24–32 px`; card padding: `20–24 px`.
- Adjacent text blocks: `8–12 px`; major sections: `20–32 px`.
- Primary button minimum: `220 × 64`; compact HUD button minimum height: `52`.
- Ability icon: `72–88 px`; compact loadout icon: `40 px`.
- Interactive visual bounds and `Button` hit bounds must be identical.

## Components

- `PrimaryButton`: 9-sliced background, content container, TMP label, normal/highlighted/pressed/disabled colors.
- `Panel`: 9-sliced decorative background plus padded content root.
- `Card`: panel, optional image area, separate text area, footer/action area.
- `AbilityCard`: icon column + title/description column; action covers the full card.
- `CharacterCard`: portrait area, identity block, compact ability list and external action footer.
- `PetCard`: portrait area and stat/description area as siblings.
- `HUDBar`: background and fill images; label is outside the fill.

## Anchoring

- Hero HUD: top-left.
- Timer and boss health: top-center.
- Pause: top-right.
- Active abilities: bottom-right.
- Touch joystick: bottom-left.
- Modals and selection screens: centered inside Safe Area.

## Overflow rules

- Titles: maximum two lines, auto-size within token range.
- Buttons: one line, ellipsis.
- Card descriptions: wrapping with a fixed content height and ellipsis.
- HUD counters: one line, no wrapping.
- Never use unrestricted overflow in gameplay UI.

## New UI acceptance checklist

1. Content is under `SafeAreaRoot`.
2. Background and content are separate objects.
3. Padding comes from a layout group/container, not text offsets.
4. All labels use a type token and bounded auto-size.
5. Clickable bounds match the visual button/card.
6. Verify at 1920×1080, 2400×1080, 2560×1440 and 20:9.
7. Verify Russian strings at their longest expected length.
