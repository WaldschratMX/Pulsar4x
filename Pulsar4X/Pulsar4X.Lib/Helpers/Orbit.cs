using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Pulsar4X.Entities;
using Pulsar4X.Helpers.GameMath;
using Pulsar4X.Helpers;
using System.ComponentModel;

namespace Pulsar4X.Entities
{
    /// <summary>
    /// Calculates and handles orbits for bodies.
    /// All angles stored in Degrees, but calculated in Radians.
    /// </summary>
	public class Orbit
    {

        #region Properties

        /// <summary>
        /// Mass in KG of this entity.
        /// </summary>
        public double Mass { get { return m_mass; } }
        private double m_mass;

        /// <summary>
        /// Mass in Earth Masses of this entity.
        /// </summary>
        public double MassRelativeToEarth { get { return m_mass / Constants.Units.EarthMassInKG; } }

        /// <summary>
        /// Mass in Solar Masses of this entity.
        /// </summary>
        public double MassRelativeToSol { get { return m_mass / Constants.Units.SolarMassInKG; } }

        /// <summary>
        /// Mass in KG of parent (object this orbit orbits)
        /// </summary>
        public double ParentMass { get { return m_parentMass; } }
        private double m_parentMass;

        /// <summary>
        /// Semimajor Axis of orbit stored in AU.
        /// Average distance of orbit from center.
        /// </summary>
		public double SemiMajorAxis { get; set; } 

        /// <summary>
        /// Eccentricity of orbit.
        /// Shape of the orbit. 0 = perfectly circular, 1 = parabolic.
        /// </summary>
		public double Eccentricity { get; set; }

        /// <summary>
        /// Angle between the orbit and the flat referance plane.
        /// Stored in degrees.
        /// </summary>
		public double Inclination { get; set; }

        /// <summary>
        /// Horizontal orientation of the point where the orbit crosses
        /// the referance frame stored in degrees.
        /// </summary>
		public double LongitudeOfAscendingNode { get; set; }

        /// <summary>
        /// Angle from the Ascending Node to the Periapsis stored in degrees.
        /// </summary>
		public double ArgumentOfPeriapsis { get; set; }

        /// <summary>
        /// Definition of the position of the body in the orbit at the referance time
        /// epoch. Mathematically convienant angle does not correspond to a real angle.
        /// Stored in degrees.
        /// </summary>
		public double MeanAnomaly { get; set; }

        /// <summary>
        /// Referance time. Orbital parameters are stored relative to this referance.
        /// </summary>
		public DateTime Epoch { get; set; }

        /// <summary>
        /// 2-Body gravitational parameter of system.
        /// </summary>
        public double GravitationalParameter { get { return m_gravitationalParameter; } }
        private double m_gravitationalParameter;

        /// <summary>
        /// Orbital Period of orbit.
        /// </summary>
        public TimeSpan OrbitalPeriod { get { return m_orbitalPeriod; } }
        private TimeSpan m_orbitalPeriod;

        /// <summary>
        /// Mean Motion of orbit. Stored as Degrees/Sec.
        /// </summary>
        public double MeanMotion { get { return m_meanMotion; } }
        private double m_meanMotion;

        /// <summary>
        /// Point in orbit furthest from the ParentBody. Measured in AU.
        /// </summary>
        public double Apoapsis { get { return (1 + Eccentricity) * SemiMajorAxis; } }

        /// <summary>
        /// Point in orbit closest to the ParentBody. Measured in AU.
        /// </summary>
        public double Periapsis { get { return (1 - Eccentricity) * SemiMajorAxis; } }

        /// <summary>
        /// Stationary orbits don't have all of the data to update. They always return (0, 0).
        /// </summary>
        private bool m_isStationary;

        #endregion

        #region Orbit Construction Interface

