# POC: RabbitMQ vs Kafka w .NET C#

## Przegląd projektu

Ten POC (Proof of Concept) demonstruje porównanie RabbitMQ i Kafka w komunikacji aplikacji .NET C# z uwzględnieniem wzorców **fan-out**, **request/reply** oraz **prostych producentów i konsumentów**. Projekt zawiera warstwę abstrakcji umożliwiającą łatwe przełączanie między implementacjami.

## Struktura projektu

```
MessagingPOC/
├── docker-compose.yml                          # Infrastruktura (RabbitMQ + Kafka)
├── src/
│   ├── MessagingPOC.Abstractions/             # Warstwa abstrakcji
│   │   ├── IMessageProducer.cs
│   │   ├── IMessageConsumer.cs
│   │   ├── IRequestReplyClient.cs
│   │   └── MessageEnvelope.cs
│   ├── MessagingPOC.RabbitMQ/                 # Implementacja RabbitMQ
│   │   ├── RabbitMQProducer.cs
│   │   ├── RabbitMQConsumer.cs
│   │   ├── RabbitMQRequestReplyClient.cs
│   │   └── RabbitMQConfiguration.cs
│   ├── MessagingPOC.Kafka/                    # Implementacja Kafka
│   │   ├── KafkaProducer.cs
│   │   ├── KafkaConsumer.cs
│   │   ├── KafkaRequestReplyClient.cs
│   │   └── KafkaConfiguration.cs
│   ├── MessagingPOC.Server/                   # Aplikacja serwera (producent)
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Controllers/MessageController.cs
│   └── MessagingPOC.Client/                   # Aplikacja klienta (konsument)
│       ├── Program.cs
│       ├── appsettings.json
│       └── Workers/MessageWorker.cs
└── README.md
```

---

## 7. Porównanie: RabbitMQ vs Kafka

### 7.1 Model kolejkowania

**RabbitMQ:**
- Wykorzystuje **exchanges** i **queues** z bindingami
- Exchange types: direct, fanout, topic, headers
- Wiadomości są **usuwane po acknowledgment**
- Każdy consumer group ma swoją dedykowaną kolejkę

**Kafka:**
- Wykorzystuje **topics** i **partitions**
- Wiadomości są **retencjonowane** przez określony czas (domyślnie 7 dni)
- Każdy consumer w grupie czyta z przydzielonych partycji
- Offset tracking pozwala na "odtworzenie" wiadomości

### 7.2 Wzorzec Fan-out

**RabbitMQ:**
- Używa **Fanout Exchange**
- Każda grupa konsumentów ma własną kolejkę zbindowaną z exchange
- Automatyczne routowanie do wszystkich zbindowanych kolejek

**Kafka:**
- Naturalnie obsługiwany przez model topics
- Każdy consumer group czyta wszystkie wiadomości z topica
- Multiple consumer groups = fan-out

### 7.3 Request-Reply Pattern

**RabbitMQ:**
- Natywne wsparcie przez **reply-to** i **correlation-id**
- Używa czasowych kolejek do odpowiedzi
- Direct reply-to (amq.rabbitmq.reply-to) dla wydajności

**Kafka:**
- Wymaga implementacji przez dwa topics (request + reply)
- Tracking przez headers (CorrelationId, ReplyTo)
- Mniej naturalne niż w RabbitMQ

### 7.4 Wydajność

**RabbitMQ:**
- **Latencja**: 1-10ms
- **Throughput**: 10K-50K msg/s (zależnie od konfiguracji)
- Lepsze dla małych wiadomości i niskiej latencji
- Vertical scaling

**Kafka:**
- **Latencja**: 5-50ms
- **Throughput**: Miliony msg/s
- Lepsze dla dużych wolumenów danych
- Horizontal scaling

### 7.5 Trwałość (Durability)

**RabbitMQ:**
```csharp
// Durable queue
_channel.QueueDeclare(
    queue: "my-queue",
    durable: true,      // Queue przetrwa restart brokera
    exclusive: false,
    autoDelete: false);

// Persistent message
properties.Persistent = true;  // Message zapisane na dysk
```

**Kafka:**
```csharp
var config = new ProducerConfig
{
    Acks = Acks.All,  // Czekaj na wszystkie repliki
    EnableIdempotence = true
};

// Topics configuration
// replication.factor = 3
// min.insync.replicas = 2
```

### 7.6 Kiedy używać czego?

**RabbitMQ - Użyj gdy:**
- Potrzebujesz niskiej latencji (< 10ms)
- Złożony routing i wzorce (topic exchange, headers)
- Request-reply pattern jest kluczowy
- Moderate throughput (do 50K msg/s)
- Task queues i work distribution

**Kafka - Użyj gdy:**
- Wysokie throughput (> 100K msg/s)
- Event sourcing i replay wiadomości
- Stream processing
- Long-term message retention
- Log aggregation i analytics

---

## 8. Testowanie POC

### 8.1 Uruchomienie infrastruktury

