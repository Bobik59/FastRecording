```mermaid
graph TD
    subgraph Сервер (ASP.NET Core)
        A[Hub: BookingHub] --> B[Методы Hub]
        B --> C[OnConnectedAsync()]
        B --> D[OnDisconnectedAsync()]
        B --> E[SendNotificationToMaster()]
        B --> F[UpdateClientStatus()]
        A --> G[Группы]
        G --> H[Группа мастеров]
        G --> I[Группа клиентов]
    end

    subgraph Клиенты
        J[Клиент (Web)] --> K[Подключение к хабу]
        L[Мастер (Web/Mobile)] --> K
        K --> M[Используемые транспорты]
        M --> N[WebSocket]
        M --> O[Server-Sent Events]
        M --> P[Long Polling]
    end
```
