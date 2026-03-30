using NachaWriter.Configuration;
using NachaWriter.Domain.Attributes;
using NachaWriter.Domain.Enums;
using NachaWriter.Domain.Models;
using System.Collections.Concurrent;
using System.Reflection;

namespace NachaWriter.Infrastructure.Serialization
{
	public sealed class RecordStreamWriter : IAsyncDisposable, IDisposable
	{
		private const string NumericFieldCannotBeNegativeMessage = "Numeric field values cannot be negative.";

		private static readonly ConcurrentDictionary<Type, IReadOnlyList<(PropertyInfo, FieldAttribute)>> _fieldCache = new();
		private readonly FieldStreamWriter _fieldWriter;
		private readonly bool _ownsFieldWriter;
		private bool _disposed;

		public int LineCount { get; private set; }

		public RecordStreamWriter(FieldStreamWriter fieldWriter)
		{
			_fieldWriter = fieldWriter ?? throw new ArgumentNullException(nameof(fieldWriter));
			_ownsFieldWriter = false;
		}

		public RecordStreamWriter(Stream stream, bool leaveOpen = true)
		{
			ArgumentNullException.ThrowIfNull(stream);
			_fieldWriter = new FieldStreamWriter(stream, leaveOpen);
			_ownsFieldWriter = true;
		}

		public async Task WriteAsync<TRecord>(TRecord record, CancellationToken cancellationToken = default)
			where TRecord : IRecord
		{
			ObjectDisposedException.ThrowIf(_disposed, typeof(RecordStreamWriter));
			ArgumentNullException.ThrowIfNull(record);

			var fields = GetCachedRecordFields(record.GetType());

			foreach (var (property, field) in fields)
			{
				var value = property.GetValue(record);
				ArgumentNullException.ThrowIfNull(value);

				await WriteFieldAsync(value, field.Length, field.ValidationMode,
					field.IsRoutingNumber, cancellationToken).ConfigureAwait(false);
			}

			await _fieldWriter.WriteAsync(cancellationToken).ConfigureAwait(false);
			++LineCount;
		}

		public async Task WriteFillerLineAsync(CancellationToken cancellationToken = default)
		{
			ObjectDisposedException.ThrowIf(_disposed, typeof(RecordStreamWriter));

			var fillerString = new string(NachaConstants.FillerCharacter, NachaConstants.RecordLength);

			await _fieldWriter.WriteAsync(fillerString, NachaConstants.RecordLength,
				cancellationToken: cancellationToken).ConfigureAwait(false);

			await _fieldWriter.WriteAsync(cancellationToken).ConfigureAwait(false);
			++LineCount;
		}

		private Task WriteFieldAsync(object value, int length,
			FieldValidationMode validationMode,
			bool isRoutingNumber,
			CancellationToken cancellationToken = default) =>
			value switch
			{
				string s => _fieldWriter.WriteAsync(s, length, validationMode, !isRoutingNumber, cancellationToken),
				char c => _fieldWriter.WriteAsync(c.ToString(), length, validationMode, cancellationToken: cancellationToken),
				DateOnly date => _fieldWriter.WriteAsync(date, cancellationToken),
				TimeOnly time => _fieldWriter.WriteAsync(time, cancellationToken),
				Enum e => _fieldWriter.WriteAsync(e, length, validationMode, cancellationToken),
				decimal amount => WriteDecimalAsync(amount, length, validationMode, cancellationToken),
				_ => _fieldWriter.WriteAsync(ConvertUnsigned(value), length, validationMode, cancellationToken)
			};

		private Task WriteDecimalAsync(decimal amount, int length,
			FieldValidationMode validationMode, CancellationToken cancellationToken = default)
		{
			var scaledAmount = decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
			if (scaledAmount < 0)
			{
				throw new InvalidOperationException(NumericFieldCannotBeNegativeMessage);
			}

			return _fieldWriter.WriteAsync(decimal.ToUInt64(scaledAmount), length, validationMode,
				cancellationToken);
		}

		private static ulong ConvertUnsigned(object value) =>
			  value switch
			  {
				  byte b => b,
				  short s when s < 0 => throw new InvalidOperationException(NumericFieldCannotBeNegativeMessage),
				  short s when s >= 0 => (ulong)s,
				  int i when i < 0 => throw new InvalidOperationException(NumericFieldCannotBeNegativeMessage),
				  int i when i >= 0 => (ulong)i,
				  long l when l < 0 => throw new InvalidOperationException(NumericFieldCannotBeNegativeMessage),
				  long l when l >= 0 => (ulong)l,
				  ushort us => us,
				  uint ui => ui,
				  ulong ul => ul,
				  _ => throw new InvalidOperationException(
						 $"Unsupported value type '{value.GetType().Name}' for unsigned conversion.")
			  };

		private static IReadOnlyList<(PropertyInfo Property, FieldAttribute Field)> GetCachedRecordFields(
			Type recordType)
		{
			return _fieldCache.GetOrAdd(recordType, type =>
			{
				var props = type
					.GetProperties(BindingFlags.Instance | BindingFlags.Public)
					.Select(property => new
					{
						Property = property,
						Field = property.GetCustomAttribute<FieldAttribute>(inherit: true)
					})
					.Where(x => x.Field is not null)
					.Select(x => (x.Property, Field: x.Field!))
					.ToArray();

				if (props.Length == 0)
				{
					throw new InvalidOperationException(
						$"Record type '{type.Name}' has no fields decorated with FieldAttribute.");
				}

				var duplicateOrder = props
					.GroupBy(x => x.Field.Order)
					.FirstOrDefault(group => group.Count() > 1);

				if (duplicateOrder is not null)
				{
					throw new InvalidOperationException(
						$"Record type '{type.Name}' has duplicate field order '{duplicateOrder.Key}'.");
				}

				var totalLength = props.Sum(x => x.Field.Length);
				if (totalLength != NachaConstants.RecordLength)
				{
					throw new InvalidOperationException(
						$"Record type '{type.Name}' field lengths total {totalLength}, " +
						$"expected {NachaConstants.RecordLength}.");
				}

				return [.. props.OrderBy(x => x.Field.Order)];
			});
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				if (_ownsFieldWriter)
				{
					_fieldWriter.Dispose();
				}
				_disposed = true;
			}
		}

		public async ValueTask DisposeAsync()
		{
			if (!_disposed)
			{
				if (_ownsFieldWriter)
				{
					await _fieldWriter.DisposeAsync();
				}
				_disposed = true;
			}
		}
	}
}
