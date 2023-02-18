using System.Text;

namespace SeMoreEvents.Components
{
    public interface IEventControllerBooleanEvent
    {
        void UpdateDetailedInfo(StringBuilder info, int slot, long entityId, bool value);
    }
}