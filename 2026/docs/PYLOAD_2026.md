# PYLOAD 2026

Guida operativa per usare la documentazione ZWCAD 2026 come riferimento di progetto per `PYLOAD`, mantenendo chiara la distinzione tra:

- stato reale e validato del bridge attuale
- API piu recenti disponibili nella reference 2026
- strategia consigliata per una vera variante o porting 2026

## 1. Stato attuale del progetto

`PYLOAD` e nato e si e consolidato come bridge C# + IronPython per ZWCAD 2015.  
Durante lo sviluppo sono stati validati sul CAD reale molti settori importanti:

- runtime `PYLOAD` e loader script Python
- helper comandi, LISP e shell transcript
- geometria 2D low-level
- famiglia DXF-like `EntMake / EntGet / EntMod`
- filtri DXF-style e traversal database-style
- blocchi, attributi e batch su block reference
- comandi di modifica geometrica
- database avanzato con `DBDictionary`, `XRecord`, extension dictionary e metadata persistenti

Questa base resta il punto di partenza piu affidabile anche per una futura variante 2026.

## 2. Documentazione 2026 disponibile

Nella cartella:

- `C:\Users\user\Desktop\PYLOAD\Versione2026`

sono presenti:

- `ZRX_Mgd.chm`
- `ZWCAD_ZRX_Reference_eng_2026.chm`
- `extracted_mgd`
- `extracted_ref`

La parte `.NET` estratta mostra chiaramente che il modello API resta centrato sugli assembly e namespace gia familiari:

- `ZwDatabaseMgd`
- `ZwSoft.ZwCAD.DatabaseServices`
- `ZwSoft.ZwCAD.Geometry`
- `ZwSoft.ZwCAD.ApplicationServices`
- `ZwSoft.ZwCAD.EditorInput`

Questa continuita e molto utile: la reference 2026 puo essere usata come inventario e guida progettuale senza ripartire da zero.

## 3. Come leggere correttamente la reference 2026

Per `PYLOAD`, la documentazione 2026 va trattata cosi:

1. come mappa funzionale delle famiglie API disponibili
2. come guida ai nomi di classi, proprieta e metodi
3. non come garanzia di compatibilita automatica con la build 2015

In pratica:

- se una API compare nella guida 2026, e un buon candidato
- prima di adottarla davvero nel bridge bisogna verificare che esista anche nelle DLL target
- se non esiste o si comporta diversamente, il bridge deve fare fallback o degradazione controllata

Questo e esattamente l’approccio che ha funzionato finora.

## 4. Cosa la guida 2026 conferma gia bene per PYLOAD

La reference 2026 conferma che il modello a oggetti su cui abbiamo gia investito e corretto:

- `DBDictionary` e gestione dizionari nominati
- `XRecord` e metadata persistenti
- `BlockReference`, `AttributeDefinition`, `AttributeReference`
- `DBText`, `MText`, `Hatch`, `Polyline`, `Region`
- tabelle e record come `TextStyleTableRecord`, `ViewTableRecord`, `UcsTableRecord`
- aree piu avanzate come `Surface`, `Solid3d`, `Viewport`, `PlotSettings`

Quindi il lavoro gia fatto non e “solo per 2015”: e coerente anche con l’architettura 2026.

## 5. Aree 2026 interessanti emerse dalla documentazione

Dalla reference 2026 si vedono chiaramente anche alcuni settori che possiamo ampliare in modo sensato.

### 5.1 Database e metadata

La guida 2026 conferma che questo settore resta molto importante:

- `DBDictionary`
- `XRecord`
- summary info e custom properties
- ownership e strutture interne del database

Per `PYLOAD` questo significa che il settore `database avanzato` che abbiamo appena aperto e strategicamente giusto.

### 5.2 Blocchi e insert piu ricchi

Nella documentazione 2026 compaiono anche tipi piu ricchi legati agli insert, ad esempio:

- `MInsertBlock`

Questo suggerisce una futura estensione del bridge verso insert multipli, array e gestione piu completa dei riferimenti blocco.

### 5.3 Testo e stili

La guida 2026 conferma profondita maggiore su:

- `TextStyleTableRecord`
- font descriptor
- proprieta avanzate su testo e stili

Questa e una buona direzione per batch di normalizzazione DWG e pulizia standard grafici.

### 5.4 Plot, views e presentazione

La reference 2026 mostra chiaramente anche:

