# Mail Service

A microservice for sending emails with RabbitMQ integration.

## Docker Setup and Running

### Requirements

- Docker
- Docker Compose
- RabbitMQ (should be already running locally)

### Installation and Running

1. Clone the project
2. Navigate to the project directory
3. Start the service with Docker Compose:

```bash
docker-compose up -d
```

This command will start the Mail Service using your local RabbitMQ instance.

- Mail Service: http://localhost:5050

### Stopping the Service

```bash
docker-compose down
```

### Viewing Logs

```bash
docker-compose logs -f
```

## Configuration

The service is configured to connect to a local RabbitMQ instance using the host machine network.

## Application Features

- Sending emails through SMTP
- Receiving email requests via RabbitMQ
- RESTful API for direct email sending
- Health check endpoints
- Swagger documentation 