```bash
# Start wszystkich serwisów
docker-compose up -d

# Sprawdź status
docker-compose ps

# Logi
docker-compose logs -f
```

### 8.2 Test z RabbitMQ

**Konfiguracja appsettings.json:**
```json
{
  "MessagingBroker": "RabbitMQ"
}
```

**Uruchom Server:**
```bash
cd MessagingPOC.Server
dotnet run
```

**Uruchom kilku Clientów (w osobnych terminalach):**
```bash
cd MessagingPOC.Client
dotnet run
```

**Testuj przez Swagger:**
- http://localhost:5000/swagger
- Wywołaj `POST /api/message/publish`
- Wywołaj `POST /api/message/publish-batch`
- Wywołaj `POST /api/message/request-reply`

**Obserwuj:**
- Logi klientów - wszystkie otrzymują wiadomości (fan-out)
- RabbitMQ UI: http://localhost:15672

### 8.3 Test z Kafka

**Zmień konfigurację:**
```json
{
  "MessagingBroker": "Kafka"
}
```

**Restart aplikacji i powtórz testy**

**Obserwuj:**
- Logi klientów
- Kafka UI: http://localhost:8080

### 8.4 Testy wydajnościowe

**Test throughput:**
```bash
# RabbitMQ - batch publish
curl -X POST http://localhost:5000/api/message/publish-batch \
  -H "Content-Type: application/json" \
  -d '{"count": 10000, "messagePrefix": "Perf Test"}'

# Kafka - to samo
```

**Monitoruj:**
- Czas przetworzenia
- Wykorzystanie CPU/RAM w Docker
- Logi konsumentów

---

## 9. Dobre praktyki

### 9.1 Warstwa abstrakcji
✅ **Używaj interfejsów** - łatwa podmiana implementacji
✅ **Dependency Injection** - konfiguracja w jednym miejscu
✅ **Configuration pattern** - appsettings.json dla środowisk

### 9.2 Error handling
✅ **RabbitMQ**: BasicNack z requeue dla retry
✅ **Kafka**: Manual commit - offset control
✅ **Dead Letter Queues** dla failed messages

### 9.3 Monitoring
✅ **Logging** - strukturyzowane logi (Serilog)
✅ **Metrics** - counter dla sent/received messages
✅ **Health checks** - broker availability

### 9.4 Bezpieczeństwo
✅ **Connection strings** - User Secrets / Environment Variables
✅ **SSL/TLS** - produkcyjne połączenia
✅ **Authentication** - credentials management

### 9.5 Performance
✅ **Connection pooling** - reuse connections
✅ **Batch publishing** - reduce network calls
✅ **Prefetch count** (RabbitMQ) - load balancing
✅ **Partition key** (Kafka) - distribution

---

## 10. Rozszerzenia POC

### 10.1 Dodatkowe wzorce
- **Competing Consumers** - multiple workers na tej samej kolejce
- **Message Priority** - priorytetyzacja wiadomości (RabbitMQ)
- **Dead Letter Exchange** - obsługa failed messages
- **Saga Pattern** - distributed transactions

### 10.2 Monitoring i observability
- **OpenTelemetry** - distributed tracing
- **Prometheus** - metrics collection
- **Grafana** - visualization dashboards

### 10.3 Resilience
- **Polly** - retry policies i circuit breakers
- **Transient fault handling**
- **Graceful shutdown** - finish processing before exit

### 10.4 Schema Registry
- **JSON Schema** - validation
- **Avro** (Kafka) - binary serialization
- **Versioning** - backward compatibility

---

## 11. NuGet Packages

```xml
<!-- Abstractions Project -->
<PackageReference Include="System.Text.Json" Version="8.0.0" />

<!-- RabbitMQ Project -->
<PackageReference Include="RabbitMQ.Client" Version="6.8.1" />

<!-- Kafka Project -->
<PackageReference Include="Confluent.Kafka" Version="2.3.0" />

<!-- Server Project -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />

<!-- Client Project -->
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
```

---

## 12. Podsumowanie

Ten POC pokazuje:
✅ **Pełną warstwę abstrakcji** - łatwa podmiana RabbitMQ ↔ Kafka
✅ **Fan-out pattern** - broadcasting do wielu konsumentów
✅ **Request-Reply** - synchroniczna komunikacja
✅ **Durability** - konfiguracja persistence
✅ **Docker Compose** - infrastruktura w jednym pliku
✅ **Różnice w modelach** - queues vs topics
✅ **Production-ready patterns** - error handling, graceful shutdown

**Rekomendacja dla POC:**
Użyj **jednego docker-compose.yml** - łatwiejsze zarządzanie, wszystkie serwisy w jednej sieci Docker, spójne volumes i networking.

**Następne kroki:**
1. Implementuj monitoring (Prometheus + Grafana)
2. Dodaj load testing (k6 lub JMeter)
3. Test failover scenarios
4. Benchmark performance różnic

Powodzenia w POC! 🚀
