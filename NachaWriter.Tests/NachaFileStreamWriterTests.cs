using NachaWriter.Domain.Aggregates;
using NachaWriter.Domain.Enums;
using NachaWriter.Domain.Models;
using NachaWriter.Infrastructure.Serialization;

namespace NachaWriter.Tests
{
	public sealed class NachaFileStreamWriterTests
	{
		[Fact]
		public async Task WriteAsync_ComplicatedFile_WritesExpectedOrderAndPadsToBlockingFactor()
		{
			// Setup the File Header
			var fileHeader = new FileHeaderRecord
			{
				ImmediateDestination = "000000210",
				ImmediateOrigin = "123456789",
				FileCreationDate = new DateOnly(2026, 3, 29),
				FileCreationTime = new TimeOnly(18, 21),
				FileIdModifier = 'A',
				ImmediateDestinationName = "JPMORGAN CHASE",
				ImmediateOriginName = "MY AWESOME CORP"
			};

			// Batch 1: PPD Payroll (Credits Only)
			var batch1Header = new BatchHeaderRecord
			{
				ServiceClassCode = ServiceClassCode.CreditsOnly,
				CompanyName = "MY AWESOME CORP",
				CompanyIdentification = "1234567890",
				StandardEntryClassCode = StandardEntryClassCode.PPD,
				CompanyEntryDescription = "PAYROLL",
				EffectiveEntryDate = new DateOnly(2026, 3, 30),
				OriginatingDfiIdentification = "02100002",
				BatchNumber = 1
			};

			var batch1Entries = new NachaEntry[]
			{
				new(new EntryDetailRecord
				{
					TransactionCode = TransactionCode.CheckingCredit,
					ReceivingDfiIdentification = "12100035",
					DfiAccountNumber = "1234567890",
					Amount = 1500.50m,
					IndividualIdentificationNumber = "EMP001",
					IndividualName = "JOHN DOE"
				}),
				new(new EntryDetailRecord
				{
					TransactionCode = TransactionCode.SavingsCredit,
					ReceivingDfiIdentification = "06100014",
					DfiAccountNumber = "0987654321",
					Amount = 2500.00m,
					IndividualIdentificationNumber = "EMP002",
					IndividualName = "JANE SMITH"
				})
			};
			var batch1 = new NachaBatch(batch1Header, batch1Entries);

			// Batch 2: CCD Vendor Payment with Addenda (Credits Only)
			var batch2Header = new BatchHeaderRecord
			{
				ServiceClassCode = ServiceClassCode.CreditsOnly,
				CompanyName = "MY AWESOME CORP",
				CompanyIdentification = "1234567890",
				StandardEntryClassCode = StandardEntryClassCode.CCD,
				CompanyEntryDescription = "VENDOR PAY",
				EffectiveEntryDate = new DateOnly(2026, 3, 30),
				OriginatingDfiIdentification = "02100002",
				BatchNumber = 2
			};

			var batch2Entries = new NachaEntry[]
			{
				new(new EntryDetailRecord
				{
					TransactionCode = TransactionCode.CheckingCredit,
					ReceivingDfiIdentification = "04400003",
					DfiAccountNumber = "55555555",
					Amount = 10450.75m,
					IndividualIdentificationNumber = "VND100",
					IndividualName = "ACME SUPPLIES"
				},
				[
					new AddendaRecord
					{
                        // Freeform invoice info linked to the payment
                        PaymentRelatedInformation = "INV-99887766 PAYING FOR WIDGETS"
					},
					new AddendaRecord
					{
						// Maybe some internal notes for the vendor portal
						PaymentRelatedInformation = "PO#12345"
					}
				])
			};
			var batch2 = new NachaBatch(batch2Header, batch2Entries);

			// Batch 3: PPD Collections (Debits Only)
			var batch3Header = new BatchHeaderRecord
			{
				ServiceClassCode = ServiceClassCode.DebitsOnly,
				CompanyName = "MY AWESOME CORP",
				CompanyIdentification = "1234567890",
				StandardEntryClassCode = StandardEntryClassCode.PPD,
				CompanyEntryDescription = "MONTHLYFEE",
				EffectiveEntryDate = new DateOnly(2026, 3, 30),
				OriginatingDfiIdentification = "02100002",
				BatchNumber = 3
			};

			var batch3Entries = new NachaEntry[]
			{
				new(new EntryDetailRecord
				{
					TransactionCode = TransactionCode.CheckingDebit,
					ReceivingDfiIdentification = "07100001",
					DfiAccountNumber = "111222333",
					Amount = 49.99m,
					IndividualIdentificationNumber = "CUST500",
					IndividualName = "BOB JOHNSON"
				})
			};
			var batch3 = new NachaBatch(batch3Header, batch3Entries);

			// Assemble the Master File
			var nachaFile = new NachaFile(fileHeader, [batch1, batch2, batch3]);

			// Act: Write the file to memory
			using var memoryStream = new MemoryStream();

			await using (var writer = new NachaFileStreamWriter(memoryStream, leaveOpen: true))
			{
				// This triggers the reflection loop, validators, and finally the 9s padding
				await writer.WriteAsync(nachaFile, TestContext.Current.CancellationToken);
			}

			// Assert: Read it back to inspect the generated text
			memoryStream.Position = 0;
			using var reader = new StreamReader(memoryStream);
			var generatedFileText = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

			Assert.False(string.IsNullOrWhiteSpace(generatedFileText));

			var lines = generatedFileText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
			Assert.Equal(20, lines.Length);
			Assert.All(lines, line => Assert.Equal(94, line.Length));

			Assert.Equal('1', lines[0][0]);
			Assert.Equal('5', lines[1][0]);
			Assert.Equal('6', lines[2][0]);
			Assert.Equal('6', lines[3][0]);
			Assert.Equal('8', lines[4][0]);
			Assert.Equal('5', lines[5][0]);
			Assert.Equal('6', lines[6][0]);
			Assert.Equal('7', lines[7][0]);
			Assert.Equal('7', lines[8][0]);
			Assert.Equal('8', lines[9][0]);
			Assert.Equal('5', lines[10][0]);
			Assert.Equal('6', lines[11][0]);
			Assert.Equal('8', lines[12][0]);
			Assert.Equal('9', lines[13][0]);

			Assert.NotEqual(new string('9', 94), lines[13]);
			Assert.All(lines.Skip(14), line => Assert.Equal(new string('9', 94), line));
		}

