using NachaWriter.Domain.Attributes;
using NachaWriter.Domain.Enums;
using NachaWriter.Domain.Models;
using NachaWriter.Infrastructure.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NachaWriter.Tests
{
	public sealed class WriterTests
	{
		[Fact]
		public async Task WriteAsync_FileHeaderRecord_WritesSingleRecordLine()
		{
			var record = new FileHeaderRecord
			{
				ImmediateDestination = "091000019",
				ImmediateOrigin = "1234567890",
				FileCreationDate = new DateOnly(2026, 01, 31),
				FileCreationTime = new TimeOnly(13, 45),
				FileIdModifier = 'A',
				ImmediateDestinationName = "DEST BANK",
				ImmediateOriginName = "MY COMPANY",
				ReferenceCode = "REF001"
			};

			var output = await WriteRecordAsync(record);

			Assert.Equal(96, output.Length);
			Assert.EndsWith("\r\n", output);
		}

		[Fact]
		public async Task WriteAsync_AddendaRecord_WritesSingleRecordLine()
		{
			var record = CreateValidAddendaRecord();

			var output = await WriteRecordAsync(record);

			Assert.Equal(96, output.Length);
			Assert.EndsWith("\r\n", output);
		}

		[Fact]
		public async Task WriteAsync_BatchHeaderRecord_WritesSingleRecordLine()
		{
			var record = new BatchHeaderRecord
			{
				ServiceClassCode = ServiceClassCode.Mixed,
				CompanyName = "MY COMPANY",
				CompanyDiscretionaryData = "PAYROLL",
				CompanyIdentification = "1234567890",
				StandardEntryClassCode = StandardEntryClassCode.PPD,
				CompanyEntryDescription = "PAYROLL",
				CompanyDescriptiveDate = "JAN 31",
				EffectiveEntryDate = new DateOnly(2026, 01, 31),
				OriginatorStatusCode = OriginatorStatusCode.Commercial,
				OriginatingDfiIdentification = "09100001",
				BatchNumber = 1
			};

			var output = await WriteRecordAsync(record);

			Assert.Equal(96, output.Length);
			Assert.EndsWith("\r\n", output);
		}

		[Fact]
		public async Task WriteAsync_EntryDetailRecord_WritesSingleRecordLine()
		{
			var record = new EntryDetailRecord
			{
				TransactionCode = TransactionCode.CheckingCredit,
				ReceivingDfiIdentification = "09100001",
				CheckDigit = 9,
				DfiAccountNumber = "123456789",
				Amount = 123.45m,
				IndividualIdentificationNumber = "INV00001",
				IndividualName = "JANE DOE",
				DiscretionaryData = "AB",
				AddendaRecordIndicator = AddendaIndicator.None,
				TraceNumber = 91000010000001
			};

			var output = await WriteRecordAsync(record);

			Assert.Equal(96, output.Length);
			Assert.EndsWith("\r\n", output);
		}

		[Fact]
		public async Task WriteAsync_BatchControlRecord_WritesSingleRecordLine()
		{
			var record = new BatchControlRecord
			{
				ServiceClassCode = ServiceClassCode.Mixed,
				EntryAddendaCount = 1,
				EntryHash = 9100001,
				TotalDebitEntryDollarAmount = 0m,
				TotalCreditEntryDollarAmount = 123.45m,
				CompanyIdentification = "1234567890",
				MessageAuthenticationCode = string.Empty,
				OriginatingDfiIdentification = "09100001",
				BatchNumber = 1
			};

			var output = await WriteRecordAsync(record);

			Assert.Equal(96, output.Length);
			Assert.EndsWith("\r\n", output);
		}

		[Fact]
		public async Task WriteAsync_FileControlRecord_WritesSingleRecordLine()
		{
			var record = new FileControlRecord
			{
				BatchCount = 1,
				BlockCount = 1,
				EntryAddendaCount = 1,
				EntryHash = 9100001,
				TotalDebitEntryDollarAmountInFile = 0m,
				TotalCreditEntryDollarAmountInFile = 123.45m
			};

			var output = await WriteRecordAsync(record);

			Assert.Equal(96, output.Length);
			Assert.EndsWith("\r\n", output);
		}

		[Fact]
		public async Task WriteAsync_RecordWithNonAsciiCharacter_ThrowsInvalidOperationException()
		{
			var record = new BatchHeaderRecord
			{
				ServiceClassCode = ServiceClassCode.Mixed,
				CompanyName = "CAFÉ",
				CompanyDiscretionaryData = "PAYROLL",
				CompanyIdentification = "1234567890",
				StandardEntryClassCode = StandardEntryClassCode.PPD,
				CompanyEntryDescription = "PAYROLL",
				CompanyDescriptiveDate = "JAN 31",
				EffectiveEntryDate = new DateOnly(2026, 01, 31),
				OriginatorStatusCode = OriginatorStatusCode.Commercial,
				OriginatingDfiIdentification = "09100001",
				BatchNumber = 1
			};

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("Invalid non-ASCII character", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_AllRecordTypes_WritesAllRecordLinesInOrder()
		{
			var fileHeader = new FileHeaderRecord
			{
				ImmediateDestination = "091000019",
				ImmediateOrigin = "1234567890",
				FileCreationDate = new DateOnly(2026, 01, 31),
				FileCreationTime = new TimeOnly(13, 45),
				FileIdModifier = 'A',
				ImmediateDestinationName = "DEST BANK",
				ImmediateOriginName = "MY COMPANY",
				ReferenceCode = "REF001"
			};

			var batchHeader = new BatchHeaderRecord
			{
				ServiceClassCode = ServiceClassCode.Mixed,
				CompanyName = "MY COMPANY",
				CompanyDiscretionaryData = "PAYROLL",
				CompanyIdentification = "1234567890",
				StandardEntryClassCode = StandardEntryClassCode.PPD,
				CompanyEntryDescription = "PAYROLL",
				CompanyDescriptiveDate = "JAN 31",
				EffectiveEntryDate = new DateOnly(2026, 01, 31),
				OriginatorStatusCode = OriginatorStatusCode.Commercial,
				OriginatingDfiIdentification = "09100001",
				BatchNumber = 1
			};

			var entryDetail = new EntryDetailRecord
			{
				TransactionCode = TransactionCode.CheckingCredit,
				ReceivingDfiIdentification = "09100001",
				CheckDigit = 9,
				DfiAccountNumber = "123456789",
				Amount = 123.45m,
				IndividualIdentificationNumber = "INV00001",
				IndividualName = "JANE DOE",
				DiscretionaryData = "AB",
				AddendaRecordIndicator = AddendaIndicator.None,
				TraceNumber = 91000010000001
			};

			var addenda = CreateValidAddendaRecord();

			var batchControl = new BatchControlRecord
			{
				ServiceClassCode = ServiceClassCode.Mixed,
				EntryAddendaCount = 1,
				EntryHash = 9100001,
				TotalDebitEntryDollarAmount = 0m,
				TotalCreditEntryDollarAmount = 123.45m,
				CompanyIdentification = "1234567890",
				MessageAuthenticationCode = string.Empty,
				OriginatingDfiIdentification = "09100001",
				BatchNumber = 1
			};

			var fileControl = new FileControlRecord
			{
				BatchCount = 1,
				BlockCount = 1,
				EntryAddendaCount = 1,
				EntryHash = 9100001,
				TotalDebitEntryDollarAmountInFile = 0m,
				TotalCreditEntryDollarAmountInFile = 123.45m
			};

			using var stream = new MemoryStream();
			await using (var writer = new RecordStreamWriter(stream, leaveOpen: true))
			{
				await writer.WriteAsync(fileHeader, TestContext.Current.CancellationToken);
				await writer.WriteAsync(batchHeader, TestContext.Current.CancellationToken);
				await writer.WriteAsync(entryDetail, TestContext.Current.CancellationToken);
				await writer.WriteAsync(addenda, TestContext.Current.CancellationToken);
				await writer.WriteAsync(batchControl, TestContext.Current.CancellationToken);
				await writer.WriteAsync(fileControl, TestContext.Current.CancellationToken);
			}

			var output = Encoding.ASCII.GetString(stream.ToArray());
			Assert.Equal(96 * 6, output.Length);

			var lines = output.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
			Assert.Equal(6, lines.Length);
			Assert.All(lines, line => Assert.Equal(94, line.Length));
			Assert.Equal('1', lines[0][0]);
			Assert.Equal('5', lines[1][0]);
			Assert.Equal('6', lines[2][0]);
			Assert.Equal('7', lines[3][0]);
			Assert.Equal('8', lines[4][0]);
			Assert.Equal('9', lines[5][0]);
		}

		[Fact]
		public void LineCount_InitialValue_IsZero()
		{
			using var stream = new MemoryStream();
			using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			Assert.Equal(0, writer.LineCount);
		}

		[Fact]
		public async Task WriteAsync_LineCount_IncrementsAfterRecordWrite()
		{
			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			await writer.WriteAsync(CreateValidBatchHeaderRecord(), TestContext.Current.CancellationToken);

			Assert.Equal(1, writer.LineCount);
		}

		[Fact]
		public async Task WriteAsync_WhenValidationFails_LineCount_DoesNotIncrement()
		{
			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var invalidRecord = CreateValidEntryDetailRecord() with { ReceivingDfiIdentification = "12A00001" };

			await Assert.ThrowsAsync<ArgumentException>(() =>
				writer.WriteAsync(invalidRecord, TestContext.Current.CancellationToken));

			Assert.Equal(0, writer.LineCount);
		}

		[Fact]
		public async Task WriteFillerLineAsync_WritesFillerLineAndIncrementsLineCount()
		{
			using var stream = new MemoryStream();
			await using (var writer = new RecordStreamWriter(stream, leaveOpen: true))
			{
				await writer.WriteFillerLineAsync(TestContext.Current.CancellationToken);
				Assert.Equal(1, writer.LineCount);
			}

			var output = Encoding.ASCII.GetString(stream.ToArray());
			Assert.Equal(96, output.Length);

			var line = GetSingleLine(output);
			Assert.Equal(new string('9', 94), line);
		}

		[Fact]
		public async Task WriteFillerLineAsync_AfterDispose_ThrowsObjectDisposedException()
		{
			using var stream = new MemoryStream();
			var writer = new RecordStreamWriter(stream, leaveOpen: true);
			await writer.DisposeAsync();

			await Assert.ThrowsAsync<ObjectDisposedException>(() =>
				writer.WriteFillerLineAsync(TestContext.Current.CancellationToken));
		}

		[Fact]
		public async Task WriteAsync_NumericValidationMode_InvalidCharacter_ThrowsArgumentException()
		{
			var record = CreateValidEntryDetailRecord() with { ReceivingDfiIdentification = "12A00001" };

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("validation mode: Numeric", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_UppercaseAlphanumericValidationMode_LowercaseCharacter_ThrowsArgumentException()
		{
			var record = new FileHeaderRecord
			{
				ImmediateDestination = "091000019",
				ImmediateOrigin = "1234567890",
				FileCreationDate = new DateOnly(2026, 01, 31),
				FileCreationTime = new TimeOnly(13, 45),
				FileIdModifier = 'a',
				ImmediateDestinationName = "DEST BANK",
				ImmediateOriginName = "MY COMPANY",
				ReferenceCode = "REF001"
			};

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("validation mode: UppercaseAlphanumeric", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_AlphanumericValidationMode_DisallowedSymbol_ThrowsArgumentException()
		{
			var record = CreateValidBatchHeaderRecord() with { CompanyName = "MY#COMPANY" };

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("validation mode: Alphanumeric", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_AddendaTextValidationMode_ExtendedSymbols_AreAllowed()
		{
			var addendaText = "PAYMENT!#[]{}|^_+?=@;:*%$";
			var record = CreateValidAddendaRecord() with { PaymentRelatedInformation = addendaText };

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal(addendaText.PadRight(80, ' '), line.Substring(3, 80));
		}

		[Fact]
		public async Task WriteAsync_AddendaTextValidationMode_DisallowedCharacter_ThrowsArgumentException()
		{
			var record = CreateValidAddendaRecord() with { PaymentRelatedInformation = "PAYMENT~DETAILS" };

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("validation mode: AddendaText", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_NullRecord_ThrowsArgumentNullException()
		{
			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				writer.WriteAsync<BatchHeaderRecord>(null!, TestContext.Current.CancellationToken));
		}

		[Fact]
		public async Task WriteAsync_AfterDispose_ThrowsObjectDisposedException()
		{
			using var stream = new MemoryStream();
			var writer = new RecordStreamWriter(stream, leaveOpen: true);
			await writer.DisposeAsync();

			var record = new BatchHeaderRecord
			{
				ServiceClassCode = ServiceClassCode.Mixed,
				CompanyName = "MY COMPANY",
				CompanyDiscretionaryData = "PAYROLL",
				CompanyIdentification = "1234567890",
				StandardEntryClassCode = StandardEntryClassCode.PPD,
				CompanyEntryDescription = "PAYROLL",
				CompanyDescriptiveDate = "JAN 31",
				EffectiveEntryDate = new DateOnly(2026, 01, 31),
				OriginatorStatusCode = OriginatorStatusCode.Commercial,
				OriginatingDfiIdentification = "09100001",
				BatchNumber = 1
			};

			await Assert.ThrowsAsync<ObjectDisposedException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
		}

		[Fact]
		public void Constructor_WithNullFieldWriter_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new RecordStreamWriter((FieldStreamWriter)null!));
		}

		[Fact]
		public async Task Dispose_WhenUsingInjectedFieldWriter_DoesNotDisposeFieldWriter()
		{
			using var stream = new MemoryStream();
			var fieldWriter = new FieldStreamWriter(stream, leaveOpen: true);
			var writer = new RecordStreamWriter(fieldWriter);

#pragma warning disable S6966
			writer.Dispose();
#pragma warning restore S6966

			await fieldWriter.WriteAsync("ABC", 3, cancellationToken: TestContext.Current.CancellationToken);
			await fieldWriter.FlushAsync(TestContext.Current.CancellationToken);

			var output = Encoding.ASCII.GetString(stream.ToArray());
			Assert.Equal("ABC", output);
		}

		[Fact]
		public async Task DisposeAsync_WhenUsingInjectedFieldWriter_DoesNotDisposeFieldWriter()
		{
			using var stream = new MemoryStream();
			var fieldWriter = new FieldStreamWriter(stream, leaveOpen: true);
			var writer = new RecordStreamWriter(fieldWriter);

			await writer.DisposeAsync();

			await fieldWriter.WriteAsync("XYZ", 3, cancellationToken: TestContext.Current.CancellationToken);
			await fieldWriter.FlushAsync(TestContext.Current.CancellationToken);

			var output = Encoding.ASCII.GetString(stream.ToArray());
			Assert.Equal("XYZ", output);
		}

		[Fact]
		public async Task WriteAsync_RecordWithNullProperty_ThrowsArgumentNullException()
		{
			var record = new BatchHeaderRecord
			{
				ServiceClassCode = ServiceClassCode.Mixed,
				CompanyName = null!,
				CompanyDiscretionaryData = "PAYROLL",
				CompanyIdentification = "1234567890",
				StandardEntryClassCode = StandardEntryClassCode.PPD,
				CompanyEntryDescription = "PAYROLL",
				CompanyDescriptiveDate = "JAN 31",
				EffectiveEntryDate = new DateOnly(2026, 01, 31),
				OriginatorStatusCode = OriginatorStatusCode.Commercial,
				OriginatingDfiIdentification = "09100001",
				BatchNumber = 1
			};

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			await Assert.ThrowsAsync<ArgumentNullException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
		}

		[Fact]
		public async Task WriteAsync_RecordWithControlCharacter_ThrowsInvalidOperationException()
		{
			var record = new BatchHeaderRecord
			{
				ServiceClassCode = ServiceClassCode.Mixed,
				CompanyName = "MY\tCOMPANY",
				CompanyDiscretionaryData = "PAYROLL",
				CompanyIdentification = "1234567890",
				StandardEntryClassCode = StandardEntryClassCode.PPD,
				CompanyEntryDescription = "PAYROLL",
				CompanyDescriptiveDate = "JAN 31",
				EffectiveEntryDate = new DateOnly(2026, 01, 31),
				OriginatorStatusCode = OriginatorStatusCode.Commercial,
				OriginatingDfiIdentification = "09100001",
				BatchNumber = 1
			};

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("Invalid non-ASCII character", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_RecordWithNegativeAmount_ThrowsInvalidOperationException()
		{
			var record = new EntryDetailRecord
			{
				TransactionCode = TransactionCode.CheckingCredit,
				ReceivingDfiIdentification = "09100001",
				CheckDigit = 9,
				DfiAccountNumber = "123456789",
				Amount = -1.00m,
				IndividualIdentificationNumber = "INV00001",
				IndividualName = "JANE DOE",
				DiscretionaryData = "AB",
				AddendaRecordIndicator = AddendaIndicator.None,
				TraceNumber = 91000010000001
			};

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("cannot be negative", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_RecordWithNumericOverflowForFieldLength_ThrowsInvalidOperationException()
		{
			var record = new BatchHeaderRecord
			{
				ServiceClassCode = ServiceClassCode.Mixed,
				CompanyName = "MY COMPANY",
				CompanyDiscretionaryData = "PAYROLL",
				CompanyIdentification = "1234567890",
				StandardEntryClassCode = StandardEntryClassCode.PPD,
				CompanyEntryDescription = "PAYROLL",
				CompanyDescriptiveDate = "JAN 31",
				EffectiveEntryDate = new DateOnly(2026, 01, 31),
				OriginatorStatusCode = OriginatorStatusCode.Commercial,
				OriginatingDfiIdentification = "09100001",
				BatchNumber = int.MaxValue
			};

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("too long for field length", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_BatchHeader_StringFieldShorterThanLength_PadsRight()
		{
			var record = CreateValidBatchHeaderRecord() with { CompanyName = "ABC" };

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("ABC".PadRight(16, ' '), line.Substring(4, 16));
		}

		[Fact]
		public async Task WriteAsync_FileHeader_RoutingNumbersShort_AreLeftPadded()
		{
			var record = new FileHeaderRecord
			{
				ImmediateDestination = "91000019",
				ImmediateOrigin = "12345",
				FileCreationDate = new DateOnly(2026, 01, 31),
				FileCreationTime = new TimeOnly(13, 45),
				FileIdModifier = 'A',
				ImmediateDestinationName = "DEST BANK",
				ImmediateOriginName = "MY COMPANY",
				ReferenceCode = "REF001"
			};

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("91000019".PadLeft(10, ' '), line.Substring(3, 10));
			Assert.Equal("12345".PadLeft(10, ' '), line.Substring(13, 10));
		}

		[Fact]
		public async Task WriteAsync_FileHeader_RoutingNumberLongerThanLength_TruncatesToTenCharacters()
		{
			var record = new FileHeaderRecord
			{
				ImmediateDestination = "1234567890123",
				ImmediateOrigin = "1234567890",
				FileCreationDate = new DateOnly(2026, 01, 31),
				FileCreationTime = new TimeOnly(13, 45),
				FileIdModifier = 'A',
				ImmediateDestinationName = "DEST BANK",
				ImmediateOriginName = "MY COMPANY",
				ReferenceCode = "REF001"
			};

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("1234567890", line.Substring(3, 10));
		}

		[Fact]
		public async Task WriteAsync_BatchHeader_StringFieldLongerThanLength_Truncates()
		{
			var record = CreateValidBatchHeaderRecord() with { CompanyName = "ABCDEFGHIJKLMNOPQRST" };

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("ABCDEFGHIJKLMNOP", line.Substring(4, 16));
		}

		[Fact]
		public async Task WriteAsync_BatchHeader_NonAsciiCharacterBeyondTruncation_DoesNotThrow()
		{
			var record = CreateValidBatchHeaderRecord() with { CompanyName = "ABCDEFGHIJKLMNOPÉ" };

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("ABCDEFGHIJKLMNOP", line.Substring(4, 16));
		}

		[Fact]
		public async Task WriteAsync_RecordWithCarriageReturnInString_ThrowsInvalidOperationException()
		{
			var record = CreateValidBatchHeaderRecord() with { CompanyName = "ABC\rDEF" };

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("Invalid non-ASCII character", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_RecordWithLineFeedInString_ThrowsInvalidOperationException()
		{
			var record = CreateValidBatchHeaderRecord() with { CompanyName = "ABC\nDEF" };

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("Invalid non-ASCII character", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_RecordWithDeleteControlCharacter_ThrowsInvalidOperationException()
		{
			var record = CreateValidBatchHeaderRecord() with { CompanyName = $"ABC{(char)0x7F}DEF" };

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("Invalid non-ASCII character", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_BatchHeader_EnumFormatting_WritesNumericAndName()
		{
			var record = CreateValidBatchHeaderRecord();

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("200", line.Substring(1, 3));
			Assert.Equal("PPD", line.Substring(50, 3));
		}

		[Fact]
		public async Task WriteAsync_BatchHeader_IntField_PadsLeftWithZeros()
		{
			var record = CreateValidBatchHeaderRecord() with { BatchNumber = 42 };

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("0000042", line.Substring(87, 7));
		}

		[Fact]
		public async Task WriteAsync_EntryDetail_DecimalAmount_RoundsToCents()
		{
			var record = CreateValidEntryDetailRecord() with { Amount = 123.456m };

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("0000012346", line.Substring(29, 10));
		}

		[Fact]
		public async Task WriteAsync_EntryDetail_DecimalAmountZero_WritesAllZeros()
		{
			var record = CreateValidEntryDetailRecord() with { Amount = 0m };

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("0000000000", line.Substring(29, 10));
		}

		[Fact]
		public void Dispose_CalledTwice_DoesNotThrow()
		{
			using var stream = new MemoryStream();
			var writer = new RecordStreamWriter(stream, leaveOpen: true);

			writer.Dispose();
			writer.Dispose();

			Assert.True(true);
		}

		[Fact]
		public async Task DisposeAsync_CalledTwice_DoesNotThrow()
		{
			using var stream = new MemoryStream();
			var writer = new RecordStreamWriter(stream, leaveOpen: true);

			await writer.DisposeAsync();
			await writer.DisposeAsync();

			Assert.True(true);
		}

		// S6966: SonarQube wants us to always use DisposeAsync, but we want to ensure
		// that both Dispose and DisposeAsync can be called without throwing, even if
		// the other has already been called.
#pragma warning disable S6966

		[Fact]
		public async Task Dispose_ThenDisposeAsync_DoesNotThrow()
		{
			using var stream = new MemoryStream();
			var writer = new RecordStreamWriter(stream, leaveOpen: true);

			writer.Dispose();
			await writer.DisposeAsync();

			Assert.True(true);
		}

		[Fact]
		public async Task DisposeAsync_ThenDispose_DoesNotThrow()
		{
			using var stream = new MemoryStream();
			var writer = new RecordStreamWriter(stream, leaveOpen: true);

			await writer.DisposeAsync();
			writer.Dispose();

			Assert.True(true);
		}

		[Fact]
		public async Task WriteAsync_AfterSyncDispose_ThrowsObjectDisposedException()
		{
			using var stream = new MemoryStream();
			var writer = new RecordStreamWriter(stream, leaveOpen: true);
			writer.Dispose();

			await Assert.ThrowsAsync<ObjectDisposedException>(() =>
				writer.WriteAsync(CreateValidBatchHeaderRecord(), TestContext.Current.CancellationToken));
		}

#pragma warning restore S6966

		[Fact]
		public async Task WriteAsync_FileHeader_DateAndTime_AreFormatted()
		{
			var record = new FileHeaderRecord
			{
				ImmediateDestination = "091000019",
				ImmediateOrigin = "1234567890",
				FileCreationDate = new DateOnly(2026, 12, 25),
				FileCreationTime = new TimeOnly(23, 59),
				FileIdModifier = 'A',
				ImmediateDestinationName = "DEST BANK",
				ImmediateOriginName = "MY COMPANY",
				ReferenceCode = "REF001"
			};

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("261225", line.Substring(23, 6));
			Assert.Equal("2359", line.Substring(29, 4));
		}

		[Fact]
		public async Task WriteAsync_BatchHeader_StandardEntryClassCode_WritesEnumName()
		{
			var record = CreateValidBatchHeaderRecord() with { StandardEntryClassCode = StandardEntryClassCode.CCD };

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("CCD", line.Substring(50, 3));
		}

		[Fact]
		public async Task WriteAsync_AddendaRecord_FieldsAreFormatted()
		{
			var record = CreateValidAddendaRecord();

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal('7', line[0]);
			Assert.Equal("05", line.Substring(1, 2));
			Assert.Equal("PAYMENT DETAILS".PadRight(80, ' '), line.Substring(3, 80));
			Assert.Equal("0001", line.Substring(83, 4));
			Assert.Equal("0000001", line.Substring(87, 7));
		}

		[Fact]
		public async Task WriteAsync_BatchHeader_EmptyStringFields_AreSpacePadded()
		{
			var record = CreateValidBatchHeaderRecord() with
			{
				CompanyDiscretionaryData = string.Empty,
				CompanyDescriptiveDate = string.Empty
			};

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal(new string(' ', 20), line.Substring(20, 20));
			Assert.Equal(new string(' ', 6), line.Substring(63, 6));
		}

		[Fact]
		public async Task WriteAsync_EntryDetail_DecimalAmount_RoundsHalfUp()
		{
			var record = CreateValidEntryDetailRecord() with { Amount = 0.005m };

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("0000000001", line.Substring(29, 10));
		}

		[Fact]
		public async Task WriteAsync_EntryDetail_DecimalAmountOverflowForFieldLength_ThrowsInvalidOperationException()
		{
			var record = CreateValidEntryDetailRecord() with { Amount = 100000000m };

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("too long for field length", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_EntryDetail_DiscretionaryDataLong_TruncatesToFieldLength()
		{
			var record = CreateValidEntryDetailRecord() with { DiscretionaryData = "ABCDE" };

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("AB", line.Substring(76, 2));
		}

		[Fact]
		public async Task DisposeAsync_LeaveOpenTrue_StreamRemainsUsable()
		{
			using var stream = new MemoryStream();
			await using (var writer = new RecordStreamWriter(stream, leaveOpen: true))
			{
				await writer.WriteAsync(CreateValidBatchHeaderRecord(),
					TestContext.Current.CancellationToken);
			}

			var canSeek = stream.CanSeek;
			Assert.True(canSeek);
			stream.Position = stream.Length;
			stream.WriteByte(0x20);
		}

		[Fact]
		public async Task DisposeAsync_LeaveOpenFalse_StreamIsClosed()
		{
			var stream = new MemoryStream();
			await using (var writer = new RecordStreamWriter(stream, leaveOpen: false))
			{
				await writer.WriteAsync(CreateValidBatchHeaderRecord(),
					TestContext.Current.CancellationToken);
			}

			Assert.False(stream.CanRead);
		}

		[Fact]
		public async Task WriteAsync_MultipleRecords_PreservesRecordOrder()
		{
			using var stream = new MemoryStream();
			await using (var writer = new RecordStreamWriter(stream, leaveOpen: true))
			{
				await writer.WriteAsync(CreateValidBatchHeaderRecord(), TestContext.Current.CancellationToken);
				await writer.WriteAsync(CreateValidEntryDetailRecord(), TestContext.Current.CancellationToken);
				await writer.WriteAsync(CreateValidBatchHeaderRecord() with
				{ BatchNumber = 2 }, TestContext.Current.CancellationToken);
			}

			var output = Encoding.ASCII.GetString(stream.ToArray());
			var lines = output.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

			Assert.Equal(3, lines.Length);
			Assert.Equal('5', lines[0][0]);
			Assert.Equal('6', lines[1][0]);
			Assert.Equal('5', lines[2][0]);
			Assert.Equal("0000002", lines[2].Substring(87, 7));
		}

		[Fact]
		public async Task WriteAsync_BatchHeader_StringFieldExactLength_WritesUnchanged()
		{
			var record = CreateValidBatchHeaderRecord() with { CompanyName = "ABCDEFGHIJKLMNOP" };

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("ABCDEFGHIJKLMNOP", line.Substring(4, 16));
		}

		[Fact]
		public async Task WriteAsync_BatchHeader_StringFieldBoundaryAscii_AllowsSpaceAndAmpersand()
		{
			var record = CreateValidBatchHeaderRecord() with { CompanyName = "& EDGE &" };

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal("& EDGE &".PadRight(16, ' '), line.Substring(4, 16));
		}

		[Fact]
		public async Task WriteAsync_RecordWithNegativeIntField_ThrowsNotSupportedException()
		{
			var record = CreateValidBatchHeaderRecord() with { BatchNumber = -1 };

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("cannot be negative", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_InterfaceTypedRecord_UsesRuntimeTypeFields()
		{
			IRecord record = CreateValidBatchHeaderRecord();

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal(94, line.Length);
			Assert.Equal('5', line[0]);
		}

		[Fact]
		public async Task WriteAsync_RecordWithNoFieldAttributes_ThrowsInvalidOperationException()
		{
			var record = new NoFieldRecord();

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("has no fields decorated", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_RecordWithDuplicateFieldOrder_ThrowsInvalidOperationException()
		{
			var record = new DuplicateOrderRecord();

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("duplicate field order", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_RecordWithInvalidTotalFieldLength_ThrowsInvalidOperationException()
		{
			var record = new InvalidLengthRecord();

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("field lengths total", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_RecordWithUnsupportedFieldType_ThrowsInvalidOperationException()
		{
			var record = new UnsupportedTypeRecord();

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, TestContext.Current.CancellationToken));
			Assert.Contains("Unsupported value type", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_EntryDetail_RecordTypeCode_WritesNumericCode()
		{
			var output = await WriteRecordAsync(CreateValidEntryDetailRecord());
			var line = GetSingleLine(output);

			Assert.Equal('6', line[0]);
		}

		[Fact]
		public async Task WriteAsync_AllRecordTypes_UsesCrlfSeparators()
		{
			using var stream = new MemoryStream();
			await using (var writer = new RecordStreamWriter(stream, leaveOpen: true))
			{

				await writer.WriteAsync(CreateValidBatchHeaderRecord(), TestContext.Current.CancellationToken);
				await writer.WriteAsync(CreateValidEntryDetailRecord(), TestContext.Current.CancellationToken);
			}

			var output = Encoding.ASCII.GetString(stream.ToArray());
			Assert.Contains("\r\n", output);
			Assert.DoesNotContain("\n", output.Replace("\r\n", string.Empty));
		}

		[Fact]
		public async Task WriteAsync_RecordWithByteShortUshortUint_ConvertsUnsignedValues()
		{
			var record = new UnsignedNumericCoverageRecord();

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal('6', line[0]);
			Assert.Equal("007", line.Substring(1, 3));
			Assert.Equal("008", line.Substring(4, 3));
			Assert.Equal("009", line.Substring(7, 3));
			Assert.Equal("010", line.Substring(10, 3));
		}

		[Fact]
		public async Task WriteAsync_RecordWithLong_ConvertsUnsignedValue()
		{
			var record = new PositiveLongRecord();

			var output = await WriteRecordAsync(record);
			var line = GetSingleLine(output);

			Assert.Equal('6', line[0]);
			Assert.Equal("0000000042", line.Substring(1, 10));
		}

		[Fact]
		public async Task WriteAsync_RecordWithNegativeLong_ThrowsInvalidOperationException()
		{
			var record = new NegativeLongRecord();

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, cancellationToken: TestContext.Current.CancellationToken));
			Assert.Contains("cannot be negative", ex.Message);
		}

		[Fact]
		public async Task WriteAsync_RecordWithNegativeShort_ThrowsInvalidOperationException()
		{
			var record = new NegativeShortRecord();

			using var stream = new MemoryStream();
			await using var writer = new RecordStreamWriter(stream, leaveOpen: true);

			var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				writer.WriteAsync(record, cancellationToken: TestContext.Current.CancellationToken));
			Assert.Contains("cannot be negative", ex.Message);
		}

		[Fact]
		public async Task FlushAsync_AfterWrite_PushesBufferedContentToStream()
		{
			using var stream = new MemoryStream();
			await using var writer = new FieldStreamWriter(stream, leaveOpen: true);

			await writer.WriteAsync("ABC", 5, cancellationToken: TestContext.Current.CancellationToken);
			await writer.FlushAsync(TestContext.Current.CancellationToken);

			var output = Encoding.ASCII.GetString(stream.ToArray());
			Assert.Equal("ABC  ", output);
		}

		[Fact]
		public async Task FlushAsync_WithoutWrites_DoesNotThrow()
		{
			using var stream = new MemoryStream();
			await using var writer = new FieldStreamWriter(stream, leaveOpen: true);

			await writer.FlushAsync(TestContext.Current.CancellationToken);

			Assert.Equal(0, stream.Length);
		}

		[Fact]
		public async Task FlushAsync_AfterDispose_ThrowsObjectDisposedException()
		{
			using var stream = new MemoryStream();
			var writer = new FieldStreamWriter(stream, leaveOpen: true);
			await writer.DisposeAsync();

			await Assert.ThrowsAsync<ObjectDisposedException>(() =>
				writer.FlushAsync(TestContext.Current.CancellationToken));
		}

		[Fact]
		public void FieldStreamWriter_Dispose_CalledTwice_DoesNotThrow()
		{
			using var stream = new MemoryStream();
			var writer = new FieldStreamWriter(stream, leaveOpen: true);

			writer.Dispose();
			writer.Dispose();

			Assert.True(true);
		}

		[Fact]
		public async Task FieldStreamWriter_DisposeAsync_CalledTwice_DoesNotThrow()
		{
			using var stream = new MemoryStream();
			var writer = new FieldStreamWriter(stream, leaveOpen: true);

			await writer.DisposeAsync();
			await writer.DisposeAsync();

			Assert.True(true);
		}


		[Fact]
		public async Task FieldStreamWriter_WriteAfterDisposeAsync_ThrowsObjectDisposedException()
		{
			using var stream = new MemoryStream();
			var writer = new FieldStreamWriter(stream, leaveOpen: true);
			await writer.DisposeAsync();

			await Assert.ThrowsAsync<ObjectDisposedException>(() =>
				writer.WriteAsync("ABC", 3, cancellationToken: TestContext.Current.CancellationToken));
		}

#pragma warning disable S6966
		[Fact]
		public async Task FieldStreamWriter_WriteAfterDispose_ThrowsObjectDisposedException()
		{
			using var stream = new MemoryStream();
			var writer = new FieldStreamWriter(stream, leaveOpen: true);
			writer.Dispose();

			await Assert.ThrowsAsync<ObjectDisposedException>(() =>
				writer.WriteAsync("ABC", 3, cancellationToken: TestContext.Current.CancellationToken));
		}

		[Fact]
		public async Task FieldStreamWriter_Dispose_ThenDisposeAsync_DoesNotThrow()
		{
			using var stream = new MemoryStream();
			var writer = new FieldStreamWriter(stream, leaveOpen: true);

			writer.Dispose();
			await writer.DisposeAsync();

			Assert.True(true);
		}

		[Fact]
		public async Task FieldStreamWriter_DisposeAsync_ThenDispose_DoesNotThrow()
		{
			using var stream = new MemoryStream();
			var writer = new FieldStreamWriter(stream, leaveOpen: true);

			await writer.DisposeAsync();
			writer.Dispose();

			Assert.True(true);
		}

#pragma warning restore S6966

		private static BatchHeaderRecord CreateValidBatchHeaderRecord() =>
			new()
			{
				ServiceClassCode = ServiceClassCode.Mixed,
				CompanyName = "MY COMPANY",
				CompanyDiscretionaryData = "PAYROLL",
				CompanyIdentification = "1234567890",
				StandardEntryClassCode = StandardEntryClassCode.PPD,
				CompanyEntryDescription = "PAYROLL",
				CompanyDescriptiveDate = "JAN 31",
				EffectiveEntryDate = new DateOnly(2026, 01, 31),
				OriginatorStatusCode = OriginatorStatusCode.Commercial,
				OriginatingDfiIdentification = "09100001",
				BatchNumber = 1
			};

		private static EntryDetailRecord CreateValidEntryDetailRecord() =>
			new()
			{
				TransactionCode = TransactionCode.CheckingCredit,
				ReceivingDfiIdentification = "09100001",
				CheckDigit = 9,
				DfiAccountNumber = "123456789",
				Amount = 123.45m,
				IndividualIdentificationNumber = "INV00001",
				IndividualName = "JANE DOE",
				DiscretionaryData = "AB",
				AddendaRecordIndicator = AddendaIndicator.None,
				TraceNumber = 91000010000001
			};

		private static AddendaRecord CreateValidAddendaRecord() =>
			new()
			{
				AddendaTypeCode = AddendaTypeCode.PaymentRelatedInformation,
				PaymentRelatedInformation = "PAYMENT DETAILS",
				AddendaSequenceNumber = 1,
				EntryDetailSequenceNumber = 1
			};

		[ExcludeFromCodeCoverage]
		private sealed record NoFieldRecord : IRecord
		{
			public RecordType RecordTypeCode { get; } = RecordType.FileHeader;
			public string Value { get; } = "X";
		}

		[ExcludeFromCodeCoverage]
		private sealed record DuplicateOrderRecord : IRecord
		{
			[Field(1, 1)]
			public RecordType RecordTypeCode { get; } = RecordType.FileHeader;

			[Field(2, 46)]
			public string Left { get; } = new('A', 46);

			[Field(2, 47)]
			public string Right { get; } = new('B', 47);
		}

		[ExcludeFromCodeCoverage]
		private sealed record InvalidLengthRecord : IRecord
		{
			[Field(1, 1)]
			public RecordType RecordTypeCode { get; } = RecordType.FileHeader;

			[Field(2, 10)]
			public string Name { get; } = "SHORT";
		}

		[ExcludeFromCodeCoverage]
		private sealed record UnsupportedTypeRecord : IRecord
		{
			[Field(1, 1)]
			public RecordType RecordTypeCode { get; } = RecordType.FileHeader;

			[Field(2, 36)]
			public Guid UnsupportedValue { get; } = Guid.NewGuid();

			[Field(3, 57)]
			public string Filler { get; } = new('X', 57);
		}

		[ExcludeFromCodeCoverage]
		private sealed record UnsignedNumericCoverageRecord : IRecord
		{
			[Field(1, 1)]
			public RecordType RecordTypeCode { get; } = RecordType.EntryDetail;

			[Field(2, 3)]
			public byte ByteValue { get; } = 7;

			[Field(3, 3)]
			public short ShortValue { get; } = 8;

			[Field(4, 3)]
			public ushort UShortValue { get; } = 9;

			[Field(5, 3)]
			public uint UIntValue { get; } = 10;

			[Field(6, 81)]
			public string Filler { get; } = new('X', 81);
		}

		[ExcludeFromCodeCoverage]
		private sealed record NegativeShortRecord : IRecord
		{
			[Field(1, 1)]
			public RecordType RecordTypeCode { get; } = RecordType.EntryDetail;

			[Field(2, 3)]
			public short ShortValue { get; } = -1;

			[Field(3, 90)]
			public string Filler { get; } = new('X', 90);
		}

		[ExcludeFromCodeCoverage]
		private sealed record PositiveLongRecord : IRecord
		{
			[Field(1, 1)]
			public RecordType RecordTypeCode { get; } = RecordType.EntryDetail;

			[Field(2, 10)]
			public long LongValue { get; } = 42;

			[Field(3, 83)]
			public string Filler { get; } = new('X', 83);
		}

		[ExcludeFromCodeCoverage]
		private sealed record NegativeLongRecord : IRecord
		{
			[Field(1, 1)]
			public RecordType RecordTypeCode { get; } = RecordType.EntryDetail;

			[Field(2, 3)]
			public long LongValue { get; } = -1;

			[Field(3, 90)]
			public string Filler { get; } = new('X', 90);
		}

		private static string GetSingleLine(string output)
		{
			Assert.EndsWith("\r\n", output);
			return output[..^2];
		}

		private static async Task<string> WriteRecordAsync<TRecord>(TRecord record) where TRecord : IRecord
		{
			using var stream = new MemoryStream();

			await using (var writer = new RecordStreamWriter(stream, leaveOpen: true))
			{
				await writer.WriteAsync(record);
			}

			return Encoding.ASCII.GetString(stream.ToArray());
		}
	}
}
