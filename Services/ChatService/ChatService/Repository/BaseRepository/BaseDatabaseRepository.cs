using ChatService.Database;
using Textinger.Shared.Responses;

namespace ChatService.Repository.BaseRepository;

public class BaseDatabaseRepository(ILogger<BaseDatabaseRepository> logger, ChatContext context) : IBaseDatabaseRepository
{
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var result = await operation();

            if (result is not Failure && result is not FailureWithMessage)
            {
                await transaction.CommitAsync();
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during transaction");
            return (TResult)(object)new Failure();
        }
    }
}