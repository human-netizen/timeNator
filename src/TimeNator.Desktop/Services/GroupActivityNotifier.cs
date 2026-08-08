namespace TimeNator.Desktop.Services;

/// <summary>Turns other members' live activity into notifications: someone started studying, or wrote in chat.</summary>
public class GroupActivityNotifier
{
    public GroupActivityNotifier(IStudyHubClient hub, INotificationService notifications, IAuthService auth)
    {
        hub.MemberStarted += (_, presence) =>
        {
            if (presence.UserId != auth.UserId)
                notifications.Show(NotificationKind.Groups, $"{presence.DisplayName} started studying",
                    presence.SubjectName);
        };
        hub.MessageReceived += (_, message) =>
        {
            if (message.UserId != auth.UserId)
                notifications.Show(NotificationKind.Groups, message.DisplayName,
                    message.Body.Length > 120 ? message.Body[..117] + "..." : message.Body);
        };
    }
}