        /// <summary>
        /// Returns an orbit representing the defined parameters.
        /// </summary>
        /// <param name="mass">Mass of this object in KG.</param>
        /// <param name="parentMass">Mass of parent object in KG.</param>
        /// <param name="semiMajorAxis">SemiMajorAxis of orbit in AU.</param>
        /// <param name="eccentricity">Eccentricity of orbit.</param>
        /// <param name="inclination">Inclination of orbit in degrees.</param>
        /// <param name="longitudeOfAscendingNode">Longitude of ascending node in degrees.</param>
        /// <param name="longitudeOfPeriapsis">Longitude of periapsis in degrees.</param>
        /// <param name="meanLongitude">Longitude of object at epoch in degrees.</param>
        /// <param name="epoch">Referance time for these orbital elements.</param>
        public static Orbit FromMajorPlanetFormat(double mass, double parentMass, double semiMajorAxis, double eccentricity, double inclination,
                                                    double longitudeOfAscendingNode, double longitudeOfPeriapsis, double meanLongitude, DateTime epoch)
        {
            // http://en.wikipedia.org/wiki/Longitude_of_the_periapsis
            double argumentOfPeriapsis = longitudeOfPeriapsis - longitudeOfAscendingNode;
            // http://en.wikipedia.org/wiki/Mean_longitude
            double meanAnomaly = meanLongitude - (longitudeOfAscendingNode + argumentOfPeriapsis);

            return new Orbit(mass, parentMass, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, epoch);
        }

        /// <summary>
        /// Returns an orbit representing the defined parameters.
        /// </summary>
        /// <param name="mass">Mass of this object in KG.</param>
        /// <param name="parentMass">Mass of parent object in KG.</param>
        /// <param name="semiMajorAxis">SemiMajorAxis of orbit in AU.</param>
        /// <param name="eccentricity">Eccentricity of orbit.</param>
        /// <param name="inclination">Inclination of orbit in degrees.</param>
        /// <param name="longitudeOfAscendingNode">Longitude of ascending node in degrees.</param>
        /// <param name="argumentOfPeriapsis">Argument of periapsis in degrees.</param>
        /// <param name="meanAnomaly">Mean Anomaly in degrees.</param>
        /// <param name="epoch">Referance time for these orbital elements.</param>
        public static Orbit FromAsteroidFormat(double mass, double parentMass, double semiMajorAxis, double eccentricity, double inclination,
                                                double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomaly, DateTime epoch)
        {
            return new Orbit(mass, parentMass, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, epoch);
        }

        /// <summary>
        /// Creates an orbit that never moves.
        /// </summary>
        public static Orbit FromStationary(double mass)
        {
            return new Orbit(mass);
        }

        /// <summary>
        /// Constructor for stationary orbits.
        /// </summary>
        private Orbit(double mass)
        {
            m_mass = mass;
            SemiMajorAxis = 0;
            Eccentricity = 0;
            m_isStationary = true;
        }

        /// <summary>
        /// Constructor for the orbit.
        /// Calculates commonly-used parameters for future use.
        /// </summary>
        private Orbit(double mass, double parentMass, double semiMajorAxis, double eccentricity, double inclination,
                        double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomaly, DateTime epoch)
        {
            m_mass = mass;
            m_parentMass = parentMass;
            SemiMajorAxis = semiMajorAxis;
            Eccentricity = Math.Min(eccentricity, 0.8D); // Max eccentricity is 0.8 Orbit code has issues at higher eccentricity. (Note: If restriction lifed, fix code in GetEccentricAnomaly)
            Inclination = inclination;
            LongitudeOfAscendingNode = longitudeOfAscendingNode;
            ArgumentOfPeriapsis = argumentOfPeriapsis;
            MeanAnomaly = meanAnomaly;
            Epoch = epoch;
            m_isStationary = false;

            // Calculate extended parameters.

            // http://en.wikipedia.org/wiki/Standard_gravitational_parameter#Two_bodies_orbiting_each_other
            m_gravitationalParameter = Constants.Science.GravitationalConstant * (ParentMass + Mass) / (1000 * 1000 * 1000); // Normalize GravitationalParameter from m^3/s^2 to km^3/s^2

            // http://en.wikipedia.org/wiki/Orbital_period#Two_bodies_orbiting_each_other
            double orbitalPeriod = 2 * Math.PI * Math.Sqrt(Math.Pow(Distance.ToKm(SemiMajorAxis), 3) / (GravitationalParameter));
            if (orbitalPeriod * 10000000 > Int64.MaxValue)
            {
                m_orbitalPeriod = TimeSpan.MaxValue;
            }
            else
            {
                m_orbitalPeriod = TimeSpan.FromSeconds(orbitalPeriod);
            }

            // http://en.wikipedia.org/wiki/Mean_motion
            m_meanMotion = Math.Sqrt(GravitationalParameter / Math.Pow(Distance.ToKm(SemiMajorAxis), 3)); // Calculated in radians.
            m_meanMotion = Angle.ToDegrees(m_meanMotion); // Stored in degrees.
        }

