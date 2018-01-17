using System;
using System.Globalization;

using Sulakore.Network;

namespace Tanji.Helpers.Converters
{
    public class HotelEndPointConverter : SingleConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((value as HotelEndPoint)?.ToString() ?? "*:*");
        }
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string address = value.ToString();
            string[] points = address.Split(':');

            if (points.Length < 2)
            {
                return null;
            }

            ushort port = 0;
            HotelEndPoint endpoint = null;
            if (!ushort.TryParse(points[1], out port) ||
                !HotelEndPoint.TryParse(points[0], port, out endpoint))
            {
                return null;
            }
            return endpoint;
        }
    }
}