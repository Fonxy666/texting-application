using Textinger.Shared.Responses;
using UserService.Database;

namespace UserService.Repository.BaseDbRepository;

public class BaseDatabaseRepository(ILogger<BaseDatabaseRepository> logger, MainDatabaseContext context) : IBaseDatabaseRepository
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