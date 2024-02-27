using Shared.Entities;

namespace TopDownShooter.ApiService.Services;

public class SessionHandler {
    
    private Dictionary<string, SessionData> _sessions = new();
    
    public void Add(string groupId, SessionData sessionData) {
        _sessions.Add(groupId, sessionData);
    }
    
    public bool TryAdd(string groupId, SessionData sessionData) {
        return _sessions.TryAdd(groupId, sessionData);
    }
    
    public void Remove(string groupId) {
        _sessions.Remove(groupId);
    }
    
    public SessionData Get(string groupId) {
        return _sessions[groupId];
    }
    
    public KeyValuePair<string,SessionData> First(Func<KeyValuePair<string, SessionData>,bool> action) {
        return _sessions.First(action);
    }
    
    public List<SessionData> GetAll() {
        return _sessions.Values.ToList();
    }

    public List<SessionData> GetAllRunning() {
        return _sessions.Values.Where(s => s.GameStarted).ToList();
    }
    
    public int Count() {
        return _sessions.Count;
    }
}