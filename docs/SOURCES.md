# Originalresurser och referenser

Den här filen sparar projektets viktiga länkar mellan arbetstillfällen. Det är tillåtet att länka till originalresurserna från projektets README, dokumentation och PowerShell-skript. Externa filer ska hämtas lokalt och ska inte checkas in i vårt GitHub-repo.

## Länkar som användaren har angett

- Dr Pinball: https://www.drpinball.co.uk/
- DotClk-Resources: https://github.com/sigmafx/DotClk-Resources
- Modern Hackerspace DMDClock: https://gitlab.com/modernhackerspace/dmdclock
- Bygg- och användarinstruktioner (PDF): https://gitlab.com/modernhackerspace/dmdclock/-/blob/master/DMD%20matrix%20build%20and%20Instructions.pdf?ref_type=heads
- Internet Pinball Database, komplett spellista: https://www.ipdb.org/lists.cgi?anonymously=true&list=games&submit=No+Thanks+-+Let+me+access+anonymously – lokal export mottagen 2026-07-22 och använd som källa för verifierad tillverkare, tillverkningsdatum, spelare, maskintyp och tema i `scene-metadata.json`.

## Ytterligare teknisk referens

- DotClk originalfirmware: https://github.com/sigmafx/DotClk – referens för storyboard, blankning, transparensmask och klocklager
- DMD Extensions: https://github.com/freezy/dmd-extensions – framtida referens för varierande DMD-upplösningar, RGB-grafik, färgläggning, skalning, nätverksströmning och fysisk DMD-utmatning. Projektet använder GPL-2.0; licens och integrationsgräns måste granskas innan kod eller binärer återanvänds.
- Comprehensive tutorial about ColorizingDMD: https://www.pincabpassion.net/t15414-comprehensive-tuto-about-colorizingdmd – framtida referens för Serum-arbetsflödet med frame dumps, jämförelsemasker, 64-färgspaletter, dynamisk färgläggning, sprites, bakgrunder och färgrotationer.
- ColorizingDMD/Serum-editor: https://github.com/SerumColor/ColorizingDMD – officiell editor och formatimplementation under GPL-2.0.
- PinScreen för Windows: https://github.com/davidvanderburgh/pinscreen
- Inter-fonten: https://github.com/rsms/inter – distribueras med appen under SIL Open Font License 1.1

## Lokala resurser

Planerad lokal plats: `external/`

Mappen ska ignoreras av Git. `scripts/Get-OriginalResources.ps1` ska senare kunna hämta originalresurserna från länkarna ovan och spara information om hämtad version eller commit lokalt.
