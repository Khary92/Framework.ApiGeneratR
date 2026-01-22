# API Documentation

Auto-generated documentation for the available endpoints. Total endpoints: 2

## Endpoints Overview

| Method | Requires Auth | Route | Command/Record | Type |
| --- | --- | --- | --- | --- |
| `POST` | False | `/create-user` | CreateUserCommand | Record |
| `POST` | False | `/get-users` | GetAllUsersQuery | Record |

---

## Request Definitions

### CreateUserCommand

Full Type: `global::Framework.Example.Handlers.Commands.CreateUserCommand` 

```csharp
            // Structure of CreateUserCommand
            public string Name { get; }
```

### GetAllUsersQuery

Full Type: `global::Framework.Example.Handlers.Queries.GetAllUsersQuery` 

```csharp
            // Structure of GetAllUsersQuery
```

