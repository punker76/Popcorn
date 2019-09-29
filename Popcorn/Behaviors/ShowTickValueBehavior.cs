using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Popcorn.Behaviors
{
    public class ShowTickValueBehavior : Behavior<Slider>
    {
        private Track track;

        public static readonly DependencyProperty PrefixProperty = DependencyProperty.Register(
            "Prefix",
            typeof(string),
            typeof(ShowTickValueBehavior),
            new PropertyMetadata(default(string)));

        public string Prefix
        {
            get
            {
                return (string)this.GetValue(PrefixProperty);
            }
            set
            {
                this.SetValue(PrefixProperty, value);
            }
        }

        protected override void OnAttached()
        {
            this.AssociatedObject.Loaded += this.AssociatedObjectOnLoaded;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            this.track.MouseMove -= this.TrackOnMouseMove;
            this.track = null;
            base.OnDetaching();
        }

        private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            this.AssociatedObject.Loaded -= this.AssociatedObjectOnLoaded;
            this.track = (Track)this.AssociatedObject.Template.FindName("PART_Track", this.AssociatedObject);
            this.track.MouseMove += this.TrackOnMouseMove;
        }

        private void TrackOnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            var position = mouseEventArgs.GetPosition(this.track);
            var valueFromPoint = this.track.ValueFromPoint(position);
            var floorOfValueFromPoint = (int)Math.Floor(valueFromPoint);
            var time = TimeSpan.FromMilliseconds(floorOfValueFromPoint);
            var toolTip = string.Format(CultureInfo.InvariantCulture, "{0}{1}", this.Prefix, time.ToString(@"hh\:mm\:ss"));

            ToolTipService.SetToolTip(this.track, toolTip);
        }
    }
}
