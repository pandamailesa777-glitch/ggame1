# External sprite pipeline

1. Create an entity JSON in `art/pipeline/specs`.
2. Prepare prompts and provider request templates:

```powershell
.\tools\external-sprite-cli.ps1 -Action prepare -Provider manual -Spec .\art\pipeline\specs\enemy_zombie.json
```

3. Review `generation-prompt.txt` and the provider request JSON in `art/entities/Enemies/<id>`.
4. Generate in Scenario or PixelLab manually, or submit only after setting environment variables and adding `-Execute`.
5. Export one transparent sheet per animation into the entity's `Source` directory using the naming convention in `STYLE_GUIDE.md`.
6. Import every available entity atomically:

```powershell
.\tools\import-sprites.ps1
```

7. Build and test. Sprite Factory reads `generated_entities.json`; gameplay code does not know PNG names.

API submission is intentionally opt-in because generation consumes provider credits. Raw job responses are stored under ignored `art/.local`.