        #endregion

        #region Orbit Position Calculations

        /// <summary>
        /// Calculates the parent-relative cartesian coordinate of an orbit for a given time.
        /// </summary>
        public void GetPosition(DateTime time, out double x, out double y)
		{
            if (m_isStationary)
            {
                x = 0;
                y = 0;
                return;
            }

			TimeSpan timeSinceEpoch = time - Epoch;

            while (timeSinceEpoch > m_orbitalPeriod)
			{
                // Don't attempt to calculate large timeframes.
				timeSinceEpoch -= m_orbitalPeriod;
                Epoch += m_orbitalPeriod;
			}

            // http://en.wikipedia.org/wiki/Mean_anomaly (M = M0 + nT)
            // Convert MeanAnomaly to radians.
            double currentMeanAnomaly = Angle.ToRadians(MeanAnomaly); 
            // Add nT
			currentMeanAnomaly += Angle.ToRadians(MeanMotion) * timeSinceEpoch.TotalSeconds;

			double EccentricAnomaly = GetEccentricAnomaly(currentMeanAnomaly);

            // http://en.wikipedia.org/wiki/True_anomaly#From_the_eccentric_anomaly
			double TrueAnomaly = Math.Atan2(Math.Sqrt(1 - Eccentricity * Eccentricity) * Math.Sin(EccentricAnomaly), Math.Cos(EccentricAnomaly) - Eccentricity);

            GetPosition(TrueAnomaly, out x, out y);
		}

        /// <summary>
        /// Calculates the cartesian coordinates (relative to it's parent) of an orbit for a given angle.
        /// </summary>
        /// <param name="TrueAnomaly">Angle in Radians.</param>
        public void GetPosition(double TrueAnomaly, out double x, out double y)
        {
            if (m_isStationary)
            {
                x = 0;
                y = 0;
                return;
            }

            // http://en.wikipedia.org/wiki/True_anomaly#Radius_from_true_anomaly
            double radius = Distance.ToKm(SemiMajorAxis) * (1 - Eccentricity * Eccentricity) / (1 + Eccentricity * Math.Cos(TrueAnomaly));

            // Adjust TrueAnomaly by the Argument of Periapsis (converted to radians)
            TrueAnomaly += Angle.ToRadians(ArgumentOfPeriapsis);

            // Convert KM to AU
            radius = Distance.ToAU(radius);

            // Polar to Cartesian conversion.
            x = radius * Math.Cos(TrueAnomaly);
            y = radius * Math.Sin(TrueAnomaly);
        }

