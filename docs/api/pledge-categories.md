# Pledge Categories API

Pledge categories are stored in an in-memory repository, so data is lost when
the application restarts.

## GET /pledge-categories/{id}

Returns a single pledge category.

| Field | Type | Description |
| --- | --- | --- |
| `id` | UUID | Pledge category identity |
| `categoryName` | string | Category name |
| `active` | boolean | Whether the category is active |
| `createdAt` | UTC timestamp | Creation time |
| `updatedAt` | UTC timestamp | Last update time |

| Condition | Response |
| --- | --- |
| Pledge category exists | `200 OK` with the pledge category details |
| No pledge category with that ID | `404 Not Found` |
| `id` is not a GUID | `404 Not Found` (route does not match) |
| `id` is the empty GUID, or the service rejects the ID | `400 Bad Request` with a ProblemDetails body |
| Unexpected retrieval failure | `500 Internal Server Error` with a sanitized ProblemDetails body |
