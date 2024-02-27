using Model.Entities;

namespace Domain.Repositories.Interfaces;

public interface IHighScoreRepository : IRepository<HighScore> {
    Task TryCreateHighScoreAsync(string playerName, CancellationToken ct);
}