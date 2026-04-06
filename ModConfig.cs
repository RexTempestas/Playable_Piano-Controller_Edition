using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playable_Piano
{
    public sealed class ModConfig
    {
        public Dictionary<string, string> InstrumentData { get; set; }

        public ModConfig() 
        {
            InstrumentData = new Dictionary<string, string>();
            InstrumentData.Add("Dark Piano", "Mushroomy.PlayablePiano_Piano");
            InstrumentData.Add("UprightPiano", "Mushroomy.PlayablePiano_Piano");
        }
    }
}
