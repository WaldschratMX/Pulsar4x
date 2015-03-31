using Pulsar4X.ECSLib.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.Factories
{
    class JumpPointFactory
    {
        /// <summary>
        /// Generates a jump points in the designated system.
        /// Used by JumpPoint class when connecting an a existing system
        /// with no unconnected jump points.
        /// </summary>
        public static JumpPoint GenerateJumpPoint(StarSystem system)
        {
            m_RNG = new Random(GalaxyFactory.SeedRNG.Next()); // Is there a better way?

            Star luckyStar;

            /// <summary>
            /// Only the system primary will have jumppoints if this is true.
            /// </summary>
            if (Constants.GameSettings.PrimaryOnlyJumpPoints == true)
            {
                luckyStar = system.Stars[0];
            }
            else
            {
                do
                {
                    luckyStar = system.Stars[m_RNG.Next(system.Stars.Count + 1)];
                } while (luckyStar.Planets.Count != 0);
            }

            return GenerateJumpPoint(luckyStar);
        }

        /// <summary>
        /// Generates Jump Points for this system.
        /// If numJumpPoints is not specified, we will generate the "Natural" amount
        /// based on GetNaturalJumpPointGeneration(Star)
        /// </summary>
        /// <param name="system">System to generate JumpPoints in.</param>
        /// <param name="numJumpPoints">Specific number of jump points to create.</param>
        public static void GenerateJumpPoints(StarSystem system, int numJumpPoints = -1)
        {
            WeightedList<Star> starList = new WeightedList<Star>();

            /// <summary>
            /// Only the system primary will have jumppoints if this is true.
            /// </summary>
            int i = 0;
            do
            {
                Star currentStar = system.Stars[i];
                starList.Add(GetNaturalJumpPointGeneration(currentStar), currentStar);
                i++;
            }
            while (i < system.Stars.Count && !Constants.GameSettings.PrimaryOnlyJumpPoints);

            // If numJumpPoints wasn't specified by the systemGen,
            // then just make as many jumpPoints as our stars cumulatively want to make.
            if (numJumpPoints == -1)
                numJumpPoints = (int)starList.TotalWeight;

            numJumpPoints = (int)Math.Round(numJumpPoints * Constants.GameSettings.JumpPointConnectivity);

            if (Constants.GameSettings.SystemJumpPointHubChance > m_RNG.Next(100))
            {
                numJumpPoints = (int)Math.Round(numJumpPoints * Constants.GameSettings.JumpPointHubConnectivity);
            }

            int jumpPointsGenerated = 0;
            while (jumpPointsGenerated < numJumpPoints)
            {
                double rnd = m_RNG.NextDouble();

                // Generate a jump point on a star from the weighted list.
                GenerateJumpPoint(starList.Select(rnd));
                jumpPointsGenerated++;
            }

        }

        /// <summary>
        /// Returns the number of JumpPoints that this star wants to generate.
        /// Currently based exclusivly on the mass of the planets around the star.
        /// </summary>
        private static int GetNaturalJumpPointGeneration(Star star)
        {
            if (star.Planets.Count == 0 && star != star.Position.System.Stars[0])
            {
                return 0; // Don't generate JP's on non-primary planetless stars.
            }

            int numJumpPoints = 1; // Each star always generates a JP.

            // Give a chance per planet to generate a JumpPoint
            foreach (SystemBody currentPlanet in star.Planets)
            {
                if (currentPlanet.Type == SystemBody.PlanetType.Comet || currentPlanet.Type == SystemBody.PlanetType.Asteroid)
                {
                    // Don't gen JP's around comets or asteroids.
                    continue;
                }

                int chance = Constants.GameSettings.JumpPointGenerationChance;

                // Higher mass planets = higher chance.
                double planetEarthMass = currentPlanet.Orbit.Mass / Constants.Units.EarthMassInKG;
                if (planetEarthMass > 1)
                {
                    chance = chance + 2;
                    if (planetEarthMass > 3)
                    {
                        chance = chance + 3;
                    }
                    if (planetEarthMass > 5)
                    {
                        chance = chance + 5;
                    }
                }
                if (chance >= m_RNG.Next(101))
                {
                    numJumpPoints++;
                }
            }

            return numJumpPoints;
        }

        /// <summary>
        /// Generates a JumpPoint on the designated star.
        /// Clamps JumpPoint generation to be within the planetary
        /// field of the star.
        /// </summary>
        private static JumpPoint GenerateJumpPoint(Star star)
        {
            double minRadius = GalaxyFactory.OrbitalDistanceByStarSpectralType[star.SpectralType].Min;
            double maxRadius = GalaxyFactory.OrbitalDistanceByStarSpectralType[star.SpectralType].Max;

            // Clamp generation to within the planetary system.
            foreach (SystemBody currentPlanet in star.Planets)
            {
                if (currentPlanet.Type == SystemBody.PlanetType.Comet || currentPlanet.Type == SystemBody.PlanetType.Asteroid)
                {
                    // Don't gen JP's around comets or asteroids.
                    continue;
                }

                if (minRadius > currentPlanet.Orbit.Periapsis)
                {
                    minRadius = currentPlanet.Orbit.Periapsis;
                }
                if (maxRadius < currentPlanet.Orbit.Apoapsis)
                {
                    maxRadius = currentPlanet.Orbit.Apoapsis;
                }
            }

            // Determine a location for the new JP.
            // Location will be between minDistance and 75% of maxDistance.
            double offsetX = (maxRadius - minRadius) * RNG_NextDoubleRange(0.0d, 0.75d) + minRadius;
            double offsetY = (maxRadius - minRadius) * RNG_NextDoubleRange(0.0d, 0.75d) + minRadius;

            // Randomly flip the sign of the offsets.
            if (m_RNG.NextDouble() >= 0.5)
            {
                offsetX = -offsetX;
            }
            if (m_RNG.NextDouble() >= 0.5)
            {
                offsetY = -offsetY;
            }

            // Create the new jumpPoint and link it to it's parent system.
            JumpPoint newJumpPoint = new JumpPoint(star, offsetX, offsetY);

            return newJumpPoint;
        }
    }
}
