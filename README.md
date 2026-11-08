# Dag 9 · Sikkerhed og adgang til API’et

I afprøver HTTPS, login og roller i det kendte bogkatalog. Bøgerne ligger stadig i en JSON-fil i et named volume. Der er ingen database og ingen nye NuGet-pakker.

## Mål

- I kan forklare, hvilken forbindelse HTTPS beskytter.
- I kan skelne mellem authentication (hvem kontoen er) og authorization (hvad kontoen må).
- I kan afprøve forskellen på 401, 403 og et vellykket kald.
- I kan holde lokale adgangskoder, tokens og certifikater ude af Git.

## 1. Klargøring i Visual Studio

Hent hele repoet og åbn `Bogkatalog.slnx`. Start Docker Desktop med Linux-containere. Begge programmer skal køre i Docker i denne øvelse. I skal ikke starte dem med Ctrl+F5.

Åbn **View > Terminal** (Developer PowerShell). Kør:

```powershell
dir
```

`dir` viser den aktuelle mappes filer. I skal kunne se `compose.yaml`, `Forbered-HTTPS.ps1` og `Bogkatalog.slnx`. Hvis I er i en anden mappe, navigerer I til repoets rod. Alle følgende kommandoer køres herfra. Kontrollér også, at I åbner filerne fra samme repo og ikke en backup.

```powershell
.\Forbered-HTTPS.ps1
```

`.\` betyder den aktuelle mappe. Scriptet laver `.env` med tre tilfældige lokale værdier og eksporterer et udviklingscertifikat til `.certs/localhost.pfx`. Eksisterende `.env` bliver bevaret. Scriptet ændrer ikke Windows' tillid til certifikater. Åbn `.env` i editoren for at se de lokale øvelsesadgangskoder. Del ikke filen. Brug aldrig en rigtig privat adgangskode til øvelsen.

```powershell
dotnet dev-certs https --trust
```

`dev-certs https` arbejder med .NETs lokale udviklingscertifikat. `--trust` beder om, at pc'en har tillid til certifikatet. Windows kan vise en godkendelsesdialog. Kør som jeres normale bruger. Hvis skolens politik blokerer script eller certifikatgodkendelse, få hjælp fra underviser/IT. Slå ikke certifikatkontrol fra i browser eller C#-kode.

Hvis eksporten eller tilliden ændres senere: Kør scriptet igen, så PFX og den godkendte udviklingsidentitet passer sammen. Genopret ikke `.env` unødigt, da adgangskoderne så skifter.

## Hvis PowerShell blokerer scriptet

Hvis fejlen siger `running scripts is disabled on this system`, køres disse to linjer i samme terminal, én ad gangen:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy RemoteSigned
.\Forbered-HTTPS.ps1
```

Svar `Y`, hvis PowerShell spørger om bekræftelse. `Set-ExecutionPolicy` ændrer reglerne for scriptkørsel. `-Scope Process` betyder kun denne terminal, indtil den lukkes. `RemoteSigned` tillader lokale scripts; scripts markeret som hentet fra internettet kræver normalt en signatur. Kommandoen kræver ikke administratorrettigheder.

Hvis scriptet stadig bliver blokeret, så sig til, så kigger vi på det sammen.

Når scriptet lykkes, fortsætter I med:

```powershell
dotnet dev-certs https --trust
docker compose up -d --build
```

Godkend Windows-dialogen om udviklingscertifikatet, hvis den vises. Kontrollér derefter `https://localhost:5443/health` (status ok) og `https://localhost:5143` (loginformular).

Kilde: [Microsoft: Set-ExecutionPolicy](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy?view=powershell-5.1).

## 2. Start begge containere

```powershell
docker compose up -d --build
```

- `docker compose` bruger `compose.yaml` i mappen.
- `up` opretter/starter de nødvendige containere og netværk.
- `-d` er detached: programmerne kører i baggrunden, og terminalen bliver fri.
- `--build` bygger images, så ændret C#-kode kommer med.

Åbn:

- **API-kontrol:** `https://localhost:5443/health` forventes at vise `status: ok`.
- **Web:** `https://localhost:5143` forventes at vise loginformularen.

Brug `localhost`, som certifikatet er udstedt til. En LAN-IP eller `api` i browseren passer ikke til dette certifikat. Certifikatfejl skal undersøges, ikke klikkes væk. Firefox kan have en anden lokal tillidsopsætning end Windows-baserede clients. Få hjælp eller brug en client, som korrekt har tillid til udviklingscertifikatet.