		[Fact]
		public void Constructor_WithNullRecordWriter_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new NachaFileStreamWriter((RecordStreamWriter)null!));
		}

		[Fact]
		public void Constructor_WithNullStream_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new NachaFileStreamWriter((Stream)null!));
		}

		[Fact]
		public async Task WriteAsync_NullNachaFile_ThrowsArgumentNullException()
		{
			using var stream = new MemoryStream();
			await using var writer = new NachaFileStreamWriter(stream, leaveOpen: true);

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				writer.WriteAsync(null!, TestContext.Current.CancellationToken));
		}

		[Fact]
		public async Task WriteAsync_AfterDispose_ThrowsObjectDisposedException()
		{
			using var stream = new MemoryStream();
			var writer = new NachaFileStreamWriter(stream, leaveOpen: true);

#pragma warning disable S6966
			writer.Dispose();
#pragma warning restore S6966

			await Assert.ThrowsAsync<ObjectDisposedException>(() =>
				writer.WriteAsync(CreateSimpleNachaFile(), TestContext.Current.CancellationToken));
		}

		[Fact]
		public async Task WriteAsync_AfterDisposeAsync_ThrowsObjectDisposedException()
		{
			using var stream = new MemoryStream();
			var writer = new NachaFileStreamWriter(stream, leaveOpen: true);
			await writer.DisposeAsync();

			await Assert.ThrowsAsync<ObjectDisposedException>(() =>
				writer.WriteAsync(CreateSimpleNachaFile(), TestContext.Current.CancellationToken));
		}

		[Fact]
		public async Task Dispose_WhenUsingInjectedRecordWriter_DoesNotDisposeRecordWriter()
		{
			using var stream = new MemoryStream();
			var recordWriter = new RecordStreamWriter(stream, leaveOpen: true);
			var writer = new NachaFileStreamWriter(recordWriter);

#pragma warning disable S6966
			writer.Dispose();
#pragma warning restore S6966

			await recordWriter.WriteAsync(CreateSimpleNachaFile().Header, TestContext.Current.CancellationToken);

			Assert.True(true);
		}

		[Fact]
		public async Task DisposeAsync_WhenUsingInjectedRecordWriter_DoesNotDisposeRecordWriter()
		{
			using var stream = new MemoryStream();
			var recordWriter = new RecordStreamWriter(stream, leaveOpen: true);
			var writer = new NachaFileStreamWriter(recordWriter);

			await writer.DisposeAsync();

			await recordWriter.WriteAsync(CreateSimpleNachaFile().Header, TestContext.Current.CancellationToken);

			Assert.True(true);
		}

		[Fact]
		public async Task DisposeAsync_LeaveOpenFalse_ClosesStream()
		{
			var stream = new MemoryStream();

			await using (var writer = new NachaFileStreamWriter(stream, leaveOpen: false))
			{
				await writer.WriteAsync(CreateSimpleNachaFile(), TestContext.Current.CancellationToken);
			}

			Assert.False(stream.CanRead);
		}

		private static NachaFile CreateSimpleNachaFile()
		{
			var fileHeader = new FileHeaderRecord
			{
				ImmediateDestination = "091000019",
				ImmediateOrigin = "1234567890",
				FileCreationDate = new DateOnly(2026, 1, 31),
				FileCreationTime = new TimeOnly(13, 45),
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
				EffectiveEntryDate = new DateOnly(2026, 1, 31),
				OriginatingDfiIdentification = "09100001",
				BatchNumber = 1
			};

			var entries = new[]
			{
				new NachaEntry(new EntryDetailRecord
				{
					TransactionCode = TransactionCode.CheckingCredit,
					ReceivingDfiIdentification = "09100001",
					DfiAccountNumber = "123456789",
					Amount = 123.45m,
					IndividualIdentificationNumber = "INV00001",
					IndividualName = "JANE DOE"
				})
			};

			var batch = new NachaBatch(batchHeader, entries);
			return new NachaFile(fileHeader, [batch]);
		}
	}
}
