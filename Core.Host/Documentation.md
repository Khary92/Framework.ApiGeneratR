# API Documentation

Auto-generated documentation for the available endpoints. Total endpoints: 7

## Endpoints Overview

| Method | Requires Auth | Route | Command/Record | Type |
| --- | --- | --- | --- | --- |
| `Post` | True | `change-password` | ChangePasswordCommand | Record |
| `Post` | True | `create-user` | CreateUserCommand | Record |
| `Post` | True | `send-message` | SendMessageCommand | Record |
| `Post` | True | `get-users` | GetAllUsersQuery | Record |
| `Post` | True | `get-messages-for-Id` | GetMessagesForUserQuery | Record |
| `Post` | True | `get-own-user-Id` | GetMyUserIdQuery | Record |
| `Post` | False | `login` | LoginQuery | Record |

---

## Request Definitions

### ChangePasswordCommand

Full Type: `global::ApiGeneratR.Definitions.Requests.Commands.ChangePasswordCommand` 

```csharp
            // Structure of ChangePasswordCommand
            public string OldPassword { get; }
            public string NewPassword { get; }
            public System.Guid IdentityId { get; }
```

### CreateUserCommand

Full Type: `global::ApiGeneratR.Definitions.Requests.Commands.CreateUserCommand` 

```csharp
            // Structure of CreateUserCommand
            public string LoginName { get; }
            public string InitialPassword { get; }
            public string FirstName { get; }
            public string LastName { get; }
```

### SendMessageCommand

Full Type: `global::ApiGeneratR.Definitions.Requests.Commands.SendMessageCommand` 

```csharp
            // Structure of SendMessageCommand
            public string Message { get; }
            public System.Guid TargetUserId { get; }
            public System.Guid IdentityId { get; }
```

### GetAllUsersQuery

Full Type: `global::ApiGeneratR.Definitions.Requests.Queries.GetAllUsersQuery` 

```csharp
            // Structure of GetAllUsersQuery
```

### GetMessagesForUserQuery

Full Type: `global::ApiGeneratR.Definitions.Requests.Queries.GetMessagesForUserQuery` 

```csharp
            // Structure of GetMessagesForUserQuery
            public System.Guid UserId { get; }
            public System.Guid IdentityId { get; }
```

### GetMyUserIdQuery

Full Type: `global::ApiGeneratR.Definitions.Requests.Queries.GetMyUserIdQuery` 

```csharp
            // Structure of GetMyUserIdQuery
            public System.Guid IdentityId { get; }
```

### LoginQuery

Full Type: `global::ApiGeneratR.Definitions.Requests.Queries.LoginQuery` 

```csharp
            // Structure of LoginQuery
            public string Email { get; }
            public string Password { get; }
```

---

# Event Documentation

Auto-generated documentation for the distributed events. Total events: 4

### MessageReceivedEvent

Full Type: `global::ApiGeneratR.Definitions.Events.Message.MessageReceivedEvent` 

Deserialization reference: `message-received` 

```csharp
            // Structure of MessageReceivedEvent
            public System.Guid Id { get; }
            public string ConversationId { get; }
            public System.Guid OriginUserId { get; }
            public string Text { get; }
            public System.DateTime TimeStamp { get; }
```

### UserCreatedEvent

Full Type: `global::ApiGeneratR.Definitions.Events.User.UserCreatedEvent` 

Deserialization reference: `user-created` 

```csharp
            // Structure of UserCreatedEvent
            public System.Guid Id { get; }
            public string LoginName { get; }
            public string FirstName { get; }
            public string LastName { get; }
```

### UserDeletedEvent

Full Type: `global::ApiGeneratR.Definitions.Events.User.UserDeletedEvent` 

Deserialization reference: `user-deleted` 

```csharp
            // Structure of UserDeletedEvent
            public System.Guid Id { get; }
```

### UserUpdatedEvent

Full Type: `global::ApiGeneratR.Definitions.Events.User.UserUpdatedEvent` 

Deserialization reference: `user-updated` 

```csharp
            // Structure of UserUpdatedEvent
            public System.Guid Id { get; }
            public string LoginName { get; }
            public string FirstName { get; }
            public string LastName { get; }
```

