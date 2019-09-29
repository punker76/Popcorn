using NUnit.Framework;
using Popcorn.Converters;
using System.Windows;

namespace Popcorn.Tests.Converters
{
    [TestFixture]
    public class WindowStateToBooleanConverterTest
    {
        private WindowStateToBooleanConverter _converter;

        [OneTimeSetUp]
        public void InitializeConverter()
        {
            _converter = new WindowStateToBooleanConverter();
        }

        [Test]
        public void Convert_True_ReturnsMaximized()
        {
            Assert.AreEqual(
                _converter.Convert(true, null, null, null), WindowState.Maximized);
        }

        [Test]
        public void Convert_False_ReturnsNormal()
        {
            Assert.AreEqual(
                _converter.Convert(false, null, null, null), WindowState.Normal);
        }
    }
}
