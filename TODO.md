# DMDClock för Windows x64 – TODO

Målet är att skapa en fristående Windows-app som visar klocka och spelar upp DotClk-animationer på en vanlig bildskärm. Appen ska inte kräva Raspberry Pi, Teensy eller fysisk DMD-hårdvara.

## Aktiv avgränsning

Aktiv utveckling gäller endast den klassiska DotClk-displayen: 128 × 32, 4-bitars monokroma `.scn`-filer, storyboard/mask, klocklager, bibliotek, styrning och klassiska enfärgade DMD-teman. Serum, cRom, full RGB, större DMD-format, DMD Extensions-integration och färgläggningsverktyg är frysta som framtidsarbete och ska inte prioriteras eller kopplas in i appen nu.

Ljud ingår inte i projektet. Appen ska inte spela upp, importera eller kräva ljudspår.

När appen är färdig ska tiden visas som på en klassisk DMD (dot matrix display): en 128 × 32-punkters yta där siffror, text och bilder byggs upp av tydligt separerade, runda lysande punkter mot svart bakgrund. Standardutseendet ska vara orange/rödgula punkter med varierande ljusstyrka, svag glöd och synligt mörkt mellanrum mellan punkterna – inte släta typsnitt eller vanliga bildskärmspixlar. Displayen ska behålla proportionen 4:1 och ha en tunn svart kant även när fönstret skalas upp.

## Föreslaget första mål

Bygg först en enkel prototyp som:

- startar direkt i Windows 10/11 x64
- öppnar en eller flera `.scn`-filer från DotClk-Resources
- spelar animationerna i ett skalbart 128 × 32-fönster
- visar en ren dot matrix-display med en tunn svart kant
- ger åtkomst till hela menyn genom högerklick var som helst i displayrutan
- har ett enkelt sätt att pausa och fortsätta uppspelningen
- visar aktuell tid mellan animationerna
- kan spela en video eller animation mellan varje visning av tiden
- kan välja animationer från hela samlingen, en tillverkare eller ett visst spel
- kan spela animationerna slumpmässigt eller i deras ursprungliga ordning
- kan aktivera eller stänga av enskilda spel och animationer
- kan växla till kantlöst helskärmsläge
- sparar inställningar lokalt

## Nästa prioriterade arbete

Arbeta i denna ordning. Senare plattforms- och extrafunktioner ska inte gå före en stabil lokal Windows-prototyp.

### Prioritet 1 – spela en vald SCN-fil

- [x] Implementera en uppspelningsmotor som följer storyboardens bildfördröjningar
- [x] Implementera storyboardens första/sista steg, blankning, transparensmask och klocklager enligt originalfirmware
- [x] Lägg till filöppning och spela en vald `.scn`-fil i den befintliga DMD-renderaren
- [x] Lägg till spela/pausa, nästa bildruta och föregående bildruta
- [x] Växla från klocka till en animation och tillbaka utan att frysa användargränssnittet
- [x] Lägg till tester för timing, paus, avslut och trasiga filer

### Prioritet 2 – välj en mapp och håll biblioteket uppdaterat

- [x] Använd `./scenes` som standardmapp, skapa den vid behov och skanna den automatiskt vid start
- [x] Bevara innehållet i den publicerade `scenes`-mappen mellan nya builds
- [x] Lägg till val av animationsmapp och rekursiv första skanning
- [x] Skapa ett versionsmärkt, atomiskt biblioteksindex med stabila fil-ID:n
- [x] Upptäck nya, ändrade och borttagna `.scn`-filer inkrementellt
- [x] Hoppa över trasiga filer, logga orsaken och fortsätt spela fungerande filer
- [x] Logga starttid, sluttid, varaktighet och resultat för varje biblioteksskanning
- [x] Logga när displayen växlar mellan klocka, datum och en namngiven animation
- [x] Logga appstart med unikt build-ID och normalt avslut med körtid
- [x] Begränsa den aktiva loggfilen till 3 MB och rotera en föregående logg
- [x] Visa och logga spel samt animationssekvens som liten vanlig text nere till höger vid uppspelningsstart
- [x] Lägg till slumpmässig och naturligt sorterad sekventiell uppspelning

