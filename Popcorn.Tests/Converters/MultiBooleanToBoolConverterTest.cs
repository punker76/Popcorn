using NUnit.Framework;
using Popcorn.Converters;

namespace Popcorn.Tests.Converters
{
    [TestFixture]
    public class MultiBooleanToBoolConverterTest
    {
        private MultiBooleanToBoolConverter _converter;

        [OneTimeSetUp]
        public void InitializeConverter()
        {
            _converter = new MultiBooleanToBoolConverter();
        }

        [Test]
        public void Convert_AllTrue_ReturnsTrue()
        {
            object[] trueBooleans = { true, true, true };
            Assert.AreEqual(
                _converter.Convert(trueBooleans, null, null, null), true);
        }

        [Test]
        public void Convert_AllFalse_ReturnsFalse()
        {
            object[] falseBooleans = { false, false, false };
            Assert.AreEqual(
                _converter.Convert(falseBooleans, null, null, null), false);
        }

        [Test]
        public void Convert_NotAllTrue_ReturnsTrue()
        {
            object[] notAllTrueBooleans = { true, false, true };
            Assert.AreEqual(
                _converter.Convert(notAllTrueBooleans, null, null, null), true);
        }
    }
}
