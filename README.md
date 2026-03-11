# Transactions Ingest Service

## Overview
This project implements a **.NET console application** that ingests transaction snapshots from a mock API (JSON file), stores them in a SQLite database, and maintains an audit log of all changes.

The service processes each snapshot and determines whether transactions should be:

- **Created** – new transaction not previously seen  
- **Updated** – fields changed for an existing active transaction  
- **Revoked** – previously active transaction missing from the latest snapshot (within 24 hours)  
- **Finalized** – active transaction older than 24 hours  

All changes are recorded in a **TransactionAudit** table to maintain a history of modifications.

---

## Tech Stack

- .NET 10  
- Entity Framework Core  
- SQLite  
- xUnit (unit testing)

---

## Build Instructions

Clone the repository and navigate to the project directory.

```bash
  cd src
  dotnet restore
  dotnet build
```

## Run the Application

```bash
  cd src
  dotnet run
```
Output will indicate when processing has completed.

## Run Tests

```bash
  cd TransactionIngest.Tests
  dotnet restore
  dotnet build
  dotnet test
```
Tests run against an in-memory EF Core database to avoid modifying the SQLite database.

## Database

Two tables are created:

### Transactions

Stores the latest state of each transaction.

Fields include:

- `TransactionId` (unique)
- `CardNumberLast4`
- `LocationCode`
- `ProductName`
- `Amount`
- `Timestamp`
- `Status`
- `LastUpdated`

### TransactionAudit

Records all changes applied to transactions.

Each audit entry captures:

- `TransactionId`
- `ChangeType`
- `ChangedField`
- `OldValue`
- `NewValue`
- `UpdatedAt`

## Processing Approach

The ingestion service follows these steps:

1. **Load Transactions**
   - Transactions are loaded from the JSON file.

2. **Handle Deduplicate Transactions**
   - Duplicate transaction IDs within the snapshot are grouped and the latest entry is used.

3. **Load Existing Transactions**
   - Existing transactions are retrieved from the database for comparison.

4. **Process Each Transaction**
   - If the transaction does not exist create it.  
   - If it exists and values changed update it and record an audit entry.

5. **Finalization Check**
   - Active transactions older than 24 hours are marked Finalized.
  
6. **Revocation Check**
   - Active transactions from the last 24 hours that are not present in the snapshot are marked Revoked.

7. **Audit Logging**
   - Every change is written to the audit table.

## Assumptions

- `TransactionId` uniquely identifies a transaction.
- The mock API snapshot may contain duplicate transaction IDs, so duplicates are resolved before processing.
- Only the last four digits of the card number are stored for security.
- Transactions older than 24 hours that remain active are automatically Finalized.
- A transaction is marked Revoked when an active transaction from the last 24 hours disappears from the latest snapshot.
- Revoked and Finalized transactions cannot be updated
