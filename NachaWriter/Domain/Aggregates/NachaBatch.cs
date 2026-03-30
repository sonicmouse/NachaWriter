using NachaWriter.Domain.Enums;
using NachaWriter.Domain.Models;

namespace NachaWriter.Domain.Aggregates
{
	public sealed class NachaBatch
	{
		public BatchHeaderRecord Header { get; }

		public IReadOnlyList<NachaEntry> Entries { get; }

		public BatchControlRecord Control => GenerateControlRecord();

		public NachaBatch(BatchHeaderRecord header, IEnumerable<NachaEntry> entries)
		{
			Header = header ?? throw new ArgumentNullException(nameof(header));

			var entryList = entries?.ToArray() ?? [];
			if (entryList.Length == 0)
			{
				throw new ArgumentException("A batch must contain at least one entry.");
			}

			Entries = [.. entryList];

			ValidateServiceClassCode();

			// automatically assign Trace Numbers to all entries
			AssignTraceNumbers();
		}

		private void ValidateServiceClassCode()
		{
			if (Header.ServiceClassCode is not ServiceClassCode.Mixed and
			   not ServiceClassCode.CreditsOnly and
			   not ServiceClassCode.DebitsOnly)
			{
				throw new InvalidOperationException(
					$"Unsupported Service Class Code '{Header.ServiceClassCode}' in batch header.");
			}

			var hasCredits = false;
			var hasDebits = false;

			foreach (var entry in Entries)
			{
				var transactionCode = entry.Detail.TransactionCode;

				if (IsCredit(transactionCode))
				{
					hasCredits = true;
				}
				else if (IsDebit(transactionCode))
				{
					hasDebits = true;
				}
				else
				{
					throw new InvalidOperationException(
						$"Unsupported transaction code '{transactionCode}' for Service Class Code validation.");
				}

				if (Header.ServiceClassCode == ServiceClassCode.CreditsOnly && hasDebits)
				{
					throw new InvalidOperationException(
						"Batch Service Class Code is CreditsOnly, but one or more debit entries were found.");
				}

				if (Header.ServiceClassCode == ServiceClassCode.DebitsOnly && hasCredits)
				{
					throw new InvalidOperationException(
						"Batch Service Class Code is DebitsOnly, but one or more credit entries were found.");
				}
			}
		}

		private void AssignTraceNumbers()
		{
			if (!ulong.TryParse(Header.OriginatingDfiIdentification, out var odfiBase))
			{
				throw new InvalidOperationException("Invalid Originating DFI Identification.");
			}

			// a trace number is 15 digits: the 8-digit ODFI routing number + a 7-digit sequence
			var traceBase = odfiBase * 10_000_000UL;

			for (var i = 0; i < Entries.Count; ++i)
			{
				var traceNumber = traceBase + (ulong)i + 1UL;

				Entries[i].UpdateTraceNumber(traceNumber);
			}
		}

		private BatchControlRecord GenerateControlRecord()
		{
			// Entry/Addenda Count: Tally of every Detail and Addenda record
			var totalEntriesAndAddendas = Entries.Count + Entries.Sum(e => e.Addendas.Count);

			// Entry Hash: Sum of the 8-digit Receiving DFI Identification for all entries
			var entryHashSum = Entries.Aggregate(0UL, (sum, e) =>
				sum + ulong.Parse(e.Detail.ReceivingDfiIdentification[..8]));

			// if the sum exceeds 10 digits, use the 10 rightmost digits
			var entryHash = entryHashSum % 10_000_000_000UL;

			// Totals: Sum of all credits and debits in the batch [7]
			var totalDebit = 0M;
			var totalCredit = 0M;

			foreach (var entryDetail in Entries.Select(x => x.Detail))
			{
				if (IsDebit(entryDetail.TransactionCode))
				{
					totalDebit += entryDetail.Amount;
				}
				else if (IsCredit(entryDetail.TransactionCode))
				{
					totalCredit += entryDetail.Amount;
				}
			}

			return new BatchControlRecord
			{
				ServiceClassCode = Header.ServiceClassCode,
				EntryAddendaCount = totalEntriesAndAddendas,
				EntryHash = entryHash,
				TotalDebitEntryDollarAmount = totalDebit,
				TotalCreditEntryDollarAmount = totalCredit,
				CompanyIdentification = Header.CompanyIdentification,
				OriginatingDfiIdentification = Header.OriginatingDfiIdentification,
				BatchNumber = Header.BatchNumber
			};
		}

		private static bool IsDebit(TransactionCode code) =>
			code is TransactionCode.CheckingDebit or
				   TransactionCode.CheckingPreNoteDebit or
				   TransactionCode.SavingsDebit or
				   TransactionCode.SavingsPreNoteDebit;

		private static bool IsCredit(TransactionCode code) =>
			code is TransactionCode.CheckingCredit or
				   TransactionCode.CheckingPreNoteCredit or
				   TransactionCode.SavingsCredit or
				   TransactionCode.SavingsPreNoteCredit;
	}
}
