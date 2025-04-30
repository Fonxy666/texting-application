namespace UserService.Models.Responses;

public abstract record ResponseBase(bool IsSuccess);

public record SuccessResponse<T>(T? Data = default) : ResponseBase(true);

public record Success() : ResponseBase(true);
public record Failure() : ResponseBase(false);
public record SuccessWithDto<T>(T? Data) : SuccessResponse<T?>(Data);
public record SuccessWithMessage(string Message) : ResponseBase(true);
public record FailureWithMessage(string Message) : ResponseBase(false);
