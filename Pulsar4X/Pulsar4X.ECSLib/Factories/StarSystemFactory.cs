using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.ECSLib.DataBlobs;

namespace Pulsar4X.ECSLib.Factories
{
    /// <summary>
    /// Stargen containes functions for generating new star systems based on the information contained in GalaxyGen. 
    /// </summary>
    /// <remarks>
    /// Stargen works by the following process:
    /// <list type="number">
    /// <item>
    /// First the Stars are generated, starting with their spectral type and then using that to generate sane vaslues for all other star data.
    /// </item>
    /// <item>
    /// Once a star is generated Planets, Asteriods, Dwarf planets and Moons (aka System Bodies) are generated for the star. 
    /// This process is quite a lot more complicated but can best be describe as a three stage process:
    ///     <list type="number">
    ///     <item>
    ///     The first stage defines a number "Protoplanets" consiting the mass and planet type. amoungst these "Protoplanets" are 
    ///     single Asteroids which will act as references for later generation of entire Asteroid belts, including Dwarf Planets. 
    ///     The list of protoplanets is sorted to make sure Terrestrial planets are mostly at the top of the list.
    ///     Note that moons are not generated at this stage.
    ///     </item>
    ///     <item>
    ///     The second stage involves generating orbits for the planets. This is done in much the same was as for Stars, 
    ///     by making sure that planets are at least 10x more gravationaly bound to the parent star then they are to their 
    ///     nearest neighbours we ensure some "sane" seperation between planets.
    ///     </item>
    ///     The third and final pass involes fleshing out the remain properties of the planets. This includes Densite, Radius, 
    ///     Temerature, Atmosphere, Ruins, Minerials, etc. For each reference asteroid an aproprate number of dwarf planets and asteroids
    ///     Are generated (note that they only go though this last stage with mass and orbits generated based on the reference asteroid provided).
    ///     Planets also have their moons generated at this stage. It is worth noting that moons go through the same 3 stage process, it is just nested
    ///     inside the 3rd stage for planets.
    ///     </list>
    /// Generation of system bodies repsent the meat of Star Generation.
    /// </item>
    /// <item>
    /// Comets are generated in their own Single stage process. it is almost identical to the process Asteriod have, except they do not have a reference orbit
    /// (A lot of code is reused for all System Body generation, including comets). 
    /// </item>
    /// <item>
    /// Orbits are generated for the stars. This is done is such a way as to ensure that the stars are more gravitationaly bound to the 
    /// Parent (largest) star in the system then to any of the child stars.
    /// </item>
    /// <item>
    /// Jump points are generated for each star in the system. Unlike Aurora where only the first star in a system had jump points in 
    /// Pulsar every star has its own, even in multi star systems. 
    /// </item>
    /// <item>
    /// Jump points are generated for each star in the system. Unlike Aurora where only the first star in a system had jump points in 
    /// Pulsar every star has its own, even in multi star systems. 
    /// </item>
    /// <item>
    /// Finally NPRs are generated. Note that this has not yet been implemented as NPR Factions are not yet supported.
    /// </item>
    /// </list>
    /// In addition it contains a function which will return a hard coded veriosn of our own Solar System as well as several related convience functions.
    /// </remarks>
    public static class StarSystemFactory
    {
        /// <summary>
        /// A small struct to hold a system body type and mass before we have generated its orbit.
        /// </summary>
        private struct ProtoSystemBody
        {
            public double _mass;
            public BodyType _type;
        }

        private struct ProtoStar
        {
            ProtoSystemBody body;
        }

        /// <summary>
        /// Creates a single Star system with the provided name. It generates a new seed for the system.
        /// </summary>
        public static StarSystem CreateSystem(string name)
        {
            return CreateSystem(name, -1);
        }

        /// <summary>
        /// Creates a single Star System using the random seed provided. 
        /// If given the same seed twice it should generate 2 identical systems even on different PCs.
        /// </summary>
        public static StarSystem CreateSystem(string name, int seed, int numJumpPoints = -1)
        {
            // create new RNG with Seed.
            if (seed == -1)
            {
                seed = GalaxyFactory.SeedRNG.Next();
            }

            Random RNG = new Random(seed);
            StarSystem newSystem = new StarSystem(name, seed);

            int numStars = RNG.Next(1, 5);
            List<int> stars = StarFactory.CreateStarsForSystem(RNG, newSystem, numStars);
            List<List<int>> systemBodies = new List<List<int>>();

            foreach (int star in stars)
            {
                List<int> starBodies = SystemBodyFactory.GenerateSystemBodiesForStar(RNG, newSystem, star);
            }

            StarFactory.GenerateStarOrbits(RNG, newSystem, stars); 

            JumpPointFactory.GenerateJumpPoints(newSystem, numJumpPoints);

            Game.Instance.StarSystems.Add(newSystem);
            Game.Instance.StarSystemCurrentIndex++;
            return newSystem;
        }

