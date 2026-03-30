using NachaWriter.Domain.Aggregates;

namespace NachaWriter.Infrastructure.Serialization
{
	public sealed class NachaFileStreamWriter : IAsyncDisposable, IDisposable
	{
		private readonly RecordStreamWriter _recordWriter;
		private readonly bool _ownsRecordWriter;
		private bool _disposed;

		public NachaFileStreamWriter(RecordStreamWriter recordWriter)
		{
			_recordWriter = recordWriter ?? throw new ArgumentNullException(nameof(recordWriter));
			_ownsRecordWriter = false;
		}

		public NachaFileStreamWriter(Stream stream, bool leaveOpen = true)
		{
			ArgumentNullException.ThrowIfNull(stream);

			_recordWriter = new RecordStreamWriter(stream, leaveOpen);
			_ownsRecordWriter = true;
		}

		public async Task WriteAsync(NachaFile nachaFile, CancellationToken cancellationToken = default)
		{
			ObjectDisposedException.ThrowIf(_disposed, this);
			ArgumentNullException.ThrowIfNull(nachaFile);

			// write File Header
			await _recordWriter.WriteAsync(nachaFile.Header, cancellationToken).ConfigureAwait(false);

			foreach (var batch in nachaFile.Batches)
			{
				// write Batch Header
				await _recordWriter.WriteAsync(batch.Header, cancellationToken).ConfigureAwait(false);

				foreach (var entry in batch.Entries)
				{
					// write Entry Detail
					await _recordWriter.WriteAsync(entry.Detail, cancellationToken).ConfigureAwait(false);

					// write Addenda Records if they exist
					foreach (var addenda in entry.Addendas)
					{
						await _recordWriter.WriteAsync(addenda, cancellationToken).ConfigureAwait(false);
					}
				}

				// write Batch Control
				await _recordWriter.WriteAsync(batch.Control, cancellationToken).ConfigureAwait(false);
			}

			// write File Control
			await _recordWriter.WriteAsync(nachaFile.Control, cancellationToken).ConfigureAwait(false);

			// write Filler Lines
			await PadFileToBlockingFactorAsync(cancellationToken).ConfigureAwait(false);
		}

		private async Task PadFileToBlockingFactorAsync(CancellationToken cancellationToken)
		{
			while (_recordWriter.LineCount % 10 != 0)
			{
				await _recordWriter.WriteFillerLineAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				if (_ownsRecordWriter)
				{
					_recordWriter.Dispose();
				}
				_disposed = true;
			}
		}

		public async ValueTask DisposeAsync()
		{
			if (!_disposed)
			{
				if (_ownsRecordWriter)
				{
					await _recordWriter.DisposeAsync();
				}
				_disposed = true;
			}
		}
	}
}
