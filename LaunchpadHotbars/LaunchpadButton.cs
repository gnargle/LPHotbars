using System.Runtime.Serialization;

namespace LaunchpadHotbars
{
    public class LaunchpadButton
    {
        public int XCoord { get; set; }
        public int YCoord { get; set; }
        public int? Hotbar { get; set; }
        public uint? Slot { get; set; }

        [IgnoreDataMember]
        public bool OnCooldown { get; set; } = false;
        [IgnoreDataMember]
        public int LastColour { get; set; } = -1;


        public override string ToString()
        {
            return $"lpbutton info: x:{XCoord}, y:{YCoord}, hotbar: {Hotbar}, slot: {Slot}";
        }
    }
}