### Prioritet 3 – grundläggande styrning och sparade inställningar

- [x] Implementera högerklicksmenyn med spela/pausa, nästa, föregående och visa klocka
- [ ] Spara animationsmapp, uppspelningsläge, intervall, färg och ljusstyrka i AppData
- [x] Spara automatisk cykel, intervall, antal animationer och slumpmässigt/sekventiellt läge i AppData
- [x] Lägg till kortkommandona mellanslag, T, D, I, F11 och Escape
- [x] Lägg till kantlöst helskärmsläge

### Prioritet 4 – DotClk-fonter

- [ ] Implementera och testa `.fnt`-läsaren
- [ ] Visa och jämför ALTERN8, FISHY, TREK och TWILIGHT i appen
- [ ] Gör fonten valbar och behåll den inbyggda 5×7-fonten som reserv
- [x] Lägg en öppet licensierad TTF-font med svenska tecken i `assets/fonts` och inkludera den i installationen
- [ ] Implementera TTF/OTF-rendering till 4-bitars `DmdFrame`

### Prioritet 5 – första distribuerbara Windows-build

- [ ] Slutför README och VS Code-konfiguration
- [x] Skapa självständig `win-x64`-build
- [ ] Skapa portabel ZIP
- [x] Verifiera att föregående build arkiveras före varje ny build
- [x] Kör en fullständig SCN-kompatibilitetsskanning och spara rapporten med builden

## Beslut att ta

Ändra gärna alternativen och markera ditt val med `[x]`.

### Teknik

- [x] Utred om Java behövs över huvud taget
- [x] Dokumentera endast den gamla Java-kodens `.scn`-tolkning och beteende
- [x] Ersätt gammal Java-kod med en modern, inbyggd implementation
- [ ] C# och aktuell LTS-version av .NET med WPF – rekommenderat för en riktig Windows-app
- [ ] C# och aktuell LTS-version av .NET med WinUI 3
- [x] C# med .NET 10 LTS och Avalonia UI för Windows och Raspberry Pi
- [ ] Webbaserad app med Electron
- [ ] Annat: ______________________________

Utgångspunkten är att slutprodukten inte ska kräva Java. Java-verktyget får användas som teknisk referens under utvecklingen, men användaren ska få en modern, självständig Windows x64-app utan gammal Java-runtime.

Den plattformsoberoende kärnan ska hållas åtskild från användargränssnitt och fysisk displayutmatning. Huvudappen ska kunna rikta sig mot Windows och Raspberry Pi, medan en eventuell ESP32-S3-version byggs som separat firmware mot samma dokumenterade bildruteformat och testvektorer.

### Utseende

- [x] Klassiska orange DMD-prickar
- [ ] Framtid: färganimationer
- [ ] Framtid: valbart mellan klassisk monokrom DMD och fullfärg
- [x] Tunn svart kant runt dot matrix-displayen
- [x] Ingen permanent menyrad eller synliga inställningsknappar
- [ ] Eget förslag: ________________________

### Visning

