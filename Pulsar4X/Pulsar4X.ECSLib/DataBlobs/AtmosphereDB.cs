using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public class AtmosphereDB : BaseDataBlob
    {
        /// <summary>
        /// Atmospheric Presure
        /// In Earth Atmospheres (atm).
        /// </summary>
        public float Pressure;

        /// <summary>
        /// Weather or not the planet has abundent water.
        /// </summary>
        public bool Hydrosphere;

        /// <summary>
        /// The percentage of the bodies sureface covered by water.
        /// </summary>
        public short HydrosphereExtent;

        /// <summary>
        /// A measure of the greenhouse factor provided by this Atmosphere.
        /// </summary>
        public float GreenhouseFactor;

        /// <summary>
        /// Pressure (in atm) of greenhouse gasses. but not really.
        /// to get this figure for a given gass toy would take its pressure 
        /// in the atmosphere and times it by the gasses GreenhouseEffect 
        /// which is a number between 1 and -1 normally.
        /// </summary>
        public float GreenhousePressure;

        /// <summary>
        /// How much light the body reflects. Affects temp.
        /// a number from 0 to 1.
        /// </summary>
        public float Albedo;

        /// <summary>
        /// Temperature of the planet AFTER greenhouse effects are taken into considuration. 
        /// This is a factor of the base temp and Green House effects.
        /// In Degrees C.
        /// </summary>
        public float SurfaceTemperature;

        /// <summary>
        /// The composition of the atmosphere, i.e. what gases make it up and in what ammounts.
        /// In Earth Atmospheres (atm).
        /// </summary>
        public Dictionary<AtmosphericGas, float> Composition;

    }
}
