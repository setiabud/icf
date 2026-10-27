# Pledge Payments API

## Conventions

These apply to every endpoint below.

- Data is persisted in SQL Server and survives application restarts.
- Error responses use a ProblemDetails body (`application/problem+json`).
- Relationship conflicts return `409 Conflict` with title `Relationship conflict` and detail `The operation conflicts with related accounting records.`; SQL internals are not exposed.
- A route ID that is not a GUID returns `404 Not Found` because the route does not match.
- Domain and service argument errors return `400 Bad Request` with the exception message, without its parameter-name suffix, as `detail`.
- Unexpected failures are logged and return `500 Internal Server Error` with title `An unexpected error occurred` and a generic `detail`; internal exception details are not exposed.
- Error responses from POST do not include a `Location` header.
- Request cancellation is allowed to propagate and is not translated into an error response.

## POST /pledge-payments

Creates an active pledge payment. The referenced pledge must exist; a SQL
foreign key enforces the relationship.

Request:

```json
{
  "pledgeId": "6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00",
  "amount": 50.00,
  "paymentDate": "2026-01-20T00:00:00",
  "comment": "Monthly payment"
}
```

`pledgeId`, `amount`, and `paymentDate` are required; `comment` is optional
and a blank comment is stored as `null`. `pledgeId` must not be the empty
GUID, `amount` must be greater than zero with at most two decimal places, and
`paymentDate` must not be the default date. `paymentDate` is stored and
returned as sent, with no time-zone conversion; `createdAt` and `updatedAt`
are always generated in UTC.

Success returns `201 Created` with a `Location: /pledge-payments/{id}` header
and the same fields as `GET /pledge-payments/{id}`, with `active` set to
`true` and `updatedAt` equal to `createdAt`.

| Condition | Response |
| --- | --- |
| Payment created | `201 Created` with payment details and `Location` header |
| A required field is missing or the body is malformed | `400 Bad Request` with ASP.NET Core's standard ProblemDetails body (request binding fails before the endpoint runs, so there is no custom title) |
| A field value is rejected by the domain or service | `400 Bad Request` with title `Invalid pledge payment` and the validation message as `detail` |
| The referenced pledge does not exist | `409 Conflict` with sanitized relationship-conflict ProblemDetails; no payment is created |
| Unexpected creation failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

## GET /pledge-payments/pledge/{pledgeId}

Returns `200 OK` with a `PledgePaymentPageResponse` JSON object containing the
payments for the given pledge:

| Response field | Description |
| --- | --- |
| `items` | Array of pledge payment details (same fields as `GET /pledge-payments/{id}`) |
| `page` | Requested current page |
| `pageSize` | Requested page size |
| `totalCount` | Total number of payments for the pledge before pagination |
| `totalPages` | Total count divided by page size, rounded up |

A pledge with no payments returns `items: []`, `totalCount: 0`, and
`totalPages: 0`. A page beyond the end returns empty `items` while retaining
the requested page and the collection's totals.

| Query parameter | Default | Accepted values |
| --- | --- | --- |
| `page` | `1` | Positive integer |
| `pageSize` | `20` | Integer from 1 to 100 |
| `sortBy` | `PaymentDate` | `PaymentDate`, `Amount` (case-insensitive) |
| `sortOrder` | `Descending` | `Ascending`, `Descending` (case-insensitive) |

Payments with equal sort values are ordered by native SQL Server GUID order descending, regardless of
`sortOrder`, to keep page boundaries
stable. Numeric enum values and abbreviations such as `asc` are rejected. For
example: `GET /pledge-payments/pledge/{pledgeId}?page=2&pageSize=10&sortBy=Amount&sortOrder=Ascending`.

| Condition | Response |
| --- | --- |
| Payments retrieved | `200 OK` with a `PledgePaymentPageResponse` |
| `pledgeId` is not a GUID | `404 Not Found` (route does not match) |
| `pledgeId` is the empty GUID | `400 Bad Request` with title `Invalid pledge ID` |
| Invalid query parameters, or a query the service rejects | `400 Bad Request` with title `Invalid pledge payment query` |
| Unexpected retrieval failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

## GET /pledge-payments/{id}

Returns `200 OK` with a single pledge payment:

| Field | Type | Description |
| --- | --- | --- |
| `id` | UUID | Pledge payment identity |
| `pledgeId` | UUID | Associated pledge |
| `amount` | decimal | Payment amount |
| `paymentDate` | date-time | Date the payment was made |
| `comment` | string or `null` | Optional comment |
| `active` | boolean | Whether the payment is active |
| `createdAt` | UTC timestamp | Creation time |
| `updatedAt` | UTC timestamp | Last update time |

| Condition | Response |
| --- | --- |
| Payment exists | `200 OK` with payment details |
| No payment with that ID | `404 Not Found` |
| `id` is not a GUID | `404 Not Found` (route does not match) |
| `id` is the empty GUID, or the service rejects the ID | `400 Bad Request` with title `Invalid pledge payment ID` |
| Unexpected retrieval failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

## PUT /pledge-payments/{id}

Updates an existing payment's pledge association, amount, payment date, comment,
and active flag. The referenced pledge must exist. `updatedAt`
is set to the current UTC time; `id` and `createdAt` are unchanged.

Request:

```json
{
  "pledgeId": "6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00",
  "amount": 50.00,
  "paymentDate": "2026-01-20T00:00:00",
  "comment": "Monthly payment",
  "active": true
}
```

`pledgeId`, `amount`, `paymentDate`, and `active` are required and follow the
same rules as `POST /pledge-payments`; `comment` is optional.

| Condition | Response |
| --- | --- |
| Payment updated | `200 OK` with the updated payment details |
| No payment with that ID | `404 Not Found` |
| `id` is not a GUID | `404 Not Found` (route does not match) |
| `id` is the empty GUID | `400 Bad Request` with title `Invalid pledge payment ID` |
| A required field is missing, or the domain or service rejects the update | `400 Bad Request` with title `Invalid pledge payment update` |
| The referenced pledge does not exist | `409 Conflict` with sanitized relationship-conflict ProblemDetails; the stored payment is unchanged |
| Unexpected update failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

## DELETE /pledge-payments/{id}

Removes the payment with the given ID. There is no request body.

| Condition | Response |
| --- | --- |
| Payment removed | `204 No Content` with an empty body |
| No payment with that ID, including one already removed | `404 Not Found` |
| `id` is not a GUID | `404 Not Found` (route does not match) |
| `id` is the empty GUID, or the service rejects the ID | `400 Bad Request` with title `Invalid pledge payment ID` |
| Unexpected removal failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

The repository throws `KeyNotFoundException` for an unknown ID (passed through
by the service), which the endpoint translates into `404 Not Found`.
