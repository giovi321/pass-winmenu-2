using System;
using PassWinmenu.Utilities.ExtensionMethods;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace PassWinmenu.Configuration.TypeConverters;

public class PascalCaseEnumConverter<T> : IYamlTypeConverter where T : struct, Enum
{
	public bool Accepts(Type type)
	{
		return type == typeof(T);
	}

	public object? ReadYaml(IParser parser, Type type)
	{
		var value = parser.Consume<Scalar>();
		
		return Enum.Parse<T>(value.Value.ToPascalCase());
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type)
	{
		throw new NotImplementedException();
	}
}