- [x] Vanligt flyttbart fönster
- [ ] Kantlöst fönster
- [ ] Helskärm på valfri bildskärm
- [ ] Starta automatiskt med Windows
- [ ] Lägg till ett valbart Windows-skärmsläckarläge (`.scr`) som använder samma klocka, animationer och inställningar
- [ ] Alltid överst
- [x] Högerklick var som helst i displayrutan öppnar hela menyn
- [x] Menyn stängs när användaren klickar utanför den eller trycker Escape
- [x] Menyn hålls öppen när ett menyalternativ väljs så att flera inställningar kan ändras i följd
- [x] Visa Alien Tech i två sekunder vid programstart och länka Hjälp till GitHub-profilen
- [x] Lägg menyerna i externa i18n-filer med engelska som standard, svensk översättning och översättningsmall
- [ ] Vänsterklick och drag flyttar ett kantlöst fönster
- [x] Mellanslag pausar eller fortsätter uppspelningen
- [x] Tangenten T visar tiden direkt
- [x] Tangenten D visar datumet direkt, om datumvisning är aktiverad
- [x] Tangenten I slår av eller på informationsrutan med spel och animationssekvens
- [x] F11 växlar helskärm och Escape lämnar helskärm
- [ ] Visa tydligt men diskret när uppspelningen är pausad

Föreslaget innehåll i högerklicksmenyn:

- Spela/pausa
- Nästa och föregående animation
- Visa klocka nu
- Klock- och animationsläge
- Animationsmapp och spellista
- Färg, ljusstyrka och DMD-effekt
- Storlek, helskärm, bildskärm och alltid överst
- Automatisk start med Windows
- Inställningar, diagnostik, om och avsluta

### Klocka

- [x] Valbart 24-timmarsformat
- [x] Valbart 12-timmarsformat
- [x] Valbara vanliga datumformat: ISO, europeiskt, amerikanskt och punktseparerat
- [ ] Valbar datumvisning
- [ ] Visa veckodag
- [ ] Visa väder senare
- [x] Visa tiden, spela en video/animation och återgå sedan till tiden
- [x] Gör antalet videor mellan tidsvisningarna konfigurerbart, med en video som standard
- [x] Gör tiden som klockan visas mellan animationsomgångarna konfigurerbar
- [x] Gör pausen med klockvisning mellan varje animation i samma cykel konfigurerbar

### Animationsurval och uppspelningsordning

- [x] Slumpmässigt urval bland alla aktiverade animationer
- [ ] Slumpmässigt urval bland alla aktiverade Stern-animationer
- [ ] Slumpmässigt urval inom ett valt spel
- [ ] Sekventiell uppspelning inom ett valt spel
- [ ] Kronologisk uppspelning av animationer enligt originalets fil-/sekvensordning
- [x] Om säker tidsmetadata saknas: använd naturlig sortering av katalog- och filnamn och visa vilken ordning som används
- [ ] Gör tillverkare valbara, exempelvis Stern, Williams och Bally när materialet kan identifieras säkert
- [ ] Visa ett biblioteksträd: tillverkare → spel → animation
- [ ] Kryssruta för att aktivera eller stänga av en hel tillverkare
- [ ] Kryssruta för att aktivera eller stänga av ett helt spel
- [ ] Kryssruta för att aktivera eller stänga av en enskild animation
- [ ] Lägg till sökning samt `Markera alla`, `Avmarkera alla` och `Återställ`
- [ ] Visa förhandsvisning och grunduppgifter för vald animation
- [ ] Spara alla val och blockerade animationer mellan programstarter
- [x] Läs valfri `scene-metadata.json` med prefixregler och exakta filöverstyrningar
- [x] Hoppa över avstängda, saknade eller trasiga animationer utan att uppspelningen stannar
- [x] Visa hur många animationer som är aktiva i nuvarande urval

### Uppdaterbart animationsbibliotek

- [x] Upptäck automatiskt nya, ändrade, flyttade och borttagna filer utan att appen behöver uppdateras
- [x] Erbjud automatisk bevakning och manuell `Skanna om` från menyn
- [x] Gör omskanningen inkrementell med filstorlek, ändringstid och innehållshash så att oförändrade filer inte avkodas igen
- [x] Låt biblioteket vara användbart medan en stor omskanning pågår och visa diskret status samt slutresultat
- [ ] Lägg till nya filer utan att återställa användarens aktiveringsval, blockeringslista eller spellistor
- [x] Använd stabila biblioteks-ID:n och bevara ID vid innehållsuppdatering eller flytt när filen kan identifieras
- [x] Hantera filer som fortfarande kopieras genom att upptäcka ändring under skanning och försöka igen vid nästa filhändelse
- [x] Skriv biblioteksindex atomiskt och behåll senast fungerande index om en skanning avbryts eller misslyckas
- [x] Versionsmärk indexformatet; migrering läggs till när en andra schemaversion finns
- [x] Rapportera nya formatversioner och okända filer utan att stoppa uppspelningen av filer som redan fungerar

