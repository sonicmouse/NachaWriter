using NachaWriter.Domain.Aggregates;
using NachaWriter.Domain.Enums;
using NachaWriter.Domain.Models;

namespace NachaWriter.Tests
{
	public sealed class AggregatesTests
	{
		[Fact]
		public void NachaEntry_WithInvalidRoutingLength_ThrowsArgumentException()
		{
			Assert.Throws<ArgumentException>(() =>
				new NachaEntry(new EntryDetailRecord
				{
					TransactionCode = TransactionCode.CheckingCredit,
					ReceivingDfiIdentification = "1234567",
					DfiAccountNumber = "123456789",
					Amount = 10.00m,
					IndividualIdentificationNumber = "EMP001",
					IndividualName = "JANE DOE"
				}));
		}

		[Fact]
		public void NachaEntry_WithEightDigitRouting_CalculatesCheckDigit()
		{
			var entry = new NachaEntry(new EntryDetailRecord
			{
				TransactionCode = TransactionCode.CheckingCredit,
				ReceivingDfiIdentification = "12100035",
				DfiAccountNumber = "123456789",
				Amount = 10.00m,
				IndividualIdentificationNumber = "EMP001",
				IndividualName = "JANE DOE"
			});

			Assert.Equal("12100035", entry.Detail.ReceivingDfiIdentification);
			Assert.Equal(8, entry.Detail.CheckDigit);
			Assert.Equal(AddendaIndicator.None, entry.Detail.AddendaRecordIndicator);
		}

		[Fact]
		public void NachaEntry_WithNineDigitRouting_UsesProvidedCheckDigit()
		{
			var entry = new NachaEntry(new EntryDetailRecord
			{
				TransactionCode = TransactionCode.CheckingCredit,
				ReceivingDfiIdentification = "091000019",
				DfiAccountNumber = "123456789",
				Amount = 10.00m,
				IndividualIdentificationNumber = "EMP001",
				IndividualName = "JANE DOE"
			});

			Assert.Equal("09100001", entry.Detail.ReceivingDfiIdentification);
			Assert.Equal(9, entry.Detail.CheckDigit);
		}

		[Fact]
		public void NachaEntry_WithInvalidRoutingCheckDigit_ThrowsArgumentException()
		{
			Assert.Throws<ArgumentException>(() =>
				new NachaEntry(new EntryDetailRecord
				{
					TransactionCode = TransactionCode.CheckingCredit,
					ReceivingDfiIdentification = "091000018",
					DfiAccountNumber = "123456789",
					Amount = 10.00m,
					IndividualIdentificationNumber = "EMP001",
					IndividualName = "JANE DOE"
				}));
		}

		[Fact]
		public void NachaEntry_WithAddendas_SetsIndicatorAndSynchronizesSequenceNumbers()
		{
			var entry = new NachaEntry(
				new EntryDetailRecord
				{
					TransactionCode = TransactionCode.CheckingCredit,
					ReceivingDfiIdentification = "04400003",
					DfiAccountNumber = "123456789",
					Amount = 10.00m,
					IndividualIdentificationNumber = "EMP001",
					IndividualName = "JANE DOE"
				},
				[
					new AddendaRecord { PaymentRelatedInformation = "INV 1" },
					new AddendaRecord { PaymentRelatedInformation = "INV 2" }
				]);

			Assert.Equal(AddendaIndicator.HasAddenda, entry.Detail.AddendaRecordIndicator);
			Assert.Equal(2, entry.Addendas.Count);
			Assert.Equal(1, entry.Addendas[0].AddendaSequenceNumber);
			Assert.Equal(2, entry.Addendas[1].AddendaSequenceNumber);

			entry.UpdateTraceNumber(210000200123456UL);
			Assert.Equal(123456, entry.Addendas[0].EntryDetailSequenceNumber);
			Assert.Equal(123456, entry.Addendas[1].EntryDetailSequenceNumber);
		}

