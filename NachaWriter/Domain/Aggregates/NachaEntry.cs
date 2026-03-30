using NachaWriter.Domain.Enums;
using NachaWriter.Domain.Models;
using System.Collections.Immutable;

namespace NachaWriter.Domain.Aggregates
{
	public sealed class NachaEntry
	{
		public EntryDetailRecord Detail { get; private set; }

		public IReadOnlyList<AddendaRecord> Addendas { get; private set; }

		public NachaEntry(EntryDetailRecord detail, IEnumerable<AddendaRecord>? addendas = null)
		{
			Detail = detail;
			var addendaList = addendas?.ToArray() ?? [];

			var (routing, checkDigit) = CalculateCheckDigit(detail.ReceivingDfiIdentification);

			Detail = Detail with
			{
				ReceivingDfiIdentification = routing,
				CheckDigit = checkDigit,
				AddendaRecordIndicator = addendaList.Length > 0
					? AddendaIndicator.HasAddenda
					: AddendaIndicator.None
			};

			// sync sequence numbers and trace numbers
			Addendas = SyncAddendaRecords(detail.TraceNumber, addendaList);
		}

		public void UpdateTraceNumber(ulong newTraceNumber)
		{
			Detail = Detail with { TraceNumber = newTraceNumber };
			Addendas = SyncAddendaRecords(newTraceNumber, Addendas);
		}

		private static ImmutableList<AddendaRecord> SyncAddendaRecords(ulong traceNumber,
			IEnumerable<AddendaRecord> addendas)
		{
			var ads = addendas.ToArray();

			if (ads.Length == 0)
			{
				return [.. ads];
			}

			// extract the last 7 digits of the trace number for the addenda link
			var traceSequence = (int)(traceNumber % 10_000_000);

			var syncedList = new List<AddendaRecord>(ads.Length);

			for (var i = 0; i < ads.Length; ++i)
			{
				syncedList.Add(ads[i] with
				{
					// sequential numbering for multiple addendas (e.g. CTX)
					AddendaSequenceNumber = i + 1,
					// link back to the parent Entry's Trace Number
					EntryDetailSequenceNumber = traceSequence
				});
			}

			return [.. syncedList];
		}

		private static (string Routing, int CheckDigit) CalculateCheckDigit(string routing)
		{
			if (string.IsNullOrWhiteSpace(routing) || (routing.Length is not 8 and not 9))
			{
				throw new ArgumentException($"Routing number must be exactly 8 or 9 digits. " +
					$"Received: '{routing}'", nameof(routing));
			}

			var eightDigitRouting = routing[..8];

			// always calculate the check digit to guarantee file integrity
			ReadOnlySpan<int> weights = [3, 7, 1, 3, 7, 1, 3, 7];
			var sum = 0;

			for (int i = 0; i < weights.Length; ++i)
			{
				sum += (eightDigitRouting[i] - '0') * weights[i];
			}

			var calculatedCheckDigit = (10 - (sum % 10)) % 10;

			// length is 9, validate the provided check digit against the calculated one
			if (routing.Length == 9)
			{
				var providedCheckDigit = routing[8] - '0';

				// prevent the consumer from passing a mathematically invalid routing number
				if (providedCheckDigit != calculatedCheckDigit)
				{
					throw new ArgumentException($"The routing number '{routing}' has an invalid check digit. " +
						$"Expected '{calculatedCheckDigit}', got '{providedCheckDigit}'.",
						nameof(routing));
				}

				return (eightDigitRouting, providedCheckDigit);
			}

			// length is 8, return the sliced string and the newly calculated digit
			return (eightDigitRouting, calculatedCheckDigit);
		}
	}
}
