# Glossary

This glossary defines the terminology used across ICF Accounting.

## Pledger

A person or organization that makes pledges. A pledger has a name, active state, optional email address, and notification preference.

## Pledge Category

A category used to classify a pledge, such as a purpose or fund. A pledge category has a name and active state.

## Pledge

A monetary commitment made by a pledger for a specific year and pledge category.

## Pledge Year

The accounting year represented by a pledge. Pledge years are currently constrained to 2020 through 2099.

## Pledge Amount

The positive monetary amount committed by a pledge. Amounts have at most two decimal places.

## Pledge Payment

A payment applied to a specific pledge. A payment has an amount, payment date, optional comment, and active state.

## Pledger ID

A strongly typed GUID identity for a pledger.

## Pledge Category ID

A strongly typed GUID identity for a pledge category.

## Pledge ID

A strongly typed GUID identity for a pledge.

## Pledge Payment ID

A strongly typed GUID identity for a pledge payment.

## Active

Whether a pledger, pledge category, or pledge payment is eligible for use in current accounting workflows.

## Relationship Conflict

A persistence failure caused when a record cannot be deleted or changed because another record depends on it. Relationship conflicts are returned to clients as HTTP 409 responses where applicable.

## Accounting Context

The EF Core persistence context for pledgers, pledge categories, pledges, and pledge payments. Its migration history is `public.__EFMigrationsHistory`.

## Authentication Context

The EF Core and ASP.NET Identity persistence context for user accounts and profiles. Its migration history is `auth.__IdentityMigrationsHistory`.

## ProblemDetails

The standardized HTTP error response format used for validation, authentication, authorization, conflict, and server-error outcomes.