		[Fact]
		public void NachaEntry_UpdateTraceNumber_UpdatesDetailTraceNumber()
		{
			var entry = new NachaEntry(new EntryDetailRecord
			{
				TransactionCode = TransactionCode.CheckingCredit,
				ReceivingDfiIdentification = "091000019",
				DfiAccountNumber = "123456789",
				Amount = 10.00m,
				IndividualIdentificationNumber = "EMP001",
				IndividualName = "JANE DOE"
			});

			entry.UpdateTraceNumber(91000010000042UL);

			Assert.Equal(91000010000042UL, entry.Detail.TraceNumber);
		}

		[Fact]
		public void NachaBatch_AssignsTraceNumbersAndCalculatesControlTotals()
		{
			var header = CreateBatchHeader(batchNumber: 1, odfi: "02100002");

			var entries = new[]
			{
				new NachaEntry(new EntryDetailRecord
				{
					TransactionCode = TransactionCode.CheckingCredit,
					ReceivingDfiIdentification = "12100035",
					DfiAccountNumber = "111111111",
					Amount = 100.00m,
					IndividualIdentificationNumber = "EMP001",
					IndividualName = "JOHN DOE"
				}),
				new NachaEntry(
					new EntryDetailRecord
					{
						TransactionCode = TransactionCode.CheckingDebit,
						ReceivingDfiIdentification = "07100001",
						DfiAccountNumber = "222222222",
						Amount = 25.50m,
						IndividualIdentificationNumber = "CUST001",
						IndividualName = "BOB SMITH"
					},
					[
						new AddendaRecord { PaymentRelatedInformation = "NOTE 1" },
						new AddendaRecord { PaymentRelatedInformation = "NOTE 2" }
					])
			};

			var batch = new NachaBatch(header, entries);
			var control = batch.Control;

			Assert.Equal(21000020000001UL, batch.Entries[0].Detail.TraceNumber);
			Assert.Equal(21000020000002UL, batch.Entries[1].Detail.TraceNumber);
			Assert.Equal(2, batch.Entries[1].Addendas.Count);
			Assert.All(batch.Entries[1].Addendas,
				addenda => Assert.Equal(2, addenda.EntryDetailSequenceNumber));

			Assert.Equal(4, control.EntryAddendaCount);
			Assert.Equal(19_200_036UL, control.EntryHash);
			Assert.Equal(25.50m, control.TotalDebitEntryDollarAmount);
			Assert.Equal(100.00m, control.TotalCreditEntryDollarAmount);
			Assert.Equal("02100002", control.OriginatingDfiIdentification);
			Assert.Equal(1, control.BatchNumber);
		}

		[Fact]
		public void NachaBatch_WithInvalidOriginatingDfiIdentification_ThrowsInvalidOperationException()
		{
			var header = CreateBatchHeader(batchNumber: 1, odfi: "ABCDEFGH");
			var entries = new[]
			{
				new NachaEntry(new EntryDetailRecord
				{
					TransactionCode = TransactionCode.CheckingCredit,
					ReceivingDfiIdentification = "12100035",
					DfiAccountNumber = "123456789",
					Amount = 1.00m,
					IndividualIdentificationNumber = "EMP001",
					IndividualName = "JANE DOE"
				})
			};

			Assert.Throws<InvalidOperationException>(() => new NachaBatch(header, entries));
		}

		[Fact]
		public void NachaBatch_WithNullHeader_ThrowsArgumentNullException()
		{
			var entries = new[]
			{
				new NachaEntry(new EntryDetailRecord
				{
					TransactionCode = TransactionCode.CheckingCredit,
					ReceivingDfiIdentification = "12100035",
					DfiAccountNumber = "123456789",
					Amount = 1.00m,
					IndividualIdentificationNumber = "EMP001",
					IndividualName = "JANE DOE"
				})
			};

			Assert.Throws<ArgumentNullException>(() => new NachaBatch(null!, entries));
		}

