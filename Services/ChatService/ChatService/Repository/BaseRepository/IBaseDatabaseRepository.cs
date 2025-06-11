namespace ChatService.Repository.BaseRepository;

public interface IBaseDatabaseRepository
{
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
}