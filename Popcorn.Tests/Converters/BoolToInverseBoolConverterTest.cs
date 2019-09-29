using NUnit.Framework;
using Popcorn.Converters;

namespace Popcorn.Tests.Converters
{
    [TestFixture]
    public class BoolToInverseBoolConverterTest
    {
        private BoolToInverseBoolConverter _converter;

        [OneTimeSetUp]
        public void InitializeConverter()
        {
            _converter = new BoolToInverseBoolConverter();
        }

        [Test]
        public void Convert_True_ReturnsFalse()
        {
            Assert.AreEqual(
                _converter.Convert(true, null, null, null), false);
        }

        [Test]
        public void Convert_False_ReturnsTrue()
        {
            Assert.AreEqual(
                _converter.Convert(false, null, null, null), true);
        }
    }
}