		[Fact]
		public void NachaBatch_WithEmptyEntries_ThrowsArgumentException()
		{
			var header = CreateBatchHeader(batchNumber: 1, odfi: "02100002");

			Assert.Throws<ArgumentException>(() => new NachaBatch(header, []));
		}

		[Fact]
		public void NachaBatch_WithNullEntries_ThrowsArgumentException()
		{
			var header = CreateBatchHeader(batchNumber: 1, odfi: "02100002");

			Assert.Throws<ArgumentException>(() => new NachaBatch(header, null!));
		}

		[Fact]
		public void NachaBatch_ControlTotals_IncludePrenoteTransactionCodes()
		{
			var batch = new NachaBatch(
				CreateBatchHeader(batchNumber: 1, odfi: "02100002"),
				[
					new NachaEntry(new EntryDetailRecord
					{
						TransactionCode = TransactionCode.CheckingPreNoteCredit,
						ReceivingDfiIdentification = "12100035",
						DfiAccountNumber = "123456789",
						Amount = 1.23m,
						IndividualIdentificationNumber = "EMP001",
						IndividualName = "JANE DOE"
					}),
					new NachaEntry(new EntryDetailRecord
					{
						TransactionCode = TransactionCode.SavingsPreNoteDebit,
						ReceivingDfiIdentification = "07100001",
						DfiAccountNumber = "987654321",
						Amount = 4.56m,
						IndividualIdentificationNumber = "CUST001",
						IndividualName = "BOB SMITH"
					})
				]);

			Assert.Equal(1.23m, batch.Control.TotalCreditEntryDollarAmount);
			Assert.Equal(4.56m, batch.Control.TotalDebitEntryDollarAmount);
		}

		[Fact]
		public void NachaBatch_ServiceClassCreditsOnly_WithOnlyCredits_IsValid()
		{
			var header = CreateBatchHeader(batchNumber: 1, odfi: "02100002") with
			{
				ServiceClassCode = ServiceClassCode.CreditsOnly
			};

			var batch = new NachaBatch(
				header,
				[
					new NachaEntry(new EntryDetailRecord
					{
						TransactionCode = TransactionCode.CheckingCredit,
						ReceivingDfiIdentification = "12100035",
						DfiAccountNumber = "123456789",
						Amount = 10.00m,
						IndividualIdentificationNumber = "EMP001",
						IndividualName = "JANE DOE"
					}),
					new NachaEntry(new EntryDetailRecord
					{
						TransactionCode = TransactionCode.SavingsPreNoteCredit,
						ReceivingDfiIdentification = "07100001",
						DfiAccountNumber = "987654321",
						Amount = 0.00m,
						IndividualIdentificationNumber = "EMP002",
						IndividualName = "JOHN DOE"
					})
				]);

			Assert.Equal(ServiceClassCode.CreditsOnly, batch.Header.ServiceClassCode);
		}

		[Fact]
		public void NachaBatch_ServiceClassDebitsOnly_WithOnlyDebits_IsValid()
		{
			var header = CreateBatchHeader(batchNumber: 1, odfi: "02100002") with
			{
				ServiceClassCode = ServiceClassCode.DebitsOnly
			};

			var batch = new NachaBatch(
				header,
				[
					new NachaEntry(new EntryDetailRecord
					{
						TransactionCode = TransactionCode.CheckingDebit,
						ReceivingDfiIdentification = "12100035",
						DfiAccountNumber = "123456789",
						Amount = 10.00m,
						IndividualIdentificationNumber = "EMP001",
						IndividualName = "JANE DOE"
					}),
					new NachaEntry(new EntryDetailRecord
					{
						TransactionCode = TransactionCode.SavingsPreNoteDebit,
						ReceivingDfiIdentification = "07100001",
						DfiAccountNumber = "987654321",
						Amount = 0.00m,
						IndividualIdentificationNumber = "EMP002",
						IndividualName = "JOHN DOE"
					})
				]);

			Assert.Equal(ServiceClassCode.DebitsOnly, batch.Header.ServiceClassCode);
		}

