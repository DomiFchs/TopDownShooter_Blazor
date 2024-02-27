using Domain.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Model.Configurations;
using Model.Entities;

namespace Domain.Repositories.Implementations;

public class HighScoreRepository(ShooterDbContext context) : ARepository<HighScore>(context), IHighScoreRepository {
    public async Task TryCreateHighScoreAsync(string playerName, CancellationToken ct) {
        var highScore = await Table.AsNoTracking().FirstOrDefaultAsync(x => x.PlayerName == playerName, ct);
        if (highScore == null) {
            await Table.AddAsync(new HighScore { PlayerName = playerName, Score = 1 }, ct);
            await Context.SaveChangesAsync(ct);
        } else{
            highScore.Score++;
            await UpdateAsync(highScore, ct);
        }
    }
}