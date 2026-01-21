# Mediator Registry Documentation

This document lists all automatically registered Request Handlers and their associated types.

## Registered Handlers

| Handler Class | Request Type | Response Type |
| --- | --- | --- |
| CreateUserCommandHandler | CreateUserCommand | Boolean |
| GetAllUsersQueryHandler | GetAllUsersQuery | List |

---

## Detailed Handler Mapping

### CreateUserCommandHandler

- **Handler:** `global::Framework.Example.Handlers.Commands.CreateUserCommandHandler`
- **Request:** `global::Framework.Example.Handlers.Commands.CreateUserCommand`
- **Response:** `bool`

### GetAllUsersQueryHandler

- **Handler:** `global::Framework.Example.Handlers.Queries.GetAllUsersQueryHandler`
- **Request:** `global::Framework.Example.Handlers.Queries.GetAllUsersQuery`
- **Response:** `global::System.Collections.Generic.List<global::Framework.Example.Entities.User>`

