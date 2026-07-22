# Syntetiskt frame-dump-format

Detta är ett litet, versionsmärkt DMDClock-testformat. Det är inte ett påstående om full kompatibilitet med alla VPinMAME-dumpvarianter. Testfilerna är egenproducerade och innehåller ingen ROM-data.

```text
DMD-DUMP 1 width=4 height=2 bpp=2
FRAME timestampMs=0
0123
3210
FRAME timestampMs=40
0123
3010
```

- Version 1 stöder 2- och 4-bitars monokroma källramar.
- Varje pixel skrivs som ett hexadecimalt tecken.
- Varje ram har en icke-negativ tidskod i millisekunder.
- Tidskoder måste vara i stigande ordning.
- Tomma rader och rader som börjar med `#` ignoreras mellan poster.

Formatet används för reproducerbara tester av parsing, maskmatchning, palettfärgläggning och fallback. En framtida VPinMAME-importör ska konvertera externa dumpar till samma interna `DmdFrameDump`-modell.
