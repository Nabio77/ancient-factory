using System;
using UnityEngine;
using Sirenix.OdinInspector;
using AncientFactory.Core.Data;
using AncientFactory.Core.Types;

namespace AncientFactory.Core.Systems
{
    public class NotificationSystem : MonoBehaviour
    {
        public static NotificationSystem Instance { get; private set; }

        public event Action<NotificationData> OnNotificationAdded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        [Button("Test Info Notification")]
        public void TestInfo(string message = "This is a test notification")
        {
            ShowNotification(new NotificationData("Information", message, NotificationType.Info));
        }

        [Button("Test Warning Notification")]
        public void TestWarning(string message = "Something might be wrong")
        {
            ShowNotification(new NotificationData("Warning", message, NotificationType.Warning));
        }

        [Button("Test Error Notification")]
        public void TestError(string message = "Critical failure detected!")
        {
            ShowNotification(new NotificationData("Error", message, NotificationType.Error));
        }

        public void ShowNotification(NotificationData data)
        {
            OnNotificationAdded?.Invoke(data);
        }

        public void ShowNotification(string title, string message, NotificationType type, float duration = 5f)
        {
            ShowNotification(new NotificationData(title, message, type, duration));
        }
    }
}
