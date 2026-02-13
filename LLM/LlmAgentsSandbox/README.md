# LlmAgentsSandbox

Proof of Concept (POC) agentow LLM w .NET. Aplikacja konsolowa uruchamia przykladowego agenta i pokazuje integracje modelu z narzedziami (tool calling) przez OpenAI SDK.

## Najwazniejsze elementy

- Architektura agentow: `IAgent` i `BaseAgent` w `Agents/Core/` oraz implementacje w `Agents/`.
- Serwis LLM: `OpenAIService` obsluguje chat completion, przekazywanie tooli i obsluge tool call.
- Rejestr narzedzi: `ToolRegistry` pozwala laczyc toole globalne i lokalne (per agent).
- Definicje tooli: `Tools/CalculatorToolDefinition.cs`, `Tools/WeatherToolDefinition.cs`.
- Konfiguracja: `appsettings.json` (ustawienia bazowe) i `appsettings.Development.example.json` (wzor konfiguracji lokalnej).

## Wymagania

- .NET SDK 9.0
- Dostep do OpenAI lub Azure OpenAI (endpoint, deployment/model, API key)

## Konfiguracja

1. Skopiuj plik `appsettings.Development.example.json` do `appsettings.Development.json`.
2. Ustaw lokalnie `Endpoint` i `ApiKey` w `appsettings.Development.json` lub przez zmienne srodowiskowe.
3. Ustaw `DOTNET_ENVIRONMENT=Development` przy uruchamianiu lokalnym.

## Uruchomienie

```powershell
dotnet restore
dotnet build LlmAgentsSandbox.sln
dotnet run --project LlmAgentsSandbox.csproj
```

## Jak to dziala

- `Program.cs` buduje konfiguracje, rejestruje serwisy i uruchamia agenta.
- `TruthTellerAgent` wysyla `SystemPrompt` i `UserPrompt`, przekazuje zestaw tooli i resolver.
- `OpenAIService`:
  - wysyla pierwsze zapytanie,
  - obsluguje `tool_calls` (jesli wystapia),
  - wysyla wynik narzedzia do modelu,
  - zwraca finalna odpowiedz tekstowa.

## Struktura projektu

- `Program.cs` - start aplikacji i konfiguracja DI
- `Agents/Core/` - interfejs i klasa bazowa agenta
- `Agents/` - implementacje agentow
- `Services/` - serwisy aplikacyjne (`ILlmService`, `OpenAIService`, `ToolRegistry`)
- `Tools/` - definicje i handlery narzedzi
- `Configuration/` - klasy konfiguracji
- `appsettings.json` - konfiguracja bazowa aplikacji (bez sekretow)
- `appsettings.Development.example.json` - przyklad lokalnego override sekcji `OpenAI`

## Dodawanie nowego agenta

1. Dodaj klase agenta w `Agents/` dziedziczaca po `BaseAgent`.
2. Zarejestruj agenta w `ConfigureServices` w `Program.cs`.
3. Dodaj metode uruchamiajaca analogicznie do `RunTruthTellerAgentExample`.

## Dodawanie nowego toola

1. Dodaj definicje toola i handler w `Tools/`.
2. Zarejestruj tool globalnie w `Program.cs` przez `ToolRegistry`, albo lokalnie w agencie przez `CreateSession(...)`.

## Bezpieczenstwo

- Nie commituj prawdziwych kluczy API.
- Nie commituj `appsettings.Development.json` (plik lokalny; ignorowany przez `.gitignore`).
- Preferuj zmienne srodowiskowe lub bezpieczny storage sekretow.

## Status

Projekt jest POC i sluzy do eksperymentow z agentami, promptami i tool calling.