## Arbetsplan

### 1. Undersök filformatet

- [x] Hämta DotClk-Resources från GitHub
- [x] Dokumentera strukturen i `.scn`-formatet
- [x] Jämför tolkningen med Modern Hackerspaces Java-kod
- [x] Avgör vilka delar av Java-koden som behöver implementeras på nytt och vilka som kan utelämnas
- [x] Implementera `.scn`-läsaren direkt i den valda moderna plattformen
- [x] Kontrollera bildstorlek, färgdjup och bildfördröjningar
- [x] Skapa testfall med några små animationsfiler
- [x] Skapa en automatisk kompatibilitetsskanning för hela animationssamlingen
- [x] Testa att samtliga `.scn`-filer kan öppnas och avkodas utan krasch
- [ ] Logga filer med okänd version, trasiga bildrutor eller ogiltiga tidsvärden
- [ ] Sammanställ en rapport med antal godkända, varnade och underkända filer

### 1b. Typsnitt

DotClk-Resources innehåller för närvarande fyra `.fnt`-typsnitt:

- `ALTERN8.fnt`
- `FISHY.fnt`
- `TREK.fnt`
- `TWILIGHT.fnt`

- [ ] Dokumentera `.fnt`-formatet
- [ ] Testa att alla fyra typsnitt kan läsas och visas korrekt
- [x] Kontrollera vilka tecken, siffror och symboler varje typsnitt innehåller
- [x] Lägg till ett eget inbyggt standardtypsnitt som reserv
- [ ] Visa tillgängliga typsnitt i högerklicksmenyn
- [ ] Låt resurskontrollen rapportera om nya typsnitt tillkommer i originalresursen

### 2. Grundläggande Windows-app

- [x] Skapa projekt med aktuell LTS-version av .NET och målplattform Windows x64
- [ ] Projektet ska kunna utvecklas, byggas och felsökas direkt i VS Code
- [ ] Undvik beroenden till fullständiga Visual Studio och projektspecifika användarinställningar
- [x] Skapa en vanlig `.sln`-fil och tydligt organiserade `.csproj`-projekt
- [ ] Lägg till rekommenderade VS Code-tillägg i `.vscode/extensions.json`
- [ ] Lägg till bygg-, test- och publiceringskommandon i `.vscode/tasks.json`
- [ ] Lägg till start och felsökning i `.vscode/launch.json`
- [ ] Kontrollera att samma kommandon fungerar både i VS Code och direkt i PowerShell
- [x] Skapa huvudfönster i proportionen 4:1
- [x] Implementera första DMD-renderaren med separerade runda punkter
- [x] Lägg till tunn svart kant runt displayytan
- [ ] Implementera en komplett högerklicksmeny i displayrutan
- [x] Lägg till öppna-mapp-dialog
- [x] Lägg till start, stopp, nästa och föregående animation
- [x] Visa tydliga felmeddelanden för trasiga eller okända filer

### 3. Klockfunktion

- [x] Skapa ett första inbyggt DMD-anpassat siffertypsnitt
- [x] Visa aktuell tid
- [x] Rendera varje DMD-punkt som en tydligt avgränsad rund ljuspunkt mot svart bakgrund
- [x] Bevara 128 × 32-upplösningen och proportionen 4:1 vid all skalning
- [x] Lägg till valbar glöd och ljusstyrkenivå utan att sudda ihop punkterna
- [x] Återgå automatiskt till klockan när en vald animation är färdig
- [x] Lägg till inställning för klockintervall och antal animationer per cykel
- [ ] Implementera samtliga urvalslägen under `Animationsurval och uppspelningsordning`

