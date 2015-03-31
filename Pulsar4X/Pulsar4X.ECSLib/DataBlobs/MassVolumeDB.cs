using Pulsar4X.ECSLib.Factories;
using Pulsar4X.ECSLib.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs
{
    class MassVolumeDB : BaseDataBlob
    {
        /// <summary>
        /// Mass in KG of this entity.
        /// </summary>
        public double Mass { get; set; }

        /// <summary>
        /// The density of the body in g/cm^3
        /// </summary> 
        public double Density { get; set; }

        /// <summary>
        /// The Average Radius (in AU)
        /// </summary>
        public double Radius { get; set; }

        /// <summary>
        /// The Average Radius (in km)
        /// </summary>
        public double RadiusinKM
        {
            get { return Distance.ToKm(Radius); }
            set { Radius = Distance.ToAU(value); }
        }

        /// <summary>
        /// Measure on the gravity of a planet at its surface.
        /// In Earth Gravities (Gs).
        /// </summary>
        public float SurfaceGravity { get; set; }

        public MassVolumeDB(double mass, double density)
        {
            Mass = mass;
            Density = density;
            Radius = SystemBodyFactory.CalculateRadiusOfBody(mass, density);
            double radiusSquaredInM = (Radius * GameSettings.Units.MetersPerAu) * (Radius * GameSettings.Units.MetersPerAu); // conver to m from au.
            SurfaceGravity = (float)((GameSettings.Science.GravitationalConstant * Mass) / radiusSquaredInM); // see: http://nova.stanford.edu/projects/mod-x/ad-surfgrav.html
        }
    }
}