Host-portene er kun bundet til `127.0.0.1`. Inde i Docker bruger Web `http://api:8080/`. **Det interne hop er HTTP og ikke krypteret i denne enkelt-pc-øvelse.** Browserens HTTPS-symbol betyder ikke, at alle hop er beskyttet. Vi skal vurdere og beskytte den interne transport før rigtig drift på tværs af værter eller i et netværk uden tilstrækkelig tillid.

## 3. Private requests

Find `src/Bogkatalog.Api/Bogkatalog.Api.http`. Gem en kopi i samme mappe med navnet **Dag09.private.http** og åbn kopien i Visual Studio. Alle filer med endelsen `.private.http` er udeladt fra Git og Docker-build.

Indsæt kun adgangskoder og tokens i den private kopi. Skabelonen i repoet skal beholde sine pladsholdere. `@baseUrl` er `https://localhost:5443`.

| Variabel i den private fil | Værdi |
|---|---|
| `readerPassword` | `READER_PASSWORD` fra .env |
| `editorPassword` | `EDITOR_PASSWORD` fra .env |
| `readerToken` | `accessToken` fra B1 |
| `editorToken` | `accessToken` fra C1 |
| `bookId` | Det faktiske id fra C2 |

Kopiér tokenets værdi uden JSON-feltnavn og uden anførselstegn. Send Request-linket gælder den request, der står under det. Lad den tomme linje mellem headers og JSON stå.

## 4. Grønne øvelser

**Grøn A:** A0 giver 200. A1 uden token og A2 med forkert password giver 401. Kig også på `WWW-Authenticate: Bearer` ved et beskyttet kald uden login.

**Grøn B:** Send B1, kopiér accessToken og send B2. Boglisten giver 200. D4 med opdigtet token giver 401. `reader` får rollen Reader, og `editor` får Editor. Rollen kommer fra serveren.

**Grøn C / TODO A:** Find slutningen på POST-handleren i API'ets `Program.cs`. Knyt den eksisterende policy til endpointet:

```csharp
}).RequireAuthorization("WriteBooks");
```

I startprojektet kræver boggruppen allerede ReadBooks. POST mangler med vilje den strengere regel. **Før rettelsen kan en læser oprette. Det er fejlen, I skal lukke.** PUT og DELETE er allerede beskyttet. I behøver ikke udføre den forkerte oprettelse for at løse øvelsen.

Gem, kør `docker compose up -d --build`, og log ind igen med B1/C1. Opdatér begge tokens. B3 som læser skal nu give **403**. C2 som redaktør skal give **201**, Location og JSON med det faktisk tildelte id. Brug id'et i C3, som skal give 200. C4 giver 400 uden at oprette noget. Tidligere testoprettelser kan have ændret antallet og næste id.

**Grøn D / TODO B:** Find `CreateClient()` i Webs `Services/BookApiClient.cs`. Indsæt følgende inde i den forberedte if-blok:

```csharp
http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);
```

Headeren føjes kun til den nye HttpClient, som bruger den aktuelle sessions token. Brug ikke en statisk, fælles tokenvariabel. Login-koden sender allerede sit token til `/auth/me`. Bogkaldene mangler headeren indtil TODO B er løst.

Gem og kør `docker compose up -d --build`. Genindlæs Web med Ctrl+R. Log ind som reader, vælg Hent bøger og afprøv oprettelse. GET virker, POST viser 403. Formularen er med vilje synlig for begge roller, så API'ets regel kan testes.

**Grøn E:** Log ud og ind som editor. Opret en testbog. Antallet skal vokse. Log ud, og vælg Hent bøger igen. Det nye kald giver 401. Den tidligere brugers bogliste er ryddet fra Web.

**Grøn F:** Kontrollér .gitignore, .dockerignore og GitHub Desktops Changes. `.env`, `.certs` og `Dag09.private.http` må ikke vises som filer til commit. En ignore-regel fjerner ikke filer, som allerede er tracked.

```powershell
docker compose config --quiet
```

`config` kontrollerer og behandler konfigurationen. `--quiet` undlader at udskrive den udfyldte konfiguration med hemmelige værdier. Undlad at dele en fuld konfigurationsudskrift, containerens miljø eller token-svar.

