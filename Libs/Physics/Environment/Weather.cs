using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Physics.Environment
{
    public static class Weather
    {
        public static double ZeroKelvin
            => 273.15;

        private static double A(double t)
           => t >= 0 ? 7.5d : 7.6d;
        private static double B(double t)
            => t >= 0 ? 237.3d : 240.7d;

        /// <summary>
        /// Saturated Vapor Pressure
        /// </summary>
        /// <param name="t">Temperature in Degree</param>
        /// <returns>Saturated Vapor Pressure in hPa</returns>
        public static double SVP(double t)
            => 6.1078 * Math.Pow(10, (A(t) * t) / (B(t) + t));

        /// <summary>
        /// Vapor Pressure
        /// </summary>
        /// <param name="t">Temperature in Degree</param>
        /// <param name="r"></param>
        /// <returns>Vapor Pressure in hPa</returns>
        public static double VP(double t, double r)
        => SVP(t) * (r / 100);

        public static double V(double t, double r)
            => Math.Log10(VP(t, r) / 6.1078);

        /// <summary>
        /// Dew Point
        /// </summary>
        /// <param name="t">Temperature in Degree</param>
        /// <param name="r">Relative Humidity in Percent</param>
        /// <returns>Dew Point in Degree</returns>
        public static double DewPoint(double t, double r)
        {
            double v = V(t, r);
            return (B(t) * v) / (A(t) - v);
        }

        /// <summary>
        /// Absolute Humidity in gramm
        /// </summary>
        /// <param name="t">Temperatur in Degress</param>
        /// <param name="r">Relative Humidity in Percent</param>
        /// <returns>Absolute Humidity in gramm</returns>
        public static double AbsolutHumidity(double t, double r)
           => 100000 * 18.016d / 8314.3d * VP(t, r) / (t + ZeroKelvin);
    }
}
