# Bundled fonts

Files in this directory are copied into application publish output under `fonts/`.

## Inter

- File: `Inter/InterVariable.ttf`
- Use: bundled text font with broad Unicode coverage, including Swedish characters
- Source: https://github.com/rsms/inter
- Source commit: `353b61b9f4430d5f420d56605a6e7993e0941470`
- SHA-256: `4989B125924991B90D05B2D16E0E388C48F7D5BB8B30539BBF9C755278D0CCAF`
- License: SIL Open Font License 1.1; see `Inter/OFL-1.1.txt`

## Original DotClk clock fonts

`DotClk/ALTERN8.fnt`, `FISHY.fnt`, `TREK.fnt`, and `TWILIGHT.fnt` are the original
DotClk clock resources. They are embedded into the application assembly and are not
copied to the external `fonts/` directory. The application preserves the original
digit and clock glyphs and supplies its own dot-matrix date separators where a font
does not contain them.

- Source: https://github.com/DrWize/DotClk-Resources
- Format: DotClk version 1, four-bit bitmap font
- Upstream repository does not currently include an explicit license file

## Pinball by Fontalicious

The Pinball OpenType family (Pinball, Galaxy, Scrambler, Scrambler II and Wizard) was downloaded separately for evaluation from https://www.fontalicious.com/fonts/pinball. The source page describes it as a free download but asks users to contact Fontalicious for commercial use. These `.otf` files are not bundled or redistributed until their applicable rights have been confirmed.
