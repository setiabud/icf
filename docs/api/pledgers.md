# Pledgers API

## GET /pledgers

Returns `200 OK` with a `PledgerPageResponse` JSON object:

| Response field | Description |
| --- | --- |
| `items` | Array of pledger details (the same fields as the creation response below) |
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
ordered by ID to keep page boundaries stable. For example:
`GET /pledgers?page=2&pageSize=5&sortBy=PledgerName&sortOrder=Descending`.

Sorting uses the `PledgerSortBy` and `SortOrder` enums internally. This is a
breaking query-contract change: the former `asc` and `desc` abbreviations are
no longer accepted. Numeric enum values are also rejected.

Invalid query parameters return `400 Bad Request` with a ProblemDetails body.
Unexpected retrieval failures are logged and return `500 Internal Server Error`
with a ProblemDetails body without internal exception details.

Service and repository callers may supply a `PledgerQuery` to request the
same pagination and ordering. The repository's `GetAllAsync` returns a domain
`PagedPledgersResult` with items, page, page size, total count, and total pages
from the same collection snapshot. The service forwards this result without
making a separate count call;
the API maps this result to `PledgerPageResponse` without calculating metadata.
Service calls without a query now use default pagination (page 1, size 10),
instead of returning all pledgers. Repository calls without a query remain
unpaged, returning all items as page 1 with page size equal to the item count
(or 1 for an empty collection).

## POST /pledgers

Creates an active pledger using the in-memory repository.

Request:

```json
{
  "pledgerName": "Community Fund"
}
```

`pledgerName` is required and must not be null, empty, or whitespace.
There are no optional creation fields.

Success returns `201 Created` with a `Location: /pledgers/{id}` header and
the following response fields:

| Field | Type | Description |
| --- | --- | --- |
| `id` | UUID | Generated pledger identity |
| `pledgerName` | string | Submitted name |
| `active` | boolean | Initially `true` |
| `createdAt` | UTC timestamp | Creation time |
| `updatedAt` | UTC timestamp | Initially equal to `createdAt` |

Invalid or missing names return `400 Bad Request` with an
`application/problem+json` ProblemDetails body.

The Location header identifies the created resource; a GET route at that
location is not currently implemented. In-memory data is lost when the
application restarts.
