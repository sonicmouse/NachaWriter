# NachaWriter

A lightweight .NET library for generating NACHA (ACH) files.

`NachaWriter` helps you build ACH files from domain models and aggregates, then serialize them into NACHA-compliant fixed-width records.

## Features

- Strongly-typed NACHA record models (`FileHeader`, `BatchHeader`, `EntryDetail`, `Addenda`, controls)
- Aggregate-based file construction (`NachaFile`, `NachaBatch`, `NachaEntry`)
- Automatic control calculations (entry/addenda counts, hash totals, debit/credit totals, block count)
- Automatic trace number and addenda sequence synchronization
- NACHA line padding to blocking factor via `NachaFileStreamWriter`
- Field-level formatting and validation rules

## Target Framework

- .NET8 and .NET10

## Basic Usage (`NachaFileStreamWriter`)

```csharp
using NachaWriter.Domain.Aggregates;
using NachaWriter.Domain.Enums;
using NachaWriter.Domain.Models;
using NachaWriter.Infrastructure.Serialization;

var fileHeader = new FileHeaderRecord
{
    ImmediateDestination = "091000019",
    ImmediateOrigin = "1234567890",
    FileCreationDate = DateOnly.FromDateTime(DateTime.UtcNow),
    FileCreationTime = TimeOnly.FromDateTime(DateTime.UtcNow),
    FileIdModifier = 'A',
    ImmediateDestinationName = "DEST BANK",
    ImmediateOriginName = "MY COMPANY"
};

var batchHeader = new BatchHeaderRecord
{
    ServiceClassCode = ServiceClassCode.CreditsOnly,
    CompanyName = "MY COMPANY",
    CompanyIdentification = "1234567890",
    StandardEntryClassCode = StandardEntryClassCode.PPD,
    CompanyEntryDescription = "PAYROLL",
    EffectiveEntryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
    OriginatingDfiIdentification = "09100001",
    BatchNumber = 1
};

var entries = new[]
{
    new NachaEntry(new EntryDetailRecord
    {
        TransactionCode = TransactionCode.CheckingCredit,
        ReceivingDfiIdentification = "12100035",
        DfiAccountNumber = "123456789",
        Amount = 1500.50m,
        IndividualIdentificationNumber = "EMP001",
        IndividualName = "JANE DOE"
    })
};

var batch = new NachaBatch(batchHeader, entries);
var nachaFile = new NachaFile(fileHeader, new[] { batch });

await using var stream = File.Create("payroll.ach");
await using var writer = new NachaFileStreamWriter(stream, leaveOpen: true);
await writer.WriteAsync(nachaFile);
```

## Lower-Level API

If you need direct control over individual records, use `RecordStreamWriter`:

- Write each record (`WriteAsync(record)`)
- Write filler lines (`WriteFillerLineAsync()`)
- Track written lines (`LineCount`)

## Notes

- Output uses CRLF (`\r\n`) line endings.
- Each NACHA record line is 94 characters.
- File output is padded to a blocking factor of 10 records.

## License

This project is licensed under the terms in `LICENSE.txt`.
