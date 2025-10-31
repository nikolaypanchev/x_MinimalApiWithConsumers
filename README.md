# Minimal API + Consumers (.NET 8)
**Messaging.Api** exposes endpoints to publish to **RabbitMQ** and **Kafka**.  
Two workers (**Rabbit.Consumer.Worker** and **Kafka.Consumer.Worker**) continuously consume messages.

Brokers run locally via **docker-compose** (RabbitMQ + Redpanda).

## Prereqs
- .NET 8 SDK
- Docker Desktop running (WSL2 on Windows)

## Start brokers
```bash
docker compose up -d
# Rabbit UI: http://localhost:15672 (guest/guest)
# Kafka bootstrap: localhost:19092
```

## Run consumers (in separate terminals)
```bash
dotnet run --project src/Rabbit.Consumer.Worker -- --queue demo-queue --prefetch 20
dotnet run --project src/Kafka.Consumer.Worker -- --topic demo-topic --group demo-group
```

## Run API
```bash
dotnet run --project src/Messaging.Api
# Swagger: http://localhost:5088/swagger
```

## Publish examples
```bash
# Rabbit
curl -X POST "http://localhost:5088/rabbit/publish?queue=demo-queue&count=5" -H "Content-Type: application/json" -d '{ "message": "Hello Rabbit" }'

# Kafka
curl -X POST "http://localhost:5088/kafka/publish?topic=demo-topic&count=5" -H "Content-Type: application/json" -d '{ "message": "Hello Kafka" }'
```

## Configuration
Env vars (defaults in parentheses):

Rabbit:
- `RABBIT_HOST` (localhost) `RABBIT_PORT` (5672) `RABBIT_USER` (guest) `RABBIT_PASS` (guest) `RABBIT_VHOST` (/)

Kafka:
- `KAFKA_BOOTSTRAP` (localhost:19092)

Workers flags:
- Rabbit: `--queue`, `--prefetch`, `--auto-ack`
- Kafka: `--topic`, `--group`

API query params:
- `/rabbit/publish?queue=&count=&delayMs=`
- `/kafka/publish?topic=&count=&delayMs=`
