# Mail Service

This microservice is designed to automatically send emails to users who register in the system. It listens to events via RabbitMQ event bus and sends relevant email templates to users.

## Overview

This service communicates with other microservices using the Domain Event Pattern via RabbitMQ. For example, when a user registration service successfully registers a user, it publishes an event, and this service detects the event and sends a welcome email to the user.

## Technologies

- .NET 8
- RabbitMQ
- MimeKit (For email sending)

## Setup

### Requirements

- .NET 8 SDK
- A running RabbitMQ server (should be running on localhost:5672 by default)

### Configuration

Customize the `appsettings.json` file according to your email configuration:

```json
"MailService": {
  "Host": "smtp.example.com",
  "Port": 587,
  "Email": "your-email@example.com",
  "Name": "Mail Service",
  "Key": "your-password-or-app-key"
},
"RabbitMQ": {
  "Host": "localhost",
  "UserName": "guest",
  "Password": "guest",
  "Port": 5672,
  "VirtualHost": "/"
}
```

### Running

```
dotnet run
```

## API Endpoints

- `GET /` - Endpoint to verify the service is running
- `GET /send-test-mail` - Endpoint for sending test emails
- `POST /api/test/simulate-user-registered` - Test endpoint to simulate user registration events

## Usage Example

To simulate a user registration event:

```
POST /api/test/simulate-user-registered
Content-Type: application/json

{
  "email": "user@example.com",
  "username": "testuser"
}
```