### 4. Inställningar

- [ ] Alla inställningar ska gå att nå från högerklicksmenyn
- [ ] Välj animationsmapp
- [ ] Välj bildskärm
- [ ] Välj fönsterstorlek eller helskärm
- [x] Välj DMD-färg och ljusstyrka
- [ ] Ställ in bildhastighet
- [ ] Slå på eller av klocka, datum och animationer
- [ ] Välj 12- eller 24-timmarsformat
- [x] Välj antal videor/animationer mellan tidsvisningarna
- [ ] Välj uppspelningsläge: alla, tillverkare, spel, slumpmässigt eller kronologiskt
- [ ] Hantera aktiva och avstängda tillverkare, spel och enskilda animationer
- [x] Spara de implementerade inställningarna i användarens AppData-mapp

### 4b. Lokala originalresurser

- [ ] Skapa `scripts/Get-OriginalResources.ps1`
- [ ] Låt PowerShell-skriptet hämta nödvändiga originalresurser från deras officiella GitHub- och GitLab-adresser
- [x] Hämta resurserna till en lokal `external/`-mapp som är ignorerad av Git
- [x] Lägg aldrig originalanimationer, externa binärfiler eller externa projekt i vårt GitHub-repo
- [ ] Gör skriptet säkert att köra flera gånger utan att skapa dubbletter
- [ ] Lägg till val för att uppdatera eller hämta om resurserna
- [ ] Visa vad som är nytt, ändrat och borttaget innan eller efter en resursuppdatering
- [ ] Spara versions-, commit- eller hämtningsinformation lokalt för reproducerbara tester
- [ ] Ge tydliga felmeddelanden vid nätverksfel eller ändrade nedladdningsadresser
- [ ] Låt appen fungera utan resurser och visa hur användaren hämtar dem

### 4c. Raspberry Pi och ESP32-S3

- [ ] Bygg Windows- och Raspberry Pi-versionerna från samma plattformsoberoende kärna
- [ ] Definiera och dokumentera ett versionsmärkt `DmdFrame`-format oberoende av C# och ESP-IDF
- [ ] Skapa gemensamma testvektorer som verifieras identiskt av desktopappen och ESP32-firmware
- [ ] Skapa ett konverteringsverktyg som bygger optimerade, versionsmärkta animationspaket för ESP32 från bibliotekets aktuella filer
- [ ] Lägg ett manifest med fil-ID, innehållshash, formatversion och paketversion i varje ESP32-paket
- [ ] Gör ESP32-paket enkla att ersätta via SD-kort och förbered för säker uppdatering via lokalt nätverk
- [ ] Validera ett nytt paket fullständigt innan det aktiveras och behåll föregående fungerande paket vid fel eller avbruten överföring
- [ ] Låt nya animationsfiler på Windows eller Raspberry Pi inkluderas genom en ny inkrementell paketbyggnad utan ändring av firmware

### 5. Distribution

- [x] Använd ett gemensamt byggskript som alltid arkiverar föregående build innan `output/current` ersätts
- [x] Stäng relaterade DMDClock-processer före build och starta automatiskt den nya Windows-builden efter lyckad publicering
- [x] Spara gamla builds under `output/archive/<tidsstämpel>-<plattform>` med ett manifest
- [ ] Lägg till valbar gallring av gamla builds, men radera dem aldrig automatiskt utan uttrycklig inställning
- [x] Publicera den aktuella distributionsversionen för Windows x64
- [x] Skapa en självständig version som inte kräver separat .NET-installation
- [ ] Skapa portabel ZIP-version
- [ ] Överväg ett vanligt installationsprogram
- [ ] Lägg till genväg och valfri automatisk start
- [ ] Testa på en ren Windows 10/11-dator

