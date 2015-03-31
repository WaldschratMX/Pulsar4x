using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.Helpers
{
    /// <summary>
    /// Small Helper Class for Angle unit Conversions
    /// </summary>
    public static class Angle
    {
        public static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public static double ToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }
    }

    /// <summary>
    /// Small helper class for Temperature unit conversions
    /// </summary>
    public static class Temperature
    {
        public static double ToKelvin(double celsius)
        {
            return celsius + GameSettings.Units.DegreesCToKelvin;
        }

        public static float ToKelvin(float celsius)
        {
            return (float)(celsius + GameSettings.Units.DegreesCToKelvin);
        }

        public static double ToCelsius(double kelvin)
        {
            return kelvin + GameSettings.Units.KelvinToDegreesC;
        }

        public static float ToCelsius(float kelvin)
        {
            return (float)(kelvin + GameSettings.Units.KelvinToDegreesC);
        }
    }

    /// <summary>
    /// Small helper class for Distance unit conversions
    /// </summary>
    public static class Distance
    {
        public static double ToAU(double km)
        {
            return km / GameSettings.Units.KmPerAu;
        }

        public static double ToKm(double au)
        {
            return au * GameSettings.Units.KmPerAu;
        }
    }


    public class WeightedValue<T>
    {
        public double Weight { get; set; }
        public T Value { get; set; }
    }

    /// <summary>
    /// Weighted list used for selecting values with a random number generator.
    /// </summary>
    /// <remarks>
    /// This is a weighted list. Input values do not need to add up to 1.
    /// </remarks>
    /// <example>
    /// <code>
    /// WeightedList<string> fruitList = new WeightList<string>();
    /// fruitList.Add(0.2, "Apple");
    /// fruitList.Add(0.5, "Banana");
    /// fruitList.Add(0.3, "Tomatoe");
    /// 
    /// fruitSelection = fruitList.Select(0.1)
    /// print(fruitSelection); // "Apple"
    /// 
    /// fruitSelection = fruitList.Select(0.69)
    /// print(fruitSelection); // "Banana"
    /// 
    /// string fruitSelection = fruitList.Select(0.7)
    /// print(fruitSelection); // "Tomatoe"
    /// </code>
    /// </example>
    /// <example>
    /// <code>
    /// WeightedList<string> fruitList = new WeightList<string>();
    /// fruitList.Add(4, "Apple");
    /// fruitList.Add(6, "Banana");
    /// fruitList.Add(10, "Tomatoe");
    /// 
    /// fruitSelection = fruitList.Select(0.19)
    /// print(fruitSelection); // "Apple"
    /// 
    /// fruitSelection = fruitList.Select(0.2)
    /// print(fruitSelection); // "Banana"
    /// 
    /// string fruitSelection = fruitList.Select(0.5)
    /// print(fruitSelection); // "Tomatoe"
    /// </code>
    /// </example>
    /// 
    public class WeightedList<T> : IEnumerable<WeightedValue<T>>
    {
        List<WeightedValue<T>> m_valueList;

        /// <summary>
        /// Total weights of the list.
        /// </summary>
        public double TotalWeight { get { return m_totalWeight; } }
        double m_totalWeight;

        public WeightedList()
        {
            m_valueList = new List<WeightedValue<T>>();
        }

        /// <summary>
        /// Adds a value to the weighted list.
        /// </summary>
        /// <param name="weight">Weight of this value in the list.</param>
        public void Add(double weight, T value)
        {
            WeightedValue<T> listEntry = new WeightedValue<T>();
            listEntry.Weight = weight;
            listEntry.Value = value;

            m_valueList.Add(listEntry);

            m_totalWeight += weight;
        }

        public IEnumerator<WeightedValue<T>> GetEnumerator() 
        {
            return m_valueList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Selects a value from the list based on the input.
        /// </summary>
        /// <param name="rngValue">Value 0.0 to 1.0 represending the random value selected by the RNG.</param>
        /// <returns></returns>
        public T Select(double rngValue)
        {
            double cumulativeChance = 0;
            foreach (WeightedValue<T> listEntry in m_valueList)
            {
                double realChance = listEntry.Weight / m_totalWeight;
                cumulativeChance += realChance;

                if (rngValue < cumulativeChance)
                {
                    return listEntry.Value;
                }
            }
            throw new InvalidOperationException("Failed to choose a random value.");
        }

        /// <summary>
        /// Selects the value at the specified index.
        /// </summary>
        public T SelectAt(int index)
        {
            return m_valueList[index].Value;
        }
    }


    /// <summary>
    /// Small helper struct to make all these min/max dicts. nicer.
    /// </summary>
    public struct MinMaxStruct 
    { 
        public double Min, Max;

        public MinMaxStruct(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// Just a container for some general math functions.
    /// </summary>
    public class GMath
    {
        /// <summary>
        /// Clamps a value between the provided man and max.
        /// </summary>
        public static double Clamp(double value, double min, double max)
        {
            if (value > max)
                return max;
            else if (value < min)
                return min;

            return value;
        }

        /// <summary>
        /// Clamps a number between 0 and 1.
        /// </summary>
        public static double Clamp01(double value)
        {
            return Clamp(value, 0, 1);
        }

        /// <summary>
        /// Selects a number from a range based on the selection percentage provided.
        /// </summary>
        public static double SelectFromRange(MinMaxStruct minMax, double selection)
        {
            return minMax.Min + selection * (minMax.Max - minMax.Min); ;
        }

        /// <summary>
        /// Returns the next Double from m_RNG adjusted to be between the min and max range.
        /// </summary>
        public static double RNG_NextDoubleRange(Random RNG, double min, double max)
        {
            return (min + RNG.NextDouble() * (max - min));
        }

        /// <summary>
        /// Version of RNG_NextDoubleRange(double min, double max) that takes GalaxyGen.MinMaxStruct directly.
        /// </summary>
        public static double RNG_NextDoubleRange(Random RNG, MinMaxStruct minMax)
        {
            return RNG_NextDoubleRange(RNG, minMax.Min, minMax.Max);
        }

        /// <summary>
        /// Raises the random number generated to the power provided to produce a non-uniform selection from the range.
        /// </summary>
        public static double RNG_NextDoubleRangeDistributedByPower(Random RNG, double min, double max, double power)
        {
            return min + Math.Pow(RNG.NextDouble(), power) * (max - min);
        }

        /// <summary>
        /// Version of RNG_NextDoubleRangeDistributedByPower(double min, double max, double power) that takes GalaxyGen.MinMaxStruct directly.
        /// </summary>
        public static double RNG_NextDoubleRangeDistributedByPower(Random RNG, MinMaxStruct minMax, double power)
        {
            return RNG_NextDoubleRangeDistributedByPower(RNG, minMax.Min, minMax.Max, power);
        }

        /// <summary>
        /// Returns a value between the min and max.
        /// </summary>
        public static uint RNG_NextRange(Random RNG, MinMaxStruct minMax)
        {
            return (uint)RNG.Next((int)minMax.Min, (int)minMax.Max);
        }

        /// <summary>
        /// Randomly reverses the current sign of the value, i.e. it will randomly make the number positive or negative.
        /// </summary>
        public static double RNG_RandomizeSign(Random RNG, double value)
        {
            // 50/50 odds of reversing the sign:
            if (RNG.NextDouble() > 0.5)
                return value * -1;

            return value;
        }

        /// <summary>
        /// Very simple random shuffle.
        /// </summary>
        public static void RandomShuffle<T>(Random RNG, List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = RNG.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
