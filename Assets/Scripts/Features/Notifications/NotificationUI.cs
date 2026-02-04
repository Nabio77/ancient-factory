using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using CarbonWorld.Core.Systems;
using CarbonWorld.Core.Data;

namespace CarbonWorld.Features.Notifications
{
    public class NotificationUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField, Required]
        private UIDocument uiDocument;

        [Title("Settings")]
        [SerializeField]
        private VisualTreeAsset notificationTemplate; // Optional if we build via code, but pure code is often easier for dynamic lists if structure is simple.

        private VisualElement _queueContainer;
        private List<VisualElement> _activeNotifications = new List<VisualElement>();
        private bool _isSubscribed;

        private void OnEnable()
        {
            SubscribeToSystem();

            // Try to find the container if document is already loaded
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                _queueContainer = uiDocument.rootVisualElement.Q<VisualElement>("notification-queue");
            }
        }

        private void OnDisable()
        {
            if (_isSubscribed && NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.OnNotificationAdded -= OnNotificationAdded;
                _isSubscribed = false;
            }
        }

        private void Start()
        {
            // Ensure container reference in Start as well
            if (uiDocument != null)
            {
                _queueContainer = uiDocument.rootVisualElement.Q<VisualElement>("notification-queue");
            }

            // Retry subscription in Start if missed in OnEnable (e.g. initialization order)
            SubscribeToSystem();

            // Clear any editor-time previews
            _queueContainer?.Clear();
        }

        private void SubscribeToSystem()
        {
            if (!_isSubscribed && NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.OnNotificationAdded += OnNotificationAdded;
                _isSubscribed = true;
            }
        }

        private void OnNotificationAdded(NotificationData data)
        {
            if (_queueContainer == null) return;

            StartCoroutine(ShowNotificationRoutine(data));
        }

        private IEnumerator ShowNotificationRoutine(NotificationData data)
        {
            // Create Toast Element
            var toast = new VisualElement();
            toast.AddToClassList("notification-toast");
            toast.AddToClassList($"notification-type-{data.Type}"); // e.g., notification-type-Warning
            toast.AddToClassList("notification-toast--hidden"); // Start hidden for animation

            // Title
            var titleLabel = new Label(data.Title);
            titleLabel.AddToClassList("notification-title");
            toast.Add(titleLabel);

            // Message
            var msgLabel = new Label(data.Message);
            msgLabel.AddToClassList("notification-message");
            toast.Add(msgLabel);

            // Add to Queue (Add to end because column-reverse will put it at bottom)
            _queueContainer.Add(toast);

            // Wait a frame for layout
            yield return null;

            // Animate In
            toast.RemoveFromClassList("notification-toast--hidden");

            // Wait duration
            yield return new WaitForSeconds(data.Duration);

            // Animate Out
            toast.AddToClassList("notification-toast--hidden");

            // Wait for animation to finish (0.3s matches USS)
            yield return new WaitForSeconds(0.4f);

            // Remove
            if (_queueContainer.Contains(toast))
            {
                _queueContainer.Remove(toast);
            }
        }
    }
}
