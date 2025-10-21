using System.Collections.Generic;

public interface ISessionManager
{
    void StartSession(string qrCodeValue);
    void ConfirmSession();
    void SessionStarted(bool success, Dictionary<string, List<ServerRequests.KeyValue>> gameStatePairs = null);
    void SessionConfirmed();
    void EndSession();
    void SessionEnded();
    void RestartSession(List<ServerRequests.KeyValue> gameState);
    void NoExistingSession();
}
