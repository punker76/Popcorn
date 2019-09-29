using System.Globalization;
using AutoFixture;
using NUnit.Framework;
using Popcorn.Converters;

namespace Popcorn.Tests.Converters
{
    [TestFixture]
    public class RatioConverterTest
    {
        private RatioConverter _converter;

        [OneTimeSetUp]
        public void InitializeConverter()
        {
            _converter = new RatioConverter();
        }

        [Test]
        public void Convert_SimpleValue_ReturnsMultipliedValueWithRatio()
        {
            var fixture = new Fixture();
            var value = fixture.Create<double>();
            var parameter = fixture.Create<double>();

            var result = _converter.Convert(value, null, parameter, null);

            Assert.AreEqual(result, value*parameter);
            Assert.That(result, Is.TypeOf<double>());
        }
    }
}