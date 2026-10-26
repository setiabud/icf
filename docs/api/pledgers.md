# Pledgers API

## Conventions

These apply to every endpoint below.

- Data is stored in an in-memory repository and is lost when the application restarts.
- Error responses use a ProblemDetails body (`application/problem+json`).
- A route ID that is not a GUID returns `404 Not Found` because the route does not match.
- Domain and service argument errors return `400 Bad Request` with the exception message, without its parameter-name suffix, as `detail`.
- Unexpected failures are logged and return `500 Internal Server Error` with title `An unexpected error occurred` and a generic `detail`; internal exception details are not exposed.
- Error responses from POST do not include a `Location` header.
- Request cancellation is allowed to propagate and is not translated into an error response.

## POST /pledgers

Creates an active pledger.

Request:

```json
{
  "pledgerName": "Community Fund"
}
```

`pledgerName` is required and must not be null, empty, or whitespace.
There are no optional creation fields.

Success returns `201 Created` with a `Location: /pledgers/{id}` header and
the fields documented under `GET /pledgers/{id}`, with `active` set to `true`
and `updatedAt` equal to `createdAt`. The Location URL can be used to retrieve
the created pledger.

| Condition | Response |
| --- | --- |
| Pledger created | `201 Created` with pledger details and `Location` header |
| `pledgerName` is absent or the body is malformed | `400 Bad Request` with ASP.NET Core's standard ProblemDetails body (request binding fails before the endpoint runs, so there is no custom title) |
| `pledgerName` is null, empty, or whitespace | `400 Bad Request` with title `Invalid pledger name` |
| The domain or service rejects the pledger | `400 Bad Request` with title `Invalid pledger` |
| Unexpected creation failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

## GET /pledgers

Returns `200 OK` with a `PledgerPageResponse` JSON object:

| Response field | Description |
| --- | --- |
| `items` | Array of pledger details (same fields as `GET /pledgers/{id}`) |
| `page` | Requested current page |
| `pageSize` | Requested page size |
| `totalCount` | Total number of pledgers before pagination |
| `totalPages` | Total count divided by page size, rounded up |

This replaces the previous plain-array response. An empty repository returns
`items: []`, `totalCount: 0`, and `totalPages: 0`. A page beyond the end returns
empty `items` while retaining the requested page and the collection's totals.

| Query parameter | Default | Accepted values |
| --- | --- | --- |
| `page` | `1` | Positive integer |
| `pageSize` | `10` | Integer from 1 to 100 |
| `sortBy` | `PledgerName` | `PledgerName` (case-insensitive) |
| `sortOrder` | `Ascending` | `Ascending`, `Descending` (case-insensitive) |

Names are sorted case-insensitively before pagination. Equal names are
ordered by ID ascending, regardless of `sortOrder`, to keep page boundaries
stable. For example:
`GET /pledgers?page=2&pageSize=5&sortBy=PledgerName&sortOrder=Descending`.

Sorting uses the `PledgerSortBy` and `SortOrder` enums internally. This is a
breaking query-contract change: the former `asc` and `desc` abbreviations are
no longer accepted. Numeric enum values are also rejected.

| Condition | Response |
| --- | --- |
| Pledgers retrieved | `200 OK` with a `PledgerPageResponse` |
| Invalid query parameters, or a query the service rejects | `400 Bad Request` with title `Invalid pledger query` |
| Unexpected retrieval failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

Service and repository callers may supply a `PledgerQuery` to request the
same pagination and ordering. The repository's `GetAllAsync` returns a domain
`PagedPledgersResult` with items, page, page size, total count, and total pages
from the same collection snapshot. The service forwards this result without
making a separate count call;
the API maps this result to `PledgerPageResponse` without calculating metadata.
Service calls without a query now use default pagination (page 1, size 20),
instead of returning all pledgers. Repository calls without a query remain
unpaged, returning all items as page 1 with page size equal to the item count
(or 1 for an empty collection).

## GET /pledgers/{id}

Returns `200 OK` with a single pledger:

| Field | Type | Description |
| --- | --- | --- |
| `id` | UUID | Pledger identity |
| `pledgerName` | string | Pledger name |
| `active` | boolean | Whether the pledger is active |
| `createdAt` | UTC timestamp | Creation time |
| `updatedAt` | UTC timestamp | Last update time |

| Condition | Response |
| --- | --- |
| Pledger exists | `200 OK` with pledger details |
| No pledger with that ID | `404 Not Found` |
| `id` is not a GUID | `404 Not Found` (route does not match) |
| `id` is the empty GUID, or the service rejects the ID | `400 Bad Request` with title `Invalid pledger ID` |
| Unexpected retrieval failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

## PUT /pledgers/{id}

Updates an existing pledger's name and active flag. `updatedAt` is set to the
current UTC time; `id` and `createdAt` are unchanged.

Request:

```json
{
  "pledgerName": "Mission Fund",
  "active": false
}
```

Both fields are required. `pledgerName` must not be null, empty, or whitespace,
and `active` must be `true` or `false`.

| Condition | Response |
| --- | --- |
| Pledger updated | `200 OK` with the updated pledger details |
| No pledger with that ID | `404 Not Found` |
| `id` is not a GUID | `404 Not Found` (route does not match) |
| `id` is the empty GUID | `400 Bad Request` with title `Invalid pledger ID` |
| The body is invalid, or the service rejects the update | `400 Bad Request` with title `Invalid pledger update` |
| Unexpected update failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

## DELETE /pledgers/{id}

Removes the pledger with the given ID. There is no request body.

| Condition | Response |
| --- | --- |
| Pledger removed | `204 No Content` with an empty body |
| No pledger with that ID, including one already removed | `404 Not Found` |
| `id` is not a GUID | `404 Not Found` (route does not match) |
| `id` is the empty GUID, or the service rejects the ID | `400 Bad Request` with title `Invalid pledger ID` |
| Unexpected removal failure | `500 Internal Server Error` with a sanitized ProblemDetails body |

The repository throws `KeyNotFoundException` for an unknown ID (passed through
by the service), which the endpoint translates into `404 Not Found`.

Removing a pledger does not remove or update pledges that reference it.
