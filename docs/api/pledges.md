# Pledges API

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

## POST /pledges

Creates a pledge. Both the referenced pledger and pledge category must exist;
SQL foreign keys enforce these relationships.

Request:

```json
{
  "year": 2026,
  "amount": 100.00,
  "pledgerId": "6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a01",
  "pledgeCategoryId": "6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a02",
  "comment": "Annual pledge"
}
```

`year`, `amount`, `pledgerId`, and `pledgeCategoryId` are required; `comment`
is optional and a blank comment is stored as `null`. `year` must be between
2020 and 2099, `amount` must be greater than zero with at most two decimal
places, and neither ID may be the empty GUID.

Success returns `201 Created` with a `Location: /pledges/{id}` header and the
same fields as `GET /pledges/{id}`, with `updatedAt` equal to `createdAt`.

| Condition | Response |
| --- | --- |
| Pledge created | `201 Created` with pledge details and `Location` header |
| A required field is missing or the body is malformed | `400 Bad Request` with ASP.NET Core's standard ProblemDetails body (request binding fails before the endpoint runs, so there is no custom title) |
| A field value is rejected by the domain or service | `400 Bad Request` with title `Invalid pledge` and the validation message as `detail` |
| The referenced pledger or pledge category does not exist | `409 Conflict` with sanitized relationship-conflict ProblemDetails; no pledge is created |
| Unexpected creation failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

## GET /pledges/pledger/{pledgerId}

## GET /pledges/pledge-category/{pledgeCategoryId}

Both endpoints return `200 OK` with a `PledgePageResponse` JSON object
containing the pledges for the given pledger or pledge category:

| Response field | Description |
| --- | --- |
| `items` | Array of pledge details (same fields as `GET /pledges/{id}`) |
| `page` | Requested current page |
| `pageSize` | Requested page size |
| `totalCount` | Total number of matching pledges before pagination |
| `totalPages` | Total count divided by page size, rounded up |

No matching pledges returns `items: []`, `totalCount: 0`, and
`totalPages: 0`. A page beyond the end returns empty `items` while retaining
the requested page and the collection's totals.

| Query parameter | Default | Accepted values |
| --- | --- | --- |
| `page` | `1` | Positive integer |
| `pageSize` | `20` | Integer from 1 to 100 |
| `sortBy` | `Year` | `Year`, `Amount` (case-insensitive) |
| `sortOrder` | `Descending` | `Ascending`, `Descending` (case-insensitive) |

Pledges with equal sort values are ordered by native SQL Server GUID order descending, regardless of
`sortOrder`, to keep page boundaries
stable. Numeric enum values and abbreviations such as `asc` are rejected. For
example: `GET /pledges/pledger/{pledgerId}?page=2&pageSize=10&sortBy=Amount&sortOrder=Ascending`.

| Condition | Response |
| --- | --- |
| Pledges retrieved | `200 OK` with a `PledgePageResponse` |
| Route ID is not a GUID | `404 Not Found` (route does not match) |
| `pledgerId` is the empty GUID | `400 Bad Request` with title `Invalid pledger ID` |
| `pledgeCategoryId` is the empty GUID | `400 Bad Request` with title `Invalid pledge category ID` |
| Invalid query parameters, or a query the service rejects | `400 Bad Request` with title `Invalid pledge query` |
| Unexpected retrieval failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

## GET /pledges/{id}

Returns `200 OK` with a single pledge:

| Field | Type | Description |
| --- | --- | --- |
| `id` | UUID | Pledge identity |
| `year` | integer | Pledge year (2020-2099) |
| `amount` | decimal | Pledged amount |
| `pledgerId` | UUID | Associated pledger |
| `pledgeCategoryId` | UUID | Associated pledge category |
| `comment` | string or `null` | Optional comment |
| `createdAt` | UTC timestamp | Creation time |
| `updatedAt` | UTC timestamp | Last update time |

| Condition | Response |
| --- | --- |
| Pledge exists | `200 OK` with pledge details |
| No pledge with that ID | `404 Not Found` |
| `id` is not a GUID | `404 Not Found` (route does not match) |
| `id` is the empty GUID, or the service rejects the ID | `400 Bad Request` with title `Invalid pledge ID` |
| Unexpected retrieval failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

## PUT /pledges/{id}

Updates an existing pledge. `updatedAt` is set to the current UTC time; `id`
and `createdAt` are unchanged.

Request:

```json
{
  "year": 2026,
  "amount": 100.00,
  "pledgerId": "6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a01",
  "pledgeCategoryId": "6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a02",
  "comment": "Annual pledge"
}
```

`year`, `amount`, `pledgerId`, and `pledgeCategoryId` are required and follow
the same rules as `POST /pledges`; `comment` is optional. Missing required
fields return a ProblemDetails response with title `Invalid pledge update`
rather than a deserialization error.

| Condition | Response |
| --- | --- |
| Pledge updated | `200 OK` with the updated pledge details |
| No pledge with that ID | `404 Not Found` |
| `id` is not a GUID | `404 Not Found` (route does not match) |
| `id` is the empty GUID | `400 Bad Request` with title `Invalid pledge ID` |
| A required field is missing, an ID in the body is empty, or the domain or service rejects the update | `400 Bad Request` with title `Invalid pledge update` |
| The referenced pledger or pledge category does not exist | `409 Conflict` with sanitized relationship-conflict ProblemDetails; the stored pledge is unchanged |
| Unexpected update failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

## DELETE /pledges/{id}

Removes the pledge with the given ID. There is no request body.

| Condition | Response |
| --- | --- |
| Pledge removed | `204 No Content` with an empty body |
| No pledge with that ID, including one already removed | `404 Not Found` |
| `id` is not a GUID | `404 Not Found` (route does not match) |
| `id` is the empty GUID, or the service rejects the ID | `400 Bad Request` with title `Invalid pledge ID` |
| Payments reference the pledge | `409 Conflict` with sanitized relationship-conflict ProblemDetails; no related records are removed |
| Unexpected removal failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

The repository throws `KeyNotFoundException` for an unknown ID (passed through
by the service), which the endpoint translates into `404 Not Found`.

Remove its payments before deleting a pledge. Deletes do not cascade.
