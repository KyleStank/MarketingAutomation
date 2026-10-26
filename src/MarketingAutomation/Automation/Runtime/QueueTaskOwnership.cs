using UnityEngine;

namespace MarketingAutomation.Automation.Runtime;

// A marker, not queue configuration: it can outlive Stop while paid work finishes.
public sealed class QueueTaskOwnership : MonoBehaviour
{
    internal MarketingQueue Owner;
}