		[Fact]
		public void NachaBatch_ServiceClassMixed_WithCreditAndDebit_IsValid()
		{
			var header = CreateBatchHeader(batchNumber: 1, odfi: "02100002") with
			{
				ServiceClassCode = ServiceClassCode.Mixed
			};

			var batch = new NachaBatch(
				header,
				[
					new NachaEntry(new EntryDetailRecord
					{
						TransactionCode = TransactionCode.CheckingCredit,
						ReceivingDfiIdentification = "12100035",
						DfiAccountNumber = "123456789",
						Amount = 10.00m,
						IndividualIdentificationNumber = "EMP001",
						IndividualName = "JANE DOE"
					}),
					new NachaEntry(new EntryDetailRecord
					{
						TransactionCode = TransactionCode.CheckingDebit,
						ReceivingDfiIdentification = "07100001",
						DfiAccountNumber = "987654321",
						Amount = 5.00m,
						IndividualIdentificationNumber = "EMP002",
						IndividualName = "JOHN DOE"
					})
				]);

			Assert.Equal(ServiceClassCode.Mixed, batch.Header.ServiceClassCode);
		}

		[Fact]
		public void NachaBatch_ServiceClassCreditsOnly_WithDebit_ThrowsInvalidOperationException()
		{
			var header = CreateBatchHeader(batchNumber: 1, odfi: "02100002") with
			{
				ServiceClassCode = ServiceClassCode.CreditsOnly
			};

			var ex = Assert.Throws<InvalidOperationException>(() =>
				new NachaBatch(
					header,
					[
						new NachaEntry(new EntryDetailRecord
						{
							TransactionCode = TransactionCode.CheckingCredit,
							ReceivingDfiIdentification = "12100035",
							DfiAccountNumber = "123456789",
							Amount = 10.00m,
							IndividualIdentificationNumber = "EMP001",
							IndividualName = "JANE DOE"
						}),
						new NachaEntry(new EntryDetailRecord
						{
							TransactionCode = TransactionCode.CheckingDebit,
							ReceivingDfiIdentification = "07100001",
							DfiAccountNumber = "987654321",
							Amount = 5.00m,
							IndividualIdentificationNumber = "EMP002",
							IndividualName = "JOHN DOE"
						})
					]));

			Assert.Contains("CreditsOnly", ex.Message);
		}

		[Fact]
		public void NachaBatch_ServiceClassDebitsOnly_WithCredit_ThrowsInvalidOperationException()
		{
			var header = CreateBatchHeader(batchNumber: 1, odfi: "02100002") with
			{
				ServiceClassCode = ServiceClassCode.DebitsOnly
			};

			var ex = Assert.Throws<InvalidOperationException>(() =>
				new NachaBatch(
					header,
					[
						new NachaEntry(new EntryDetailRecord
						{
							TransactionCode = TransactionCode.CheckingDebit,
							ReceivingDfiIdentification = "12100035",
							DfiAccountNumber = "123456789",
							Amount = 10.00m,
							IndividualIdentificationNumber = "EMP001",
							IndividualName = "JANE DOE"
						}),
						new NachaEntry(new EntryDetailRecord
						{
							TransactionCode = TransactionCode.CheckingCredit,
							ReceivingDfiIdentification = "07100001",
							DfiAccountNumber = "987654321",
							Amount = 5.00m,
							IndividualIdentificationNumber = "EMP002",
							IndividualName = "JOHN DOE"
						})
					]));

			Assert.Contains("DebitsOnly", ex.Message);
		}

