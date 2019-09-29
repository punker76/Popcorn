using NUnit.Framework;
using Popcorn.Converters;

namespace Popcorn.Tests.Converters
{
    [TestFixture]
    public class NullAsBoolConverterTest
    {
        private NullAsBoolConverter _converter;

        [OneTimeSetUp]
        public void InitializeConverter()
        {
            _converter = new NullAsBoolConverter();
        }

        [Test]
        public void Convert_NullWithParamTrue_ReturnsTrue()
        {
            Assert.AreEqual(
                _converter.Convert(null, null, true, null), true);
        }

        [Test]
        public void Convert_NotNullWithParamTrue_ReturnsFalse()
        {
            Assert.AreEqual(
                _converter.Convert(new object(), null, true, null), false);
        }

        [Test]
        public void Convert_NullWithParamFalse_ReturnsTrue()
        {
            Assert.AreEqual(
                _converter.Convert(null, null, false, null), false);
        }

        [Test]
        public void Convert_NotNullWithParamFalse_ReturnsFalse()
        {
            Assert.AreEqual(
                _converter.Convert(new object(), null, false, null), true);
        }
    }
}