		/// <summary>
		/// Calculates the current Eccentric Anomaly given certain orbital parameters.
		/// </summary>
		private double GetEccentricAnomaly(double currentMeanAnomaly)
		{
			//Kepler's Equation
			List<double> E = new List<double>();
			double Epsilon = 1E-12; // Plenty of accuracy.
			/* Eccentricity is currently clamped @ 0.8
            if (Eccentricity > 0.8)
			{
				E.Add(Math.PI);
			} else
            */
			{
				E.Add(currentMeanAnomaly);
			}
			int i = 0;

			do
			{
				// Newton's Method.
                /*					 E(n) - e sin(E(n)) - M(t)
                 * E(n+1) = E(n) - ( ------------------------- )
                 *					      1 - e cos(E(n)
                 * 
                 * E == EccentricAnomaly, e == Eccentricity, M == MeanAnomaly.
                 * http://en.wikipedia.org/wiki/Eccentric_anomaly#From_the_mean_anomaly
                */
                E.Add(E[i] - ((E[i] - Eccentricity * Math.Sin(E[i]) - currentMeanAnomaly) / (1 - Eccentricity * Math.Cos(E[i]))));
				i++;
			} while (Math.Abs(E[i] - E[i - 1]) > Epsilon && i < 1000);

			if (i > 1000)
			{
				// <? todo: Flag an error about non-convergence of Newton's method.
			}

            double eccentricAnomaly = E[i - 1];

			return E[i - 1];
        }

        #endregion

    }


    #region Data Binding

    /// <summary>
    /// Used for databinding, see here: http://blogs.msdn.com/b/msdnts/archive/2007/01/19/how-to-bind-a-datagridview-column-to-a-second-level-property-of-a-data-source.aspx
    /// </summary>
    public class OrbitTypeDescriptor : CustomTypeDescriptor
    {
        public OrbitTypeDescriptor(ICustomTypeDescriptor parent)
            : base(parent)
        { }

        public override PropertyDescriptorCollection GetProperties()
        {
            PropertyDescriptorCollection cols = base.GetProperties();
            PropertyDescriptor addressPD = cols["Orbit"];
            PropertyDescriptorCollection Orbit_child = addressPD.GetChildProperties();
            PropertyDescriptor[] array = new PropertyDescriptor[cols.Count + 5];

            cols.CopyTo(array, 0);
            array[cols.Count] = new SubPropertyDescriptor(addressPD, Orbit_child["Mass"], "Orbit_Mass");
            array[cols.Count + 1] = new SubPropertyDescriptor(addressPD, Orbit_child["MassRelativeToEarth"], "Orbit_MassRelativeToEarth");
            array[cols.Count + 2] = new SubPropertyDescriptor(addressPD, Orbit_child["MassRelativeToSol"], "Orbit_MassRelativeToSol");
            array[cols.Count + 3] = new SubPropertyDescriptor(addressPD, Orbit_child["SemiMajorAxis"], "Orbit_SemiMajorAxis");
            array[cols.Count + 4] = new SubPropertyDescriptor(addressPD, Orbit_child["OrbitalPeriod"], "Orbit_OrbitalPeriod");

            PropertyDescriptorCollection newcols = new PropertyDescriptorCollection(array);
            return newcols;
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection cols = base.GetProperties(attributes);
            PropertyDescriptor addressPD = cols["Orbit"];
            PropertyDescriptorCollection Orbit_child = addressPD.GetChildProperties();
            PropertyDescriptor[] array = new PropertyDescriptor[cols.Count + 5];

            cols.CopyTo(array, 0);
            array[cols.Count] = new SubPropertyDescriptor(addressPD, Orbit_child["Mass"], "Orbit_Mass");
            array[cols.Count + 1] = new SubPropertyDescriptor(addressPD, Orbit_child["MassRelativeToEarth"], "Orbit_MassRelativeToEarth");
            array[cols.Count + 2] = new SubPropertyDescriptor(addressPD, Orbit_child["MassRelativeToSol"], "Orbit_MassRelativeToSol");
            array[cols.Count + 3] = new SubPropertyDescriptor(addressPD, Orbit_child["SemiMajorAxis"], "Orbit_SemiMajorAxis");
            array[cols.Count + 4] = new SubPropertyDescriptor(addressPD, Orbit_child["OrbitalPeriod"], "Orbit_OrbitalPeriod");

            PropertyDescriptorCollection newcols = new PropertyDescriptorCollection(array);
            return newcols;
        }
    }

    #endregion
}