		[Fact]
		public void NachaBatch_ServiceClassUnsupportedValue_ThrowsInvalidOperationException()
		{
			var header = CreateBatchHeader(batchNumber: 1, odfi: "02100002") with
			{
				ServiceClassCode = (ServiceClassCode)999
			};

			var ex = Assert.Throws<InvalidOperationException>(() =>
				new NachaBatch(
					header,
					[
						new NachaEntry(new EntryDetailRecord
						{
							TransactionCode = TransactionCode.CheckingCredit,
							ReceivingDfiIdentification = "12100035",
							DfiAccountNumber = "123456789",
							Amount = 10.00m,
							IndividualIdentificationNumber = "EMP001",
							IndividualName = "JANE DOE"
						})
					]));

			Assert.Contains("Unsupported Service Class Code", ex.Message);
		}

		[Fact]
		public void NachaBatch_UnsupportedTransactionCode_ThrowsInvalidOperationException()
		{
			var header = CreateBatchHeader(batchNumber: 1, odfi: "02100002") with
			{
				ServiceClassCode = ServiceClassCode.Mixed
			};

			var ex = Assert.Throws<InvalidOperationException>(() =>
				new NachaBatch(
					header,
					[
						new NachaEntry(new EntryDetailRecord
						{
							TransactionCode = TransactionCode.CheckingReturn,
							ReceivingDfiIdentification = "12100035",
							DfiAccountNumber = "123456789",
							Amount = 10.00m,
							IndividualIdentificationNumber = "EMP001",
							IndividualName = "JANE DOE"
						})
					]));

			Assert.Contains("Unsupported transaction code", ex.Message);
		}

		[Fact]
		public void NachaBatch_EntryHash_UsesRightmostTenDigitsWhenOverflow()
		{
			var batch = new NachaBatch(
				CreateBatchHeader(batchNumber: 1, odfi: "02100002"),
				CreateManyEntries(count: 101, receivingDfi8: "99999999", amount: 1.00m, TransactionCode.CheckingCredit));

			Assert.Equal(99_999_899UL, batch.Control.EntryHash);
		}

		[Fact]
		public void NachaFile_ControlAggregatesTotalsAndBlockCount()
		{
			var fileHeader = CreateFileHeader();

			var batch1 = new NachaBatch(
				CreateBatchHeader(batchNumber: 1, odfi: "02100002"),
				[
					new NachaEntry(new EntryDetailRecord
					{
						TransactionCode = TransactionCode.CheckingCredit,
						ReceivingDfiIdentification = "12100035",
						DfiAccountNumber = "123456789",
						Amount = 10.00m,
						IndividualIdentificationNumber = "EMP001",
						IndividualName = "JANE DOE"
					})
				]);

			var batch2 = new NachaBatch(
				CreateBatchHeader(batchNumber: 2, odfi: "02100002"),
				[
					new NachaEntry(
						new EntryDetailRecord
						{
							TransactionCode = TransactionCode.CheckingDebit,
							ReceivingDfiIdentification = "07100001",
							DfiAccountNumber = "987654321",
							Amount = 7.25m,
							IndividualIdentificationNumber = "CUST001",
							IndividualName = "BOB SMITH"
						},
						[
							new AddendaRecord { PaymentRelatedInformation = "A1" },
							new AddendaRecord { PaymentRelatedInformation = "A2" }
						])
				]);

			var file = new NachaFile(fileHeader, [batch1, batch2]);
			var control = file.Control;

			Assert.Equal(2, control.BatchCount);
			Assert.Equal(4, control.EntryAddendaCount);
			Assert.Equal(19_200_036UL, control.EntryHash);
			Assert.Equal(7.25m, control.TotalDebitEntryDollarAmountInFile);
			Assert.Equal(10.00m, control.TotalCreditEntryDollarAmountInFile);
			Assert.Equal(1, control.BlockCount);
		}

