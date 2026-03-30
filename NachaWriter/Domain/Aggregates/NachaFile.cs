using NachaWriter.Domain.Models;

namespace NachaWriter.Domain.Aggregates
{
	public sealed class NachaFile
	{
		public FileHeaderRecord Header { get; }

		public IReadOnlyList<NachaBatch> Batches { get; }

		public FileControlRecord Control => GenerateControlRecord();

		public NachaFile(FileHeaderRecord header, IEnumerable<NachaBatch> batches)
		{
			Header = header ?? throw new ArgumentNullException(nameof(header));

			var batchList = batches?.ToArray() ?? [];
			if (batchList.Length == 0)
			{
				throw new ArgumentException("An ACH file must contain at least one batch.");
			}

			Batches = [.. batchList];
		}

		private FileControlRecord GenerateControlRecord()
		{
			var batchCount = Batches.Count;
			var totalEntryAddendaCount = 0;
			var totalEntryHashSum = 0UL;
			var totalDebit = 0M;
			var totalCredit = 0M;

			// start line count at 2 (File Header + File Control)
			var totalLines = 2;

			foreach (var batchControl in Batches.Select(x => x.Control))
			{
				// Accumulate the totals from each Batch Control record
				totalEntryAddendaCount += batchControl.EntryAddendaCount;
				totalEntryHashSum += batchControl.EntryHash;
				totalDebit += batchControl.TotalDebitEntryDollarAmount;
				totalCredit += batchControl.TotalCreditEntryDollarAmount;

				// add the lines this batch takes up in the file
				// Batch Header (1) + Batch Control (1) + All Entries and Addendas
				totalLines += 2 + batchControl.EntryAddendaCount;
			}

			// File Entry Hash: Must be the 10 rightmost digits if the sum exceeds 10 digits
			var fileEntryHash = totalEntryHashSum % 10_000_000_000UL;

			// Block Count: Calculate padded lines based on the Blocking Factor of 10
			var paddedLines = (int)Math.Ceiling(totalLines / 10.0) * 10;
			var blockCount = paddedLines / 10;

			// return the automatically synchronized File Control record
			return new FileControlRecord
			{
				BatchCount = batchCount,
				BlockCount = blockCount,
				EntryAddendaCount = totalEntryAddendaCount,
				EntryHash = fileEntryHash,
				TotalDebitEntryDollarAmountInFile = totalDebit,
				TotalCreditEntryDollarAmountInFile = totalCredit
			};
		}
	}
}
