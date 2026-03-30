using NachaWriter.Configuration;
using NachaWriter.Domain.Attributes;
using NachaWriter.Domain.Enums;
using System.Text;

namespace NachaWriter.Infrastructure.Serialization
{
	public sealed class FieldStreamWriter(Stream stream, bool leaveOpen = true)
		: IAsyncDisposable, IDisposable
	{
		private bool _disposed;

		private readonly StreamWriter _writer = new(stream, Encoding.ASCII, leaveOpen: leaveOpen);

		public async Task WriteAsync(string value, int length,
			FieldValidationMode validationMode = FieldValidationMode.None,
			bool padRight = true,
			CancellationToken cancellationToken = default)
		{
			ObjectDisposedException.ThrowIf(_disposed, typeof(FieldStreamWriter));
			ArgumentNullException.ThrowIfNull(value);

			if (value.Length > length)
			{
				value = value[..length];
			}

			EnsurePrintableAscii(value);
			FieldValidator.Validate(value, validationMode);

			var paddedValue = padRight ? value.PadRight(length, ' ') : value.PadLeft(length, ' ');

			await _writer.WriteAsync(paddedValue.AsMemory(), cancellationToken)
				.ConfigureAwait(false);
		}

		public async Task WriteAsync(ulong value, int length,
			FieldValidationMode validationMode = FieldValidationMode.None,
			CancellationToken cancellationToken = default)
		{
			ObjectDisposedException.ThrowIf(_disposed, typeof(FieldStreamWriter));
			ArgumentNullException.ThrowIfNull(value);

			var stringValue = value.ToString();
			if (stringValue.Length > length)
			{
				throw new InvalidOperationException(
					$"Value {stringValue} is too long for field length {length}.");
			}

			FieldValidator.Validate(stringValue, validationMode);

			await _writer.WriteAsync(stringValue.PadLeft(length, '0').AsMemory(),
				cancellationToken).ConfigureAwait(false);
		}

		public Task WriteAsync(DateOnly date, CancellationToken cancellationToken = default) =>
			WriteAsync($"{date:yyMMdd}", 6, FieldValidationMode.Numeric, cancellationToken: cancellationToken);

		public Task WriteAsync(TimeOnly time, CancellationToken cancellationToken = default) =>
			WriteAsync($"{time:HHmm}", 4, FieldValidationMode.Numeric, cancellationToken: cancellationToken);

		public async Task WriteAsync(CancellationToken cancellationToken = default)
		{
			ObjectDisposedException.ThrowIf(_disposed, typeof(FieldStreamWriter));
			await _writer.WriteAsync(NachaConstants.NewLine.AsMemory(),
				cancellationToken).ConfigureAwait(false);
		}

		public Task WriteAsync(Enum value, int length,
			FieldValidationMode validationMode = FieldValidationMode.None,
			CancellationToken cancellationToken = default)
		{
			ObjectDisposedException.ThrowIf(_disposed, typeof(FieldStreamWriter));
			ArgumentNullException.ThrowIfNull(value);

			var enumType = value.GetType();
			var attribute = Attribute.GetCustomAttribute(enumType,
				typeof(EnumFormatAttribute)) as EnumFormatAttribute;

			var format = attribute?.EnumFormat ?? EnumFormat.Numeric;

			return format == EnumFormat.Numeric ?
				WriteAsync(Convert.ToUInt64(value), length, validationMode, cancellationToken) :
				WriteAsync(value.ToString(), length, validationMode, cancellationToken: cancellationToken);
		}

		public async Task FlushAsync(CancellationToken cancellationToken = default)
		{
			ObjectDisposedException.ThrowIf(_disposed, typeof(FieldStreamWriter));
			await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
		}

		private static void EnsurePrintableAscii(string value)
		{
			for (var i = 0; i < value.Length; ++i)
			{
				if ((uint)(value[i] - 0x20) > (0x7E - 0x20))
				{
					throw new InvalidOperationException(
						$"Invalid non-ASCII character '{value[i]}' at index {i}.");
				}
			}
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_writer.Dispose();
				_disposed = true;
			}
		}

		public async ValueTask DisposeAsync()
		{
			if (!_disposed)
			{
				await _writer.DisposeAsync();
				_disposed = true;
			}
		}
	}
}
