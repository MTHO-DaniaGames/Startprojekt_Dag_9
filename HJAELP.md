# Hjælp · Dag 9

- **Rigtig mappe:** Kør `dir` i Developer PowerShell. Den viser filerne. Find compose.yaml, Bogkatalog.slnx og Forbered-HTTPS.ps1. Alle ændringer og kommandoer skal gælde samme kopi.
- **Lokal opsætning:** Kør `.\Forbered-HTTPS.ps1` og derefter `dotnet dev-certs https --trust`. Første kommando genererer lokale værdier og eksporterer certifikatet. `--trust` beder om lokal tillid. Få hjælp, hvis skolens politik blokerer godkendelsen.
- **Adresser:** Web: https://localhost:5143. API: https://localhost:5443. Brug localhost, som certifikatet gælder for. Det interne HTTP-hop bruger http://api:8080/ i Docker.
- **401:** Login mangler, tokenet er ugyldigt/udløbet, eller Authorization-headeren mangler. Log ind igen, hvis API-containeren er udskiftet. Kontrollér TODO B, hvis Web kan logge ind, men ikke hente bøger.
- **403:** Kontoen er kendt, men endpointet kræver en anden rolle. En læser må ikke oprette, ændre eller slette efter TODO A.
- **429:** For mange loginforsøg. Vent et minut. Øvelsens grænse er 20 loginrequests pr. minut for hele API-processen.
- **TODO A:** Knyt den eksisterende WriteBooks-policy til POST-handleren. PUT og DELETE har allerede reglen.
- **TODO B:** Tilføj `http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);` i den forberedte if-blok i CreateClient.
- **Ændringer slår ikke igennem:** Gem C#-filen og kør `docker compose up -d --build`. `up` starter, `-d` kører i baggrunden, og `--build` bygger images først. Genindlæs Web med Ctrl+R efter udskiftning af Web-containeren.
- **Privat requestfil:** Gem en kopi som Dag09.private.http i API-mappen. Indsæt kun koder og tokens i kopien. Brug accessToken fra login-svaret, uden anførselstegn. Log ind igen efter 15 minutter.
- **Ny bog mangler på første side:** Listen er sorteret efter id. Brug Næste eller hent med det faktiske id fra oprettelsen. Et named volume bevarer tidligere testdata, så næste id er ikke altid 7.
- **Log ud:** Web fjerner sit token og sin viste liste. Det tilbagekalder ikke et token, der er kopieret til en anden client. Genindlæsning kræver nyt login.

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

## Status og logs

`docker compose ps -a` viser containernes status. ps kommer fra process status. -a medtager også stoppede containere.

`docker compose logs --tail 40 api web` viser de sidste 40 loglinjer fra hver af de to services. logs viser beskeder, --tail begrænser antallet, og api web vælger services. Del ikke adgangskoder, tokens eller containerens miljø.

Data Protection kan skrive, at nøgler i containeren ikke er krypteret på disken. Det er en reel begrænsning i den lokale demo. Nøglerne ligger i containerens filsystem og er ikke bogdata i bookdata-volume. Ved rigtig drift skal nøglernes opbevaring og adgang beskyttes. Der kan også stå, at HTTP_PORTS bliver tilsidesat af URLS. Her er det forventet, fordi compose.yaml angiver både HTTP- og HTTPS-listeneren med URLS.

## Afslutning

`docker compose down` stopper og fjerner projektets containere og netværk. Det navngivne bookdata-volume bevares. Brug ikke --volumes, medmindre I bevidst vil slette de gemte øvelsesdata.

Se README for den samlede fremgangsmåde og sikkerhedsafgrænsningen.
