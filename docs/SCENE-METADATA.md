# Scene metadata

`scenes/scene-metadata.json` kompletterar SCN-filerna med uppgifter som inte finns i själva SCN-formatet. Filen är versionsmärkt och kan redigeras utan att appen byggs om.

```json
{
  "schemaVersion": 1,
  "prefixes": [
    { "prefix": "got", "game": "Game of Thrones", "manufacturer": "Stern", "year": 2015,
      "dateManufactured": "2015", "players": 4, "machineType": "SS", "theme": "Licensed Theme" }
  ],
  "files": [
    { "path": "got01.scn", "title": "Valfri scenrubrik" }
  ]
}
```

En exakt post under `files` har företräde framför en prefixregel. Sökvägar är relativa till den valda animationsmappen och `/` används även på Windows. Fälten `title`, `game`, `manufacturer`, `year`, `dateManufactured`, `players`, `machineType` och `theme` är valfria. Okända filer fortsätter fungera och visas med sitt filnamn.

Metadata ska bara läggas till när uppgiften är säker. Appen ska inte gissa spel för generiska namn såsom `RD0001.scn`.
