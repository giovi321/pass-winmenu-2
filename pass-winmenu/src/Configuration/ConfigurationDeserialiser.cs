using System.IO;
using PassWinmenu.Configuration.TypeConverters;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PassWinmenu.Configuration
{
	public static class ConfigurationDeserialiser
	{
		private static readonly IDeserializer Deserialiser = new DeserializerBuilder()
			.WithNamingConvention(HyphenatedNamingConvention.Instance)
			.WithTypeConverter(new WidthConverter())
			.WithTypeConverter(new BrushConverter())
			.WithTypeConverter(new SizeConverter())
			.WithTypeConverter(new PascalCaseEnumConverter<HotkeyAction>())
			.WithTypeConverter(new PascalCaseEnumConverter<PasswordGenerationStyle>())
			.WithTypeConverter(new PascalCaseEnumConverter<XkcdCapitalisation>())
			.Build();

		public static T Deserialise<T>(StreamReader reader)
		{
			return Deserialiser.Deserialize<T>(reader);
		}
	}
}
