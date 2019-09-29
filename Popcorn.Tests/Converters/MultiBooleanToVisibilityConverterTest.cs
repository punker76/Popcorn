using NUnit.Framework;
using Popcorn.Converters;
using System.Windows;

namespace Popcorn.Tests.Converters
{
    [TestFixture]
    public class MultiBooleanToVisibilityConverterTest
    {
        private MultiBooleanToVisibilityConverter _converter;

        [OneTimeSetUp]
        public void InitializeConverter()
        {
            _converter = new MultiBooleanToVisibilityConverter();
        }

        [Test]
        public void Convert_AndAllTrue_ReturnsVisible()
        {
            object[] trueBooleans = { true, true, true };
            Assert.AreEqual(
                _converter.Convert(trueBooleans, null, "AND", null), Visibility.Visible);
        }

        [Test]
        public void Convert_AndAllFalse_ReturnsCollapsed()
        {
            object[] falseBooleans = { false, false, false };
            Assert.AreEqual(
                _converter.Convert(falseBooleans, null, "AND", null), Visibility.Collapsed);
        }

        [Test]
        public void Convert_AndNotAllTrue_ReturnsCollapsed()
        {
            object[] notAllTrueBooleans = { true, false, true };
            Assert.AreEqual(
                _converter.Convert(notAllTrueBooleans, null, "AND", null), Visibility.Collapsed);
        }


        [Test]
        public void Convert_OrAllTrue_ReturnsVisible()
        {
            object[] trueBooleans = { true, true, true };
            Assert.AreEqual(
                _converter.Convert(trueBooleans, null, "OR", null), Visibility.Visible);
        }

        [Test]
        public void Convert_OrAllFalse_ReturnsCollapsed()
        {
            object[] falseBooleans = { false, false, false };
            Assert.AreEqual(
                _converter.Convert(falseBooleans, null, "OR", null), Visibility.Collapsed);
        }

        [Test]
        public void Convert_OrNotAllTrue_ReturnsCollapsed()
        {
            object[] notAllTrueBooleans = { true, false, true };
            Assert.AreEqual(
                _converter.Convert(notAllTrueBooleans, null, "OR", null), Visibility.Visible);
        }
    }
}
