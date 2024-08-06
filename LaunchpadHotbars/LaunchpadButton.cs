using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaunchpadHotbars
{
    [Serializable]
    public class LaunchpadButton
    {
        public int XCoord { get; set; }
        public int YCoord { get; set; }
        public int CCVal { get; set; }
        public int? Hotbar { get; set; }
        public uint? Slot { get; set; }
    }
}
