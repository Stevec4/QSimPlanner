﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;
using static QSP.AviationTools.ConversionTools;
using static QSP.MathTools.Integers;

namespace QSP.AviationTools
{
    public static class SpeedConversion
    {
        public static double TasKnots(double mach, double altFt)
        {
            return 39.0 * mach * Sqrt(IsaTemp(altFt) + 273.0);
        }

        public static double Ktas(double kias, double altFt)
        {
            double eas = KcasToKeas(kias, altFt);
            return eas * Sqrt(AirDensity(0.0) / AirDensity(altFt));
        }

        public static double KcasToKeas(double kcas, double altFt)
        {
            double eas = kcas;
            var delta = Delta(altFt);
            double mach = 0.0;
            const int iterationCount = 5;

            // Use iteration to improve accuracy.
            for (int i = 0; i < iterationCount; i++)
            {
                mach = MachNumber(eas, delta);
                eas = kcas / ConversionFactor(delta, mach);
            }

            return eas;
        }

        private static double Delta(double altitudeFt)
        {
            return PressureMb(altitudeFt) / PressureMb(0.0);
        }

        private static double ConversionFactor(double delta, double mach)
        {
            return 1.0 + 1.0 / 8.0 * (1.0 - delta) * mach * mach +
                    3.0 / 640.0 * (1.0 - 10.0 * delta + 9 * delta * delta)
                    * Pow(mach, 4);
        }

        /// <summary>
        /// Returns the calibrated airspeed in knots.
        /// </summary>
        public static double Kcas(double mach, double altFt)
        {
            var delta = Delta(altFt);
            var eas = MachToKeas(mach, delta);
            return eas * ConversionFactor(delta, mach);
        }

        /// <summary>
        /// Returns the equivalent airspeed in knots.
        /// </summary>
        public static double MachToKeas(double mach, double delta)
        {
            const double standardSoundSpeedKnots = 661.47;
            return mach * standardSoundSpeedKnots * Sqrt(delta);
        }

        public static double MachNumber(double keas, double delta)
        {
            const double standardSoundSpeedKnots = 661.47;
            return keas / standardSoundSpeedKnots / Sqrt(delta);
        }
    }
}