		[Fact]
		public void NachaFile_WithNullHeader_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new NachaFile(null!, [CreateSingleEntryBatch(1)]));
		}

		[Fact]
		public void NachaFile_WithEmptyBatches_ThrowsArgumentException()
		{
			Assert.Throws<ArgumentException>(() => new NachaFile(CreateFileHeader(), []));
		}

		[Fact]
		public void NachaFile_WithNullBatches_ThrowsArgumentException()
		{
			Assert.Throws<ArgumentException>(() => new NachaFile(CreateFileHeader(), null!));
		}

		[Fact]
		public void NachaFile_BlockCount_ExactlyTenLines_RemainsOneBlock()
		{
			var entries = CreateManyEntries(6, "12100035", 1.00m, TransactionCode.CheckingCredit);
			var batch = new NachaBatch(CreateBatchHeader(batchNumber: 1, odfi: "02100002"), entries);
			var file = new NachaFile(CreateFileHeader(), [batch]);

			Assert.Equal(1, file.Control.BlockCount);
		}

		[Fact]
		public void NachaFile_EntryHash_UsesRightmostTenDigitsWhenOverflow()
		{
			var batch1 = new NachaBatch(
				CreateBatchHeader(batchNumber: 1, odfi: "02100002"),
				CreateManyEntries(count: 100, receivingDfi8: "99999999", amount: 1.00m, TransactionCode.CheckingCredit));

			var batch2 = new NachaBatch(
				CreateBatchHeader(batchNumber: 2, odfi: "02100002"),
				CreateManyEntries(count: 100, receivingDfi8: "99999999", amount: 1.00m, TransactionCode.CheckingCredit));

			var file = new NachaFile(CreateFileHeader(), [batch1, batch2]);

			Assert.Equal(9_999_999_800UL, file.Control.EntryHash);
			Assert.Equal(21, file.Control.BlockCount);
		}

		[Fact]
		public void NachaEntry_CheckDigitCalculation_MatchesExpectedAcrossMultipleRoutings()
		{
			var routingBases = new[]
			{
				"01100001",
				"02100002",
				"03110020",
				"04400003",
				"07100001",
				"09100001",
				"12100035",
				"99999999"
			};

			foreach (var baseRouting in routingBases)
			{
				var entry = new NachaEntry(new EntryDetailRecord
				{
					TransactionCode = TransactionCode.CheckingCredit,
					ReceivingDfiIdentification = baseRouting,
					DfiAccountNumber = "123456789",
					Amount = 1.00m,
					IndividualIdentificationNumber = "EMP001",
					IndividualName = "JANE DOE"
				});

				Assert.Equal(baseRouting, entry.Detail.ReceivingDfiIdentification);
				Assert.Equal(CalculateRoutingCheckDigit(baseRouting), entry.Detail.CheckDigit);
			}
		}

		[Fact]
		public void NachaEntry_ValidNineDigitRouting_IsAcceptedAcrossMultipleSamples()
		{
			var routingBases = new[]
			{
				"01100001",
				"02100002",
				"03110020",
				"04400003",
				"07100001",
				"09100001",
				"12100035",
				"99999999"
			};

			foreach (var baseRouting in routingBases)
			{
				var fullRouting = baseRouting + CalculateRoutingCheckDigit(baseRouting);

				var entry = new NachaEntry(new EntryDetailRecord
				{
					TransactionCode = TransactionCode.CheckingCredit,
					ReceivingDfiIdentification = fullRouting,
					DfiAccountNumber = "123456789",
					Amount = 1.00m,
					IndividualIdentificationNumber = "EMP001",
					IndividualName = "JANE DOE"
				});

				Assert.Equal(baseRouting, entry.Detail.ReceivingDfiIdentification);
				Assert.Equal(int.Parse(fullRouting[^1..]), entry.Detail.CheckDigit);
			}
		}

		[Fact]
		public void NachaBatch_EntryHash_MatchesManualCalculationForMixedRoutings()
		{
			var receivingDfis = new[] { "12100035", "07100001", "04400003", "02100002", "99999999" };
			var entries = receivingDfis
				.Select((routing, i) => new NachaEntry(new EntryDetailRecord
				{
					TransactionCode = i % 2 == 0 ? TransactionCode.CheckingCredit : TransactionCode.CheckingDebit,
					ReceivingDfiIdentification = routing,
					DfiAccountNumber = $"ACCT{i:000000000}",
					Amount = i + 1,
					IndividualIdentificationNumber = $"ID{i:000000}",
					IndividualName = "NAME"
				}))
				.ToArray();

			var batch = new NachaBatch(CreateBatchHeader(batchNumber: 1, odfi: "02100002"), entries);
			var expectedHash = receivingDfis
				.Aggregate(0UL, (sum, dfi) => sum + ulong.Parse(dfi[..8])) % 10_000_000_000UL;

			Assert.Equal(expectedHash, batch.Control.EntryHash);
		}

		[Fact]
		public void NachaFile_BlockCount_RoundsUpCorrectlyAcrossBoundaries()
		{
			var entryCounts = new[] { 1, 5, 6, 7, 16, 17 };

			foreach (var entryCount in entryCounts)
			{
				var entries = CreateManyEntries(entryCount, "12100035", 1.00m, TransactionCode.CheckingCredit);
				var batch = new NachaBatch(CreateBatchHeader(batchNumber: 1, odfi: "02100002"), entries);
				var file = new NachaFile(CreateFileHeader(), [batch]);

				var expectedTotalLines = 4 + entryCount;
				var expectedBlockCount = (int)Math.Ceiling(expectedTotalLines / 10.0);

				Assert.Equal(expectedBlockCount, file.Control.BlockCount);
			}
		}

		private static FileHeaderRecord CreateFileHeader() =>
			new()
			{
				ImmediateDestination = "091000019",
				ImmediateOrigin = "1234567890",
				FileCreationDate = new DateOnly(2026, 1, 31),
				FileCreationTime = new TimeOnly(13, 45),
				FileIdModifier = 'A',
				ImmediateDestinationName = "DEST BANK",
				ImmediateOriginName = "MY COMPANY"
			};

		private static BatchHeaderRecord CreateBatchHeader(int batchNumber, string odfi) =>
			new()
			{
				ServiceClassCode = ServiceClassCode.Mixed,
				CompanyName = "MY COMPANY",
				CompanyIdentification = "1234567890",
				StandardEntryClassCode = StandardEntryClassCode.PPD,
				CompanyEntryDescription = "PAYROLL",
				EffectiveEntryDate = new DateOnly(2026, 1, 31),
				OriginatingDfiIdentification = odfi,
				BatchNumber = batchNumber
			};

		private static NachaBatch CreateSingleEntryBatch(int batchNumber) =>
			new(
				CreateBatchHeader(batchNumber, "02100002"),
				[
					new NachaEntry(new EntryDetailRecord
					{
						TransactionCode = TransactionCode.CheckingCredit,
						ReceivingDfiIdentification = "12100035",
						DfiAccountNumber = "123456789",
						Amount = 1.00m,
						IndividualIdentificationNumber = "EMP001",
						IndividualName = "JANE DOE"
					})
				]);

		private static NachaEntry[] CreateManyEntries(int count, string receivingDfi8,
			decimal amount, TransactionCode transactionCode) =>
			[.. Enumerable.Range(0, count)
				.Select(i => new NachaEntry(new EntryDetailRecord
				{
					TransactionCode = transactionCode,
					ReceivingDfiIdentification = receivingDfi8,
					DfiAccountNumber = $"ACCT{i:000000000}",
					Amount = amount,
					IndividualIdentificationNumber = $"ID{i:000000}",
					IndividualName = "NAME"
				}))];

		private static int CalculateRoutingCheckDigit(string eightDigitRouting)
		{
			ReadOnlySpan<int> weights = [3, 7, 1, 3, 7, 1, 3, 7];
			var sum = 0;

			for (var i = 0; i < weights.Length; ++i)
			{
				sum += (eightDigitRouting[i] - '0') * weights[i];
			}

			return (10 - (sum % 10)) % 10;
		}
	}
}
