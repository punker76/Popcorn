using System.Windows;
using NUnit.Framework;
using Popcorn.Converters;

namespace Popcorn.Tests.Converters
{
    [TestFixture]
    public class BoolToVisibilityConverterTest
    {
        private BoolToVisibilityConverter _converter;

        [OneTimeSetUp]
        public void InitializeConverter()
        {
            _converter = new BoolToVisibilityConverter();
        }

        [Test]
        public void Convert_False_ReturnsVisible()
        {
            Assert.AreEqual(
                _converter.Convert(false, typeof (Visibility), null, null), Visibility.Visible);
        }

        [Test]
        public void Convert_True_ReturnsCollapsed()
        {
            Assert.AreEqual(
                _converter.Convert(true, typeof(Visibility), null, null), Visibility.Collapsed);
        }
    }
}