# Pledge Payments API

## POST /pledge-payments

Creates an active pledge payment using the in-memory repository. The
referenced pledge is not checked for existence.

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
`paymentDate` must not be the default date.

Success returns `201 Created` with a `Location: /pledge-payments/{id}` header
and the same fields as `GET /pledge-payments/{id}`, with `active` set to
`true` and `updatedAt` equal to `createdAt`.

| Condition | Response |
| --- | --- |
| Payment created | `201 Created` with payment details and `Location` header |
| A required field is missing or the body is malformed | `400 Bad Request` with a ProblemDetails body |
| A field value is rejected by the domain or service | `400 Bad Request` with title `Invalid pledge payment` and the validation message as `detail` |
| Unexpected creation failure | `500 Internal Server Error` with a sanitized ProblemDetails body and no `Location` header |

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

Payments with equal sort values are ordered by ID to keep page boundaries
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

Returns `200 OK` with a single pledge payment.

| Condition | Response |
| --- | --- |
| Payment exists | `200 OK` with payment details |
| No payment with that ID | `404 Not Found` |
| `id` is not a GUID | `404 Not Found` (route does not match) |
| `id` is the empty GUID, or the service rejects the ID | `400 Bad Request` with a ProblemDetails body |
| Unexpected retrieval failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

## PUT /pledge-payments/{id}

Updates an existing payment's amount, payment date, comment, and active flag.
`amount`, `paymentDate`, and `active` are required; `comment` is optional.

| Condition | Response |
| --- | --- |
| Payment updated | `200 OK` with the updated payment details |
| No payment with that ID | `404 Not Found` |
| `id` is not a GUID | `404 Not Found` (route does not match) |
| `id` is the empty GUID, the body is invalid, or the service rejects the update | `400 Bad Request` with a ProblemDetails body |
| Unexpected update failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

## DELETE /pledge-payments/{id}

Removes the payment with the given ID. There is no request body.

| Condition | Response |
| --- | --- |
| Payment removed | `204 No Content` with an empty body |
| No payment with that ID, including one already removed | `404 Not Found` |
| `id` is not a GUID | `404 Not Found` (route does not match) |
| `id` is the empty GUID, or the service rejects the ID | `400 Bad Request` with a ProblemDetails body |
| Unexpected removal failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

The not-found case is detected when the repository throws
`KeyNotFoundException`, which the endpoint translates into `404 Not Found`.
