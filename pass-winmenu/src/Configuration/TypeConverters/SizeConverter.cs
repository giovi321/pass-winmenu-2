using System;
using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace PassWinmenu.Configuration.TypeConverters
{
	internal class SizeConverter : IYamlTypeConverter
	{
		public bool Accepts(Type type)
		{
			return type == typeof(Size);
		}

		public object ReadYaml(IParser parser, Type type)
		{
			// A size should be represented as a string value.
			var value = parser.Consume<Scalar>();
			var str = value.Value;

			if (str.EndsWith("%", StringComparison.Ordinal))
			{
				var percentage = double.Parse(str[..^1], CultureInfo.InvariantCulture);
				return Size.Percent.FromPercentage(percentage);
			}

			return new Size.Pixels(double.Parse(str, CultureInfo.InvariantCulture));
		}

		public void WriteYaml(IEmitter emitter, object? value, Type type)
		{
			throw new NotSupportedException();
		}
	}
}