### 6. README och dokumentation

- [ ] Skapa en strukturerad `README.md`
- [ ] Beskriv projektets syfte och vad som inte ingår i repot
- [ ] Visa hur projektet byggs för Windows x64
- [ ] Beskriv hur projektmappen öppnas och används i VS Code
- [ ] Lista nödvändiga och rekommenderade VS Code-tillägg
- [x] Dokumentera medföljande fontfiler, ursprung, kontrollsumma och licens
- [ ] Dokumentera kommandon för att återställa paket, bygga, starta, testa och publicera
- [ ] Visa hur `Get-OriginalResources.ps1` körs
- [ ] Visa var lokala animationer och typsnitt hamnar
- [ ] Beskriv hur appen startas och styrs
- [ ] Dokumentera högerklicksmenyn och kortkommandona
- [ ] Beskriv testning av en fil och hela animationssamlingen
- [ ] Lägg till felsökning för saknade resurser, ogiltiga `.scn`-filer och skärmproblem
- [ ] Länka till `docs/SOURCES.md` för beständiga original- och referenslänkar

## Viktiga kontroller

- [ ] Licensbedömningar hanteras separat av andra och ska inte blockera den tekniska utvecklingen
- [ ] Vårt GitHub-repo ska inte innehålla proprietära animationer, ROM-filer eller andra externa binärdata
- [ ] GitHub-repot ska endast innehålla vår egen källkod, dokumentation och egna fria testresurser
- [ ] Låt användaren själv välja en lokal animationsmapp utanför applikationen och repot
- [ ] Använd syntetiska eller egenproducerade minimala `.scn`-filer i automatiska tester
- [ ] Behåll tekniska källhänvisningar till projekt vars filformat eller beteende har studerats
- [ ] Det går bra att länka till originalresurserna från README, dokumentation och nedladdningsskript
- [ ] Spara alla användarens originallänkar beständigt i `docs/SOURCES.md`
- [ ] Kör inte gamla Java-verktyg i slutprodukten

## Förslag på mappstruktur

```text
DMDClock-Windows-x64/
├── README.md             Installation, användning och utveckling
├── src/                 Windows-appens källkod
├── tests/               Tester och små tillåtna testdata
├── docs/                Filformats- och användardokumentation
│   └── SOURCES.md        Beständiga original- och referenslänkar
├── scripts/              PowerShell-skript för lokal installation
├── tools/               Egna konverteringsverktyg
├── assets/              Egna typsnitt, ikoner och grafik
├── external/             Lokalt hämtade originalresurser, ignoreras av Git
├── output/              Färdiga byggen och ZIP-filer
└── TODO.md
```

Externa animationssamlingar och referensprojekt ska hållas utanför GitHub-repot. Lokala sökvägar till sådant innehåll ska ignoreras av Git och väljas av användaren i appen.

## Idéer för senare versioner

### Framtid – Serum, fullfärg och större DMD

Detta avsnitt är uttryckligen parkerat. Inget här ska gå före arbetet med den gamla 128 × 32-displayen. Se `docs/FUTURE-DMD-EXTENSIONS.md` när spåret återupptas.

- [x] Lägg till DMD Extensions som beständig teknisk referens
- [x] Dokumentera ColorizingDMD-guidens Serum-flöde som framtida referens
- [x] Bevara den isolerade prototypen för syntetiska dumps, 6-bitars palett, maskmatchning och monokrom fallback som vilande referens
- [ ] Utred GPL-2.0 och välj en säker integrationsgräns innan kod eller binärer tas in
- [ ] Gör generell bildstorlek, stride och pixel-/färgformat explicita i ett versionsmärkt `DmdFrame`
- [ ] Lägg till indexerad färg och RGB24 utan att påverka monokrom uppspelning
- [ ] Testa 128 × 32, 192 × 64 och 256 × 64 med fit, fill, stretch och heltalsskalning
- [ ] Utred Serum-, cRom-, VNI-, PAL- och PAC-färgläggning
- [ ] Implementera dynamiska palettset, områdesmasker, bakgrunder, färgrotationer och sprites
- [ ] Lägg till licens- och upphovsmetadata per lokalt tillagd färgläggning
- [ ] Verifiera framtida färgrendering mot DMD Extensions i en separat integrationstestprofil
- [ ] Utred nätverksadapter och fysisk DMD-utmatning först efter att den gamla displayen är färdig

