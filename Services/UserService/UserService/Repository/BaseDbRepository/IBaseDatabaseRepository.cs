namespace UserService.Repository.BaseDbRepository;

public interface IBaseDatabaseRepository
{
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
}