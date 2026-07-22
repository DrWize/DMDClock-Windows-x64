# Framtida stöd med DMD Extensions som referens

> Parkerad framtidsplan: aktiv utveckling ska tills vidare endast omfatta den gamla 128 × 32-displayen och 4-bitars monokroma SCN-filer. Serum, fullfärg, större upplösningar och DMD Extensions får inte kopplas in i huvudappen förrän denna avgränsning ändras uttryckligen.

`freezy/dmd-extensions` är en framtida teknisk referens, inte ett beroende i den nuvarande builden:

https://github.com/freezy/dmd-extensions

Nuvarande DMDClock-mål förblir stabil uppspelning av DotClk SCN i 128 × 32 och 4-bitars ljusstyrka. Kärnan ska senare kunna utökas utan att bryta detta format.

## Planerad arkitektur

1. Ersätt fasta 128 × 32-antaganden i den generella bildkärnan med ett versionsmärkt bildformat som anger bredd, höjd, pixel-/färgformat och stride.
2. Behåll en optimerad kompatibilitetsväg för nuvarande 128 × 32 SCN och dess transparensmasker.
3. Lägg till färgformat stegvis: 4-bitars monokrom, indexerad palett och RGB24.
4. Lägg till explicita skalningslägen: heltalsskalning, fit, fill och stretch. Testa minst 128 × 32, 192 × 64 och 256 × 64.
5. Separera bildkälla, komposition, skalning och utmatning så att Windows, Raspberry Pi, ESP32-paket och eventuell fysisk DMD-utmatning kan dela testvektorer.
6. Utred en valfri adapter för DMD Extensions, dess nätverksgränssnitt eller stödda enheter först efter att den lokala färgrenderaren är stabil.

## Färg och media

Följande ska utredas separat och vara valfria:

- full RGB-grafik och RGB24-utmatning
- Serum-färgläggning samt VNI/PAL/PAC-kompatibilitet
- PNG, GIF och andra mediaformat
- nätverksströmning till eller från externa DMD-enheter
- hårdvara såsom PIN2DMD, Pixelcade, PinDMD och ZeDMD

## Serum och ColorizingDMD

Planen bygger även på följande arbetsflödesguide:

https://www.pincabpassion.net/t15414-comprehensive-tuto-about-colorizingdmd

Guiden beskriver Serum/cRom som ett tvåstegsflöde:

1. **Identifiering:** en inkommande 2- eller 4-bitars DMD-bild matchas mot kända ramar. Jämförelsemasker ignorerar dynamiska områden såsom poäng, bollar och spelaruppgifter. Maskerna ska återanvändas där det går eftersom många jämförelser kostar CPU.
2. **Färgläggning:** den identifierade ramen får en paletterad 6-bitarsbild med upp till 64 RGB-färger. Dynamiska originalpixlar färgläggs i realtid genom palettset och områdesmasker.

Serum-funktioner att ta hänsyn till i ett framtida, valfritt lager:

- upp till 64-färgspalett per relevant scen/ram
- jämförelsemasker för statiska identifieringsområden
- dynamiska färgset och masker för poäng och annan varierande text
- sprites med begränsade detektionsområden för rörliga objekt
- fasta bakgrunder under dynamiskt innehåll
- tidsstyrda färgrotationer och gradienter
- bild- och videoimport för att skapa paletterade ramar
- frame dumps med tidskod som reproducerbart testunderlag
- förhandsvisning av original och färglagd utdata med verkliga bildtider

### Föreslagen implementeringsordning

Status 2026-07-22: en isolerad prototyp för steg 1–4 finns och är testad i den plattformsoberoende kärnan. Den är fryst, används inte av appens UI och ska inte byggas vidare eller kopplas till en extern `.cRom`-importör under det aktiva gamla-display-spåret.

1. Skapa testvektorer från syntetiska frame dumps; distribuera inte ROM-data eller tredjepartsfärgläggningar utan klar rättighet.
2. Implementera en fristående 6-bitars palettram och konvertering till RGB24.
3. Implementera deterministisk identifiering med jämförelsemask och diagnostik för ingen, en eller flera träffar.
4. Lägg till statisk palettfärgläggning och fallback till den ursprungliga monokroma ramen när ingen regel matchar.
5. Lägg till dynamiska palettset och områdesmasker.
6. Lägg därefter till bakgrunder, färgrotationer och sprites, med tydliga CPU-budgetar.
7. Verifiera resultatet mot DMD Extensions i en separat integrationstestprofil.
8. Lägg till import av `.cRom`/Serum först när formatversioner, licens och kompatibilitet är dokumenterade med testfiler som får distribueras.

### Prestanda och portabilitet

En 64-färgs palettram är särskilt intressant för Raspberry Pi och framtida seriell DMD-utmatning eftersom den kräver mindre bandbredd än RGB24. Desktop-renderaren kan expandera palettindex till RGB24 sent i renderingskedjan, medan ESP32-S3-paket kan behålla palettformatet. Matchning, sprites och färgrotationer ska mätas separat så att en långsam regel inte stoppar klockan eller animationsuppspelningen.

### Innehåll och distribution

Serum-editorn och formatimplementationen är GPL-2.0. Guiden anger dessutom villkor för kreditering och delning av skapade färgläggningar. Varje färgläggningsfil måste därför ha eget ursprung, licens, upphovsperson och distributionsstatus i bibliotekets manifest. Appen ska kunna använda lokalt tillagda filer utan att de automatiskt följer med installationen.

## Licensgräns

DMD Extensions är GPL-2.0. Innan bibliotek, DLL:er eller källkod integreras ska projektets egen licens beslutas och konsekvenserna granskas. En separat process eller dokumenterat protokoll kan vara en bättre integrationsgräns, men även detta ska verifieras innan distribution.