- `PlotSettings`
- `ViewTableRecord`
- `Viewport`
- `UcsTableRecord`

Quindi il settore `Views / UCS / Viewports` e un candidato molto forte per una prossima espansione.

### 5.5 3D e sezioni

Compaiono anche classi e metodi interessanti per evolvere il progetto oltre il 2D:

- `Surface`
- `Solid3d`
- operazioni di sezione come `CreateSectionObjects(...)`

Questa e una pista concreta per una futura variante `PYLOAD 3D`.

## 6. Strategia consigliata per una vera versione 2026

Se l’obiettivo e preparare una vera “edizione 2026” del bridge, la strategia che consiglio e questa.

### Fase A. Separare documentazione e runtime

Tenere distinti:

- bridge attuale validato sul CAD reale
- reference 2026 come sorgente di progetto

Questo documento serve proprio a evitare confusione tra “supportato davvero oggi” e “potenzialmente disponibile nel 2026”.

### Fase B. Creare una variante 2026 senza rompere la 2015

La soluzione migliore non e sostituire subito il runtime storico, ma creare una variante dedicata, ad esempio:

- `PYLOAD.2026.csproj`
- oppure una configurazione build separata

con differenze controllate su:

- riferimenti DLL
- piattaforma
- eventuali API disponibili solo nel 2026

### Fase C. Portare per famiglie, non per file sparsi

L’ordine migliore resta:

1. `Commands / Editor / Prompt`
2. `DatabaseServices` base
3. `Text / Blocks / Attributes`
4. `DXF-like bridge`
5. `Modify`
6. `Views / UCS / Viewports`
7. `3D`

Questo approccio ha gia funzionato molto bene nel ramo storico.

## 7. Settori del bridge che conviene portare per primi al 2026

Se vogliamo una roadmap concreta, partirei cosi.

### Priorita alta

- `database avanzato`
- `blocchi / attributi`
- `commands / lisp / shell`
- `DXF-like`

Sono i settori che danno piu potenza pratica e dove la continuita architetturale tra 2015 e 2026 sembra piu forte.

### Priorita media

- `Views / UCS / Viewports`
- `batch testo avanzato`
- `XRef`

### Priorita successiva

- `3D solids`
- `Surface`
- sezioni e strumenti di presentazione avanzata

## 8. Rischi da tenere presenti

La guida 2026 e preziosa, ma non elimina alcuni rischi tipici:

- proprieta presenti in doc ma non scrivibili a runtime
- membri rinominati o ridefiniti
- differenze di comportamento su comandi e transazioni
- operazioni che sulla 2015 richiedevano fallback manuali ma nel 2026 potrebbero essere native, o viceversa

Per questo `PYLOAD` deve continuare a privilegiare:

- reflection prudente dove serve
- fallback manuali
- test mirati dentro ZWCAD reale

## 9. Raccomandazione pratica

La reference 2026 non va vista come “nuova documentazione da leggere tutta”, ma come:

- catalogo delle famiglie API
- guida per scegliere i prossimi batch
- base per costruire una vera variante `PYLOAD 2026`

Il progetto oggi e gia abbastanza maturo da fare quel salto con ordine.

## 10. Prossimo passo consigliato

Se decidiamo di usare davvero questa documentazione come base di evoluzione, il passo tecnico migliore e uno di questi due:

1. creare una configurazione build separata per 2026
2. costruire prima una matrice di compatibilita `2015 vs 2026` per famiglie API

Tra i due, io consiglio di partire dalla matrice di compatibilita:

- piu veloce
- meno rischiosa
- utilissima per decidere dove investire il lavoro successivo

## 11. Variante pronta nel repository

Per spostare davvero il lavoro sul runtime 2026, nel repository conviene mantenere una variante dedicata:

- progetto separato `PYLOAD.2026.csproj`
- build dedicata tramite `build_2026.ps1`
- smoke test ampio `test_master_2026.py`

Questa soluzione evita di rompere il ramo storico 2015 e rende piu semplice fare confronti reali tra:

- build storica
- build 2026
- comportamento dei test sul CAD reale

---

## Nota finale

Questo documento descrive una versione aggiornata del progetto in ottica 2026, non certifica automaticamente che tutte le API della reference 2026 siano gia operative nel runtime corrente del bridge.

La regola migliore resta:

- la guida 2026 orienta
- il codice compila
- ZWCAD reale conferma
