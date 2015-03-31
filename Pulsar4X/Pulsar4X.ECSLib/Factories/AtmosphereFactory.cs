using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.Factories
{
    public static class AtmosphereFactory
    {
        /// <summary>
        /// Generates an atmosphere for the provided planet based on its type.
        /// Atmosphere needs to gen:
        /// -- Albedo (affected by Hydrosphere how exactly?)  
        /// -- presure
        /// -- Hydrosphere
        /// -- Hydrosphere extent
        /// And the following are worked out by the Atmosphere:
        /// -- Greenhouse Factor
        /// -- surface Temp. (based on base temp, greehhouse factor and Albedo).
        /// </summary>
        private static Atmosphere GenerateAtmosphere(SystemBody planet)
        {
            Atmosphere atmo = new AtmosphereDB(planet);
            atmo.SurfaceTemperature = planet.BaseTemperature;       // we need something sane to star us off.

            // calc albedo:
            atmo.Albedo = (float)RNG_NextDoubleRange(GalaxyFactory.PlanetAlbedoByType[planet.Type]);

            // some safe defaults:
            atmo.HydrosphereExtent = 0;
            atmo.Hydrosphere = false;

            // we uses these to keep a running tally of how much gass we have generated.
            float totalATM = 0;
            float currATM = 0;
            int noOfTraceGases = 0;

            // Generate an Atmosphere
            ///< @todo Remove some of this hard coding:
            switch (planet.Type)
            {
                case BodyType.GasDwarf:
                case BodyType.GasGiant:
                    // Start with the ammount of heilum:
                    currATM = (float)RNG_NextDoubleRange(0.05, 0.3);
                    atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(1), currATM);
                    totalATM += currATM;

                    // next get a random number/ammount of trace gases:
                    noOfTraceGases = RNG.Next(1, 4);
                    totalATM += AddTraceGases(atmo, noOfTraceGases);

                    // now make the remaining amount Hydrogen:
                    currATM = 1 - totalATM; // get the remaining ATM.
                    AddGasToAtmoSafely(atmo, AtmosphericGas.AtmosphericGases.SelectAt(0), currATM);
                    break;

                case BodyType.IceGiant:
                    // Start with the ammount of heilum:
                    currATM = (float)RNG_NextDoubleRange(0.1, 0.25);
                    atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(1), currATM);
                    totalATM += currATM;

                    // next add a small amount of Methane:
                    currATM = (float)RNG_NextDoubleRange(0.01, 0.03);
                    atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(2), currATM);
                    totalATM += currATM;

                    // Next some water and ammonia:
                    currATM = (float)RNG_NextDoubleRange(0.0, 0.01);
                    atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(3), currATM);
                    totalATM += currATM;
                    currATM = (float)RNG_NextDoubleRange(0.0, 0.01);
                    atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(4), currATM);
                    totalATM += currATM;

                    // now make the remaining amount Hydrogen:
                    currATM = 1 - totalATM; // get the remaining ATM.
                    atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(0), currATM);
                    break;

                case BodyType.Moon:
                case BodyType.Terrestrial:
                    // Only Terrestrial like planets have a limited chance of having an atmo:
                    double atmoChance = GMath.Clamp01(GalaxyFactory.AtmosphereGenerationModifier[planet.Type] *
                                                        (planet.Orbit.Mass / GalaxyFactory.SystemBodyMassByType[planet.Type].Max));

                    if (RNG.NextDouble() < atmoChance)
                    {
                        // Terrestrial Planets can have very large ammount of ATM.
                        // so we will generate a number to act as the total:
                        double planetsATMChance = RNG.NextDouble();// (float)RNG_NextDoubleRange(0.1, 100);
                        // get my mass ratio relative to earth (so really small bodies cannot have massive atmos:
                        double massRatio = planet.Orbit.Mass / GameSettings.Units.EarthMassInKG;
                        float planetsATM = 1;

                        // Start with the ammount of Oxygen or Carbin Di-oxide or methane:
                        int atmoTypeChance = RNG.Next(0, 3);
                        if (atmoTypeChance == 0)            // methane
                        {
                            planetsATM = (float)GMath.Clamp((double)planetsATMChance * 5 * massRatio, 0.01, 5);
                            currATM = (float)RNG_NextDoubleRange(0.05, 0.40);
                            atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(2), currATM * planetsATM);
                            totalATM += currATM;
                        }
                        else if (atmoTypeChance == 1)   // Carbon Di-Oxide
                        {
                            planetsATM = (float)GMath.Clamp((double)planetsATMChance * 5 * massRatio, 0.01, 200); // allow presure cooker atmos!!
                            currATM = (float)RNG_NextDoubleRange(0.05, 0.90);
                            atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(12), currATM * planetsATM);
                            totalATM += currATM;
                        }
                        else                        // oxygen
                        {
                            planetsATM = (float)GMath.Clamp((double)planetsATMChance * 5 * massRatio, 0.01, 5);
                            currATM = (float)RNG_NextDoubleRange(0.05, 0.40);
                            atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(9), currATM * planetsATM);
                            totalATM += currATM;

                            // Gen Hydrosphere for these planets:
                            if (RNG.Next(0, 1) == 0)
                            {
                                atmo.Hydrosphere = true;
                                atmo.HydrosphereExtent = (short)Math.Round(RNG.NextDouble() * 100);
                            }
                        }

                        // next get a random number/ammount of trace gases:
                        noOfTraceGases = RNG.Next(1, 3);
                        totalATM += AddTraceGases(atmo, noOfTraceGases, planetsATM, false);

                        // now make the remaining amount Nitrogen:
                        currATM = 1 - totalATM; // get the remaining ATM.
                        AddGasToAtmoSafely(atmo, AtmosphericGas.AtmosphericGases.SelectAt(6), currATM * planetsATM);
                    }
                    break;

                // Everthing else has no atmosphere at all.
                case BodyType.IceMoon:
                case BodyType.DwarfPlanet:
                case BodyType.Asteroid:
                case BodyType.Comet:
                default:
                    break; // none
            }

            // now calc data resulting from above:
            atmo.UpdateState();

            return atmo;
        }

        /// <summary>
        /// Just adds the specified ammount of gas to the specified atmosphere safely.
        /// </summary>
        private static void AddGasToAtmoSafely(Atmosphere atmo, AtmosphericGas gas, float ammount)
        {
            if (atmo.Composition.ContainsKey(gas))
                atmo.Composition[gas] += ammount;
            else
                atmo.Composition.Add(gas, ammount);
        }

        /// <summary>
        /// A small helper function for GenerateAtmosphere. It generates up to the specified number of
        /// "trace" gases and adds them to the atmosphere.
        /// </summary>
        /// <param name="totalAtmoPressure">The ammount of gass added is multiplyed by this before being added to the Atmosphere.</param>
        /// <returns>The ammount of gas added in ATMs</returns>
        private static float AddTraceGases(Atmosphere atmo, int numberToAdd, float totalAtmoPressure = 1, bool allowHydrogenOrHelium = true)
        {
            float totalATMAdded = 0;
            int gassesAdded = 0;
            while (gassesAdded < numberToAdd)
            {
                //float currATM = (float)RNG_NextDoubleRange(0, 0.01);
                var gas = AtmosphericGas.AtmosphericGases.Select(RNG.NextDouble());
                if (allowHydrogenOrHelium == false)
                {
                    if (gas.ChemicalSymbol == "H" || gas.ChemicalSymbol == "He")
                        continue;
                }
                if (atmo.Composition.ContainsKey(gas))
                    continue;
                atmo.Composition.Add(gas, 0.01f * totalAtmoPressure);   // add 1% for trace gasses.
                totalATMAdded += 0.01f * totalAtmoPressure;
                gassesAdded++;
            }

            return totalATMAdded;
        }
    }
}
