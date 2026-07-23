# Original resources and references

This file preserves important project links between work sessions. The project's README, documentation, and PowerShell scripts may link to the original resources. External files should be downloaded locally and must not be committed to this GitHub repository.

## Links provided by the user

- Dr Pinball: https://www.drpinball.co.uk/
- DotClk Resources: https://github.com/sigmafx/DotClk-Resources
- Modern Hackerspace DMDClock: https://gitlab.com/modernhackerspace/dmdclock
- Build and usage instructions (PDF): https://gitlab.com/modernhackerspace/dmdclock/-/blob/master/DMD%20matrix%20build%20and%20Instructions.pdf?ref_type=heads
- Internet Pinball Database complete game list: https://www.ipdb.org/lists.cgi?anonymously=true&list=games&submit=No+Thanks+-+Let+me+access+anonymously — a local export is used as the source for verified manufacturer, manufacturing date, player count, machine type, and theme data in `scene-metadata.json`.

## Additional technical references

- Original DotClk firmware: https://github.com/sigmafx/DotClk — reference for storyboards, blanking, transparency masks, and clock layers
- DMD Extensions: https://github.com/freezy/dmd-extensions — future reference for varying DMD resolutions, RGB graphics, colorization, scaling, network streaming, and physical DMD output. The project uses GPL-2.0; its license and integration boundary must be reviewed before code or binaries are reused.
- Comprehensive ColorizingDMD tutorial: https://www.pincabpassion.net/t15414-comprehensive-tuto-about-colorizingdmd — future reference for the Serum workflow with frame dumps, comparison masks, 64-color palettes, dynamic colorization, sprites, backgrounds, and color rotations
- ColorizingDMD/Serum editor: https://github.com/SerumColor/ColorizingDMD — official editor and format implementation under GPL-2.0
- PinScreen for Windows: https://github.com/davidvanderburgh/pinscreen
- Inter font: https://github.com/rsms/inter — distributed with the application under the SIL Open Font License 1.1

## Local resources

Planned local location: `external/`

The directory should be ignored by Git. `scripts/Get-OriginalResources.ps1` may later download the original resources from the links above and store local version or commit information.