- [ ] Egna namngivna spellistor utöver de inbyggda urvalslägena
- [ ] Favoritmarkering utöver den obligatoriska aktiverings-/blockeringslistan
- [ ] Schema för dag- och nattljusstyrka
- [ ] DMD-effekter som glöd, scanlines och färgpaletter
- [ ] Stöd för GIF och MP4 utöver `.scn`
- [ ] Fjärrkontroll från mobil via lokalt webbgränssnitt
- [ ] Stöd för fysisk LED-matris eller Pin2DMD
- [ ] Skärmsläckarläge för Windows

## Ytterligare funktionsförslag

### Klocka och automatisk visning

- [ ] Flera klocklayouter med eller utan sekunder och datum
- [ ] Visa sekunder som siffror, blinkande kolon eller inte alls
- [ ] Valbara övergångar mellan klocka och animation: direkt byte, toning eller DMD-upplösning
- [ ] Schema för när displayen ska vara aktiv, nedtonad eller helt svart
- [ ] Separat ljusstyrka för dag och natt
- [ ] Förhindra inbränning genom små positionsbyten och varierad klocklayout
- [ ] Stöd för flera tidszoner som kan växla automatiskt

### Animationer och bibliotek

- [ ] Undvik att samma animation spelas igen innan övriga i urvalet har visats
- [ ] Uppspelningshistorik med senast visade spel och animationer
- [ ] Tillfällig `Visa inte igen` direkt från högerklicksmenyn
- [ ] Regler för minsta och högsta visningstid samt antal repetitioner per animation
- [ ] Möjlighet att låta korta animationer loopa ett bestämt antal gånger
- [ ] Egna etiketter och korrigering av tillverkare, spel och animationsnamn
- [ ] Miniatyrbilder eller första bildrutan i biblioteket
- [ ] Upptäck nya, ändrade och borttagna filer när biblioteket skannas om
- [ ] Visa dubbletter och låt användaren välja vilken kopia som ska användas

### Bild och skärm

- [ ] Valbar punktform, punktstorlek, mellanrum och glödstyrka
- [ ] Färgpalett globalt, per tillverkare eller per spel
- [x] Snabbval för klassisk orange, röd, plasma och monokrom
- [ ] Pixelperfekt heltalsskalning när skärmens storlek tillåter det
- [ ] Automatisk återställning till rätt bildskärm om en skärm kopplas bort
- [ ] Separata inställningar för varje ansluten bildskärm
- [ ] Dold muspekare efter några sekunders inaktivitet i helskärm
- [ ] Framtid: stöd större DMD-ytor och fullfärg enligt planen för DMD Extensions

### Drift och säkerhet

- [ ] Systemfältsikon med spela/pausa, visa klocka, nästa animation och avsluta
- [ ] Återuppta senaste läge efter omstart eller strömavbrott
- [ ] Exportera och importera inställningar och aktiveringslistor
- [ ] Automatisk säkerhetskopia av inställningar före större uppdateringar
- [ ] Biblioteksrapport över fungerande, trasiga och avstängda filer
- [ ] Diagnostiksida med aktuell fil, bildfrekvens, upplösning och avkodningsfel
- [ ] Tyst felläge i helskärm: hoppa över felet och skriv det i loggen utan dialogruta

### Utanför projektets omfattning

- [x] Ingen ljuduppspelning eller ljudhantering

## Egna anteckningar
