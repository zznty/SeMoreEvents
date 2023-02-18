using System.Text;

namespace SeMoreEvents.Components
{
    public interface IEventControllerEvent
    {
        void UpdateDetailedInfo(StringBuilder info, int slot, long entityId, float value);
    }
}