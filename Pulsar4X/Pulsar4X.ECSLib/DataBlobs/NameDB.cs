using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public class NameDB : BaseDataBlob
    {
        public string Name { get; set; }

        public NameDB(string name)
        {
            Name = name;
        }
    }
}
