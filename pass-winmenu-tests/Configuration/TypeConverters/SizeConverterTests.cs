using PassWinmenu.Configuration;
using PassWinmenu.Configuration.TypeConverters;
using Shouldly;
using Xunit;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace PassWinmenuTests.Configuration.TypeConverters;

public class SizeConverterTests
{
	[Theory]
	[InlineData("0", 0)]
	[InlineData("1.77", 1.77)]
	[InlineData("1.2e-3", 1.2e-3)]
	public void Deserialize_NoPercentSymbol_Pixels(string size, double expected)
	{
		var des = GetDeserialiser();

		var obj = des.Deserialize<WrapperObject>("Size: " + size);

		obj.Size.ShouldBe(new Size.Pixels(expected));
	}

	[Theory]
	[InlineData("0%", 0)]
	[InlineData("33%", 33)]
	[InlineData("1.5%", 1.5)]
	public void Deserialize_PercentSymbol_Percentage(string size, double expected)
	{
		var des = GetDeserialiser();

		var obj = des.Deserialize<WrapperObject>("Size: " + size);

		obj.Size.ShouldBe(Size.Percent.FromPercentage(expected));
	}

	[Theory]
	[InlineData("text")]
	public void Deserialize_InvalidInput_ThrowsException(string size)
	{
		var des = GetDeserialiser();

		Should.Throw<YamlException>(() => des.Deserialize<WrapperObject>("Size: " + size));
	}

	private class WrapperObject
	{
		public Size Size { get; init; }
	}

	private static IDeserializer GetDeserialiser()
	{
		return new DeserializerBuilder()
			.WithTypeConverter(new SizeConverter())
			.Build();
	}
}
