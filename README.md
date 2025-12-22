# RabbitMQ Message Services

Проект состоит из двух микросервисов на C#, использующих RabbitMQ для обмена сообщениями, с мониторингом через Prometheus и Grafana.

## Архитектура

- **MessageSender** - сервис для отправки сообщений в RabbitMQ
- **MessageReceiver** - сервис для получения и хранения сообщений
- **RabbitMQ** - брокер сообщений
- **Prometheus** - система мониторинга и сбора метрик
- **Grafana** - визуализация метрик
- **Jenkins** - CI/CD сервер в Docker

## Структура проекта

```
.
├── MessageSender/          # Сервис отправки сообщений
│   ├── Controllers/
│   ├── Program.cs
│   ├── MessageSender.csproj
│   ├── appsettings.json
│   └── Dockerfile
├── MessageReceiver/        # Сервис получения сообщений
│   ├── Controllers/
│   ├── Program.cs
│   ├── MessageReceiver.csproj
│   ├── appsettings.json
│   └── Dockerfile
├── prometheus/
│   └── prometheus.yml      # Конфигурация Prometheus
├── jenkins/
│   └── Dockerfile          # Dockerfile для Jenkins с Docker и docker-compose
├── docker-compose.yml      # Оркестрация всех сервисов
├── Jenkinsfile            # Конфигурация Jenkins CI/CD
└── README.md
```

## Требования

- Docker и Docker Compose
- .NET 8.0 SDK (для локальной разработки, опционально)

## Быстрый старт

### Запуск через Docker Compose

1. Клонируйте репозиторий или перейдите в директорию проекта:
```bash
cd /home/konditer/Documents/1_STUDY/ADMIN_EXAM
```

2. Запустите все сервисы:
```bash
docker-compose up -d
```

3. Проверьте статус сервисов:
```bash
docker-compose ps
```

4. Просмотрите логи:
```bash
docker-compose logs -f
```

### Доступ к сервисам

После запуска сервисы будут доступны по следующим адресам:

- **MessageSender API**: http://localhost:5001
  - Swagger UI: http://localhost:5001/swagger
  - Метрики Prometheus: http://localhost:5001/metrics

- **MessageReceiver API**: http://localhost:5002
  - Swagger UI: http://localhost:5002/swagger
  - Метрики Prometheus: http://localhost:5002/metrics

- **RabbitMQ Management**: http://localhost:15672
  - Логин: `guest`
  - Пароль: `guest`

- **Prometheus**: http://localhost:9090

- **Grafana**: http://localhost:3000
  - Логин: `admin`
  - Пароль: `admin`

- **Jenkins**: http://localhost:8080
  - Первоначальный пароль можно получить командой:
    ```bash
    docker exec jenkins cat /var/jenkins_home/secrets/initialAdminPassword
    ```

## Использование API

### Отправка сообщения

Отправьте POST запрос на MessageSender:

```bash
curl -X POST http://localhost:5001/api/message/send \
  -H "Content-Type: application/json" \
  -d '{"text": "Hello, RabbitMQ!"}'
```

### Получение всех сообщений

Получите все сохраненные сообщения из MessageReceiver:

```bash
curl http://localhost:5002/api/message/all
```

### Получение количества сообщений

Получите количество сохраненных сообщений:

```bash
curl http://localhost:5002/api/message/count
```

## Настройка Prometheus

Prometheus автоматически собирает метрики с обоих сервисов. Конфигурация находится в `prometheus/prometheus.yml`.

Для проверки метрик перейдите в Prometheus UI и выполните запросы:
- `http_requests_received_total` - общее количество HTTP запросов
- `http_request_duration_seconds` - длительность HTTP запросов

## Настройка Grafana

1. Войдите в Grafana (http://localhost:3000)
2. Добавьте источник данных Prometheus:
   - URL: `http://prometheus:9090`
   - Access: Server (default)
3. Создайте дашборды для визуализации метрик

## Jenkins CI/CD

Jenkins запускается в Docker и уже содержит все необходимые инструменты:
- Docker CLI
- docker-compose
- Плагины: workflow-aggregator, docker-workflow, docker-plugin, pipeline-stage-view, git, github

### Первый запуск Jenkins

1. После запуска `docker-compose up -d` подождите несколько секунд, пока Jenkins инициализируется

2. Получите первоначальный пароль администратора:
```bash
docker exec jenkins cat /var/jenkins_home/secrets/initialAdminPassword
```

3. Откройте http://localhost:8080 и введите полученный пароль

4. Установите рекомендуемые плагины (или выберите нужные вручную)

5. Создайте администратора или пропустите этот шаг

### Настройка Pipeline в Jenkins

1. Создайте новый Pipeline job:
   - Нажмите "New Item"
   - Введите имя проекта (например, "MessageServices")
   - Выберите "Pipeline"
   - Нажмите "OK"

2. Настройте Pipeline:
   - В разделе "Pipeline" выберите "Pipeline script from SCM"
   - SCM: Git (или другой, если используете)
   - Repository URL: укажите путь к вашему репозиторию
   - Branch: укажите ветку (например, `*/main` или `*/master`)
   - Script Path: `Jenkinsfile`
   - Сохраните

3. Запустите сборку:
   - Нажмите "Build Now"
   - Pipeline автоматически соберет образы, запустит сервисы и проверит их здоровье

### Важно

Jenkins контейнер имеет доступ к Docker socket хоста (`/var/run/docker.sock`), что позволяет ему запускать docker-compose команды. Все сервисы запускаются в той же сети Docker, что и Jenkins.

## Остановка сервисов

```bash
docker-compose down
```

Для удаления всех данных (volumes):
```bash
docker-compose down -v
```

## Локальная разработка

### Запуск без Docker

1. Убедитесь, что RabbitMQ запущен:
```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

2. Запустите MessageSender:
```bash
cd MessageSender
dotnet run
```

3. Запустите MessageReceiver (в другом терминале):
```bash
cd MessageReceiver
dotnet run
```

## Мониторинг

### Проверка метрик Prometheus

Метрики доступны по адресам:
- MessageSender: http://localhost:5001/metrics
- MessageReceiver: http://localhost:5002/metrics

### Примеры запросов в Prometheus

- `rate(http_requests_received_total[5m])` - скорость запросов
- `http_request_duration_seconds_sum` - суммарное время обработки запросов

## Устранение неполадок

### Сервисы не запускаются

1. Проверьте логи:
```bash
docker-compose logs message-sender
docker-compose logs message-receiver
```

2. Убедитесь, что порты не заняты:
```bash
netstat -tulpn | grep -E '5001|5002|5672|15672|9090|3000'
```

### RabbitMQ недоступен

1. Проверьте статус:
```bash
docker-compose ps rabbitmq
```

2. Проверьте логи:
```bash
docker-compose logs rabbitmq
```

### Проблемы с метриками

1. Убедитесь, что Prometheus может достучаться до сервисов
2. Проверьте конфигурацию в `prometheus/prometheus.yml`
3. Проверьте, что сервисы экспортируют метрики на `/metrics`

## Лицензия

Этот проект создан в учебных целях.