## 5. Gul og rød

**Gul A:** Brug D1 og D2 til at afprøve læserens PUT/DELETE. Begge giver 403, og bogen er uændret. D3 som redaktør giver 204 første gang og 404 næste gang.

**Gul B:** Et token i den private .http-fil kan stadig virke, når I logger ud af Web. Log ud rydder kun Webs egen session. Et kopieret token bliver ikke tilbagekaldt og kan bruges indtil udløb. Forklar begrænsningen.

**Rød:** Vurder en offentlig udgave til en forening. Udpeg tre konkrete problemer ved den lokale demo og foreslå ændringer. Begrund både beskyttelsen og driftsansvaret. Der skal ikke implementeres en ny platform.

## 6. Afgrænsning og forberedt sikkerhed

- ASP.NET Cores `AddBearerToken` beskytter og validerer tokens. Formatet er ikke JWT. Vi skriver ikke vores egen kryptografi.
- Login udsteder et access token, som gælder 15 minutter. Svaret indeholder også frameworkets refresh token, men øvelsen har intet refresh-endpoint og bruger det ikke. Log ind igen efter udløb.
- To lokale konti erstatter en rigtig brugeradministration. `PasswordHasher` laver saltede passwordhashes i hukommelsen ved start. Konfigurationen indeholder stadig de oprindelige lokale adgangskoder.
- Rate limit: Højst 20 loginrequests pr. minut, fælles for denne API-proces. 429 betyder, at I skal vente. Begrænsningen er enkel og ikke et fuldt misbrugsforsvar.
- Web holder access token i en scoped ApiSession på Web-serveren. Der bruges ikke localStorage. Genindlæsning giver ny Blazor-session og nyt login.
- Webs Log ud rydder kun sessionen. Et kopieret token kan fortsat virke. Der er ikke serverbaseret tilbagekaldelse i denne demo.
- Udskiftning af API-containeren kan miste Data Protection-nøglerne og gøre gamle tokens ugyldige. Stop/start af samme container er noget andet. Bogfilen ligger separat i bookdata-volume.
- Et rigtigt system kræver blandt andet en gennemprøvet identitetsløsning, passende kontosikkerhed, beskyttelse af intern transport og hemmeligheder samt plan for backup, nøglehåndtering, opdateringer og overvågning.
- Én API-proces skriver til JSON-filen. Flere processer mod samme fil understøttes ikke. Et volume er ikke en backup.
- .env er tekst, og miljøvariabler kan læses af personer med adgang til containerdriften. Ved rigtig drift vælges en passende secret-tjeneste og adgangskontrol.

## 7. Fejlfinding og afslutning

| Symptom | Undersøg |
|---|---|
| Certifikatfejl | Tillid, værtsnavn localhost, gyldighed og om den eksporterede PFX passer til det godkendte certifikat |
| 401 | Manglende, ugyldigt eller udløbet token, en gammel container eller manglende Authorization-header |
| 403 | Den faktiske rolle og endpointets policy |
| 429 | Vent et minut efter mange loginforsøg |
| Forbindelsesfejl | Containerstatus, adresse og porte |
| YAML-fejl | Indrykning med mellemrum, korrekt fil og gemte ændringer |

```powershell
docker compose ps -a
docker compose logs --tail 40 api web
```

`ps` kommer fra process status og viser projektets containere. `-a` viser alle, også stoppede. `logs` viser logbeskeder. `--tail 40` tager de sidste 40 linjer fra hver angivet service. `api web` vælger de to services.

Efter en C#-ændring bruges `docker compose up -d --build`. Efter en ren Compose/miljøændring er `docker compose up -d` normalt nok. Et almindeligt restart indlæser ikke ny konfiguration eller nyt image. Efter udskiftning af Web-containeren skal browseren genindlæses. Hvis Blazor viser Rejoining the server, så brug Ctrl+R, når Web er startet.

```powershell
docker compose down
```

`down` stopper og fjerner projektets containere og netværk. Det navngivne bookdata-volume bevares. Tilføj ikke `--volumes`, medmindre I bevidst vil slette de gemte øvelsesdata.

## Kilder

- https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html
- https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html
- https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html
- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/aspnet/core/security/docker-https?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/azure/security/fundamentals/shared-responsibility
- https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.bearertokenextensions.addbearertoken
- https://www.rfc-editor.org/rfc/rfc6750