        #region Create Sol
        /// <summary>
        /// Creates our own solar system.
        /// </summary>
        public static StarSystem CreateSol()
        {
            StarSystem Sol = new StarSystem("Sol", GalaxyFactory.SeedRNG.Next());

            // Used for JumpPoint generation.
            m_RNG = new Random(Sol.Seed);

            Star Sun = new Star("Sol", Constants.Units.SolarRadiusInAu, 5505, 1, SpectralType.G, Sol);
            Sun.Age = 4.6E9;
            Sun.Orbit = Orbit.FromStationary(Constants.Units.SolarMassInKG);
            Sun.Class = "G2";
            SetHabitableZone(Sun);
            Sol.Stars.Add(Sun);

            SystemBody Mercury = new SystemBody(Sun, SystemBody.PlanetType.Terrestrial);
            Mercury.Name = "Mercury";
            Mercury.Orbit = Orbit.FromMajorPlanetFormat(3.3022E23, Sun.Orbit.Mass, 0.387098, 0.205630, 0, 48.33167, 29.124, 252.25084, GalaxyFactory.J2000);
            Mercury.Radius = Distance.ToAU(2439.7);
            double x, y;
            Mercury.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Mercury.Position.System = Sol;
            Mercury.Position.X = x;
            Mercury.Position.Y = y;
            Sun.Planets.Add(Mercury);

            SystemBody Venus = new SystemBody(Sun, SystemBody.PlanetType.Terrestrial);
            Venus.Name = "Venus";
            Venus.Orbit = Orbit.FromMajorPlanetFormat(4.8676E24, Sun.Orbit.Mass, 0.72333199, 0.00677323, 0, 76.68069, 131.53298, 181.97973, GalaxyFactory.J2000);
            Venus.Radius = Distance.ToAU(6051.8);
            Venus.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Venus.Position.System = Sol;
            Venus.Position.X = x;
            Venus.Position.Y = y;
            Sun.Planets.Add(Venus);

            SystemBody Earth = new SystemBody(Sun, SystemBody.PlanetType.Terrestrial);
            Earth.Name = "Earth";
            Earth.Orbit = Orbit.FromMajorPlanetFormat(5.9726E24, Sun.Orbit.Mass, 1.00000011, 0.01671022, 0, -11.26064, 102.94719, 100.46435, GalaxyFactory.J2000);
            Earth.Radius = Distance.ToAU(6378.1);
            Earth.BaseTemperature = Temperature.ToCelsius(279.3f);  //(float)CalculateBaseTemperatureOfBody(Sun, Earth.Orbit.SemiMajorAxis);
            Earth.Tectonics = SystemBody.TectonicActivity.EarthLike;
            Earth.SurfaceGravity = 9.8f;
            Earth.Atmosphere = new Atmosphere(Earth);
            Earth.Atmosphere.Albedo = 0.306f;
            Earth.Atmosphere.SurfaceTemperature = Earth.BaseTemperature;
            AddGasToAtmoSafely(Earth.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(6), 0.78f);  // N
            AddGasToAtmoSafely(Earth.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(9), 0.21f);  // O
            AddGasToAtmoSafely(Earth.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(11), 0.01f);  // Ar
            Earth.Atmosphere.UpdateState();
            Earth.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Earth.Position.System = Sol;
            Earth.Position.X = x;
            Earth.Position.Y = y;
            Sun.Planets.Add(Earth);

            SystemBody Moon = new SystemBody(Earth, SystemBody.PlanetType.Moon);
            Moon.Name = "Moon";
            Moon.Orbit = Orbit.FromAsteroidFormat(0.073E24, Earth.Orbit.Mass, Distance.ToAU(384748), 0.0549006, 0, 0, 0, 0, GalaxyFactory.J2000);
            Moon.Radius = Distance.ToAU(1738.14);
            Moon.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Moon.Position.System = Sol;
            Moon.Position.X = Earth.Position.X + x;
            Moon.Position.Y = Earth.Position.Y + y;
            Earth.Moons.Add(Moon);

            SystemBody Mars = new SystemBody(Sun, SystemBody.PlanetType.Terrestrial);
            Mars.Name = "Mars";
            Mars.Orbit = Orbit.FromMajorPlanetFormat(0.64174E24, Sun.Orbit.Mass, 1.52366231, 0.09341233, 1.85061, 49.57854, 336.04084, 355.45332, GalaxyFactory.J2000);
            Mars.Radius = Distance.ToAU(3396.2);
            Mars.BaseTemperature = (float)CalculateBaseTemperatureOfBody(Sun, Mars.Orbit.SemiMajorAxis);// 210.1f + (float)Constants.Units.KELVIN_TO_DEGREES_C;
            Mars.Tectonics = SystemBody.TectonicActivity.Dead;
            Mars.SurfaceGravity = 3.71f;
            Mars.Atmosphere = new Atmosphere(Mars);
            Mars.Atmosphere.Albedo = 0.250f;
            Mars.Atmosphere.SurfaceTemperature = Mars.BaseTemperature;
            AddGasToAtmoSafely(Mars.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(12), 0.95f * 0.01f);  // C02% * Mars Atms
            AddGasToAtmoSafely(Mars.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(6), 0.027f * 0.01f);  // N% * Mars Atms
            AddGasToAtmoSafely(Mars.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(9), 0.007f * 0.01f);  // O% * Mars Atms
            AddGasToAtmoSafely(Mars.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(11), 0.016f * 0.01f);  // Ar% * Mars Atms
            Mars.Atmosphere.UpdateState();
            Mars.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Mars.Position.System = Sol;
            Mars.Position.X = x;
            Mars.Position.Y = y;
            Sun.Planets.Add(Mars);

            SystemBody Jupiter = new SystemBody(Sun, SystemBody.PlanetType.GasGiant);
            Jupiter.Name = "Jupiter";
            Jupiter.Orbit = Orbit.FromMajorPlanetFormat(1898.3E24, Sun.Orbit.Mass, 5.20336301, 0.04839266, 1.30530, 100.55615, 14.75385, 34.40438, GalaxyFactory.J2000);
            Jupiter.Radius = Distance.ToAU(71492);
            Jupiter.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Jupiter.Position.System = Sol;
            Jupiter.Position.X = x;
            Jupiter.Position.Y = y;
            Sun.Planets.Add(Jupiter);

            SystemBody Saturn = new SystemBody(Sun, SystemBody.PlanetType.GasGiant);
            Saturn.Name = "Saturn";
            Saturn.Orbit = Orbit.FromMajorPlanetFormat(568.36E24, Sun.Orbit.Mass, 9.53707032, 0.05415060, 2.48446, 113.71504, 92.43194, 49.94432, GalaxyFactory.J2000);
            Saturn.Radius = Distance.ToAU(60268);
            Saturn.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Saturn.Position.System = Sol;
            Saturn.Position.X = x;
            Saturn.Position.Y = y;
            Sun.Planets.Add(Saturn);

            SystemBody Uranus = new SystemBody(Sun, SystemBody.PlanetType.IceGiant);
            Uranus.Name = "Uranus";
            Uranus.Orbit = Orbit.FromMajorPlanetFormat(86.816E24, Sun.Orbit.Mass, 19.19126393, 0.04716771, 0.76986, 74.22988, 170.96424, 313.23218, GalaxyFactory.J2000);
            Uranus.Radius = Distance.ToAU(25559);
            Uranus.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Uranus.Position.System = Sol;
            Uranus.Position.X = x;
            Uranus.Position.Y = y;
            Sun.Planets.Add(Uranus);

            SystemBody Neptune = new SystemBody(Sun, SystemBody.PlanetType.IceGiant);
            Neptune.Name = "Neptune";
            Neptune.Orbit = Orbit.FromMajorPlanetFormat(102E24, Sun.Orbit.Mass, Distance.ToAU(4495.1E6), 0.011, 1.8, 131.72169, 44.97135, 304.88003, GalaxyFactory.J2000);
            Neptune.Radius = Distance.ToAU(24764);
            Neptune.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Neptune.Position.System = Sol;
            Neptune.Position.X = x;
            Neptune.Position.Y = y;
            Sun.Planets.Add(Neptune);

            SystemBody Pluto = new SystemBody(Sun, SystemBody.PlanetType.DwarfPlanet);
            Pluto.Name = "Pluto";
            Pluto.Orbit = Orbit.FromMajorPlanetFormat(0.0131E24, Sun.Orbit.Mass, Distance.ToAU(5906.38E6), 0.24880766, 17.14175, 110.30347, 224.06676, 238.92881, GalaxyFactory.J2000);
            Pluto.Radius = Distance.ToAU(1195);
            Pluto.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Pluto.Position.System = Sol;
            Pluto.Position.X = x;
            Pluto.Position.Y = y;
            Sun.Planets.Add(Pluto);

            GenerateJumpPoints(Sol);

            // Clean up cached RNG:
            m_RNG = null;

            GameState.Instance.StarSystems.Add(Sol);
            GameState.Instance.StarSystemCurrentIndex++;
            return Sol;
        }

        #endregion

        #region Util Functions

        #endregion
    }
}
