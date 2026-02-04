using System;
using CarbonWorld.Core.Types;

namespace CarbonWorld.Core.Data
{
    [Serializable]
    public struct NotificationData
    {
        public string Title;
        public string Message;
        public NotificationType Type;
        public float Duration;

        public NotificationData(string title, string message, NotificationType type = NotificationType.Info, float duration = 5f)
        {
            Title = title;
            Message = message;
            Type = type;
            Duration = duration;
        }
    }
}
