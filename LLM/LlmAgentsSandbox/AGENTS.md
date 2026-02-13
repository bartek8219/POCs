# Wytyczne dla Repozytorium

## Struktura projektu i organizacja modulow

- Kod zrodlowy znajduje sie w katalogu glownym oraz uporzadkowanych podkatalogach (`Agents/`, `Services/`, `Tools/`, `Configuration/`).
- Testy umieszczaj w katalogu `tests/` w glownym folderze projektu.
- Pliki konfiguracyjne (np. `appsettings.json`, `.gitignore`) trzymaj w katalogu glownym.
- Dokumentacja: `README.md` oraz `AGENTS.md`.

## Komendy budowania, testowania i rozwoju

- `dotnet restore`: przywraca zaleznosci NuGet.
- `dotnet build LlmAgentsSandbox.sln`: buduje rozwiazanie.
- `dotnet run --project LlmAgentsSandbox.csproj`: uruchamia aplikacje lokalnie.
- `dotnet test`: uruchamia testy (gdy projekt testowy jest dostepny).

## Styl kodowania i konwencje nazewnicze

- Stosuj wciecia na 4 spacje (bez tabulatorow).
- Dla C# stosuj standard .NET i czytelne, krotkie nazwy.
- Nazwy metod i wlasciwosci: `PascalCase`.
- Nazwy pol prywatnych: `_camelCase`.
- Nazwy katalogow i plikow utrzymuj spojne z konwencja projektu.
- Przed commitem uruchom `dotnet format` (jesli skonfigurowane) oraz build/test.

## Wytyczne dla testowania

- Testy pisz w `xUnit` lub `NUnit` i umieszczaj w osobnym projekcie testowym.
- Nazwy plikow testowych: np. `TruthTellerAgentTests.cs`.
- Do kazdej nowej funkcjonalnosci/bledu dodawaj odpowiednie testy.
- Daz do wysokiego pokrycia kodu dla logiki biznesowej i resolverow tooli.

## Wytyczne dotyczace commitow i pull requestow

- Tworz zwiezle, opisowe komunikaty commitow (np. `fix: poprawa obslugi tool call`, `feat: dodanie weather tool`).
- Lacz powiazane zmiany w jeden commit, gdzie to mozliwe.
- Pull request powinien zawierac opis zmian, zakres testow oraz powiazane zgloszenia (`Closes #numer`).
- Przed zgloszeniem PR sprawdz, czy build i testy przechodza bez bledow.

## Wskazowki bezpieczenstwa i konfiguracji

- Nie commituj kluczy API, hasel ani innych danych wrazliwych.
- Trzymaj lokalne sekrety w `appsettings.Development.json` (plik lokalny, ignorowany przez `.gitignore`).
- Commituj jedynie szablony konfiguracji, np. `appsettings.Development.example.json`.
- Uzywaj zmiennych srodowiskowych lub dedykowanego mechanizmu sekretow.
- Traktuj logi i output narzedzi jako potencjalnie wrazliwe dane.

---

W razie pytan zajrzyj do `README.md` lub skontaktuj sie z maintainerem.
