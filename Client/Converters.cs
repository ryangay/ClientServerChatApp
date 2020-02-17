using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Common;

namespace Client
{
    /// <summary>
    /// Converts a <see cref="bool"/> to a <see cref="Visibility"/>
    /// </summary>
    class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value as Visibility? == Visibility.Visible;
        }
    }

    /// <summary>
    /// Converts an <see cref="Array"/> of <see cref="Byte"/> to a Windows <see cref="ImageSource"/>
    /// </summary>
    class JpegStreamConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is byte[])
            {
                var decoder = BitmapDecoder.Create(new MemoryStream((byte[]) value),
                    BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);// new JpegBitmapDecoder(new MemoryStream((byte[]) value, false), BitmapCreateOptions.None,
                    //BitmapCacheOption.OnDemand);
                var imgFrame = decoder.Frames[0] as ImageSource;
                imgFrame.Freeze();

                return imgFrame;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an <see cref="Array"/> of <see cref="Byte"/> to a Windows <see cref="ImageSource"/>
    /// </summary>
    class PngStreamConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is byte[])
            {
                var decoder = BitmapDecoder.Create(new MemoryStream((byte[])value),
                    BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);// new PngBitmapDecoder(new MemoryStream((byte[])value), BitmapCreateOptions.None, BitmapCacheOption.OnDemand);
                var imgFrame = decoder.Frames[0] as ImageSource;
                imgFrame.Freeze();

                return imgFrame;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an <see cref="Array"/> of <see cref="Byte"/> to a <see cref="string"/>
    /// </summary>
    class TextStreamConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is byte[])
            {
                return Encoding.Unicode.GetString((byte[]) value);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class MessageAlignmentConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            var sentBy = value[0] as byte?;
            var self = value[1] as byte?;

            if (sentBy != null && self != null)
            {
                return sentBy == self;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class IsStringEmptyOrWhitespaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            if (text == null) return false;
            return string.IsNullOrWhiteSpace(text);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
