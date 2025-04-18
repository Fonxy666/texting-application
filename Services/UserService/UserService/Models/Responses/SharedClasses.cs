namespace UserService.Models.Responses;

public abstract record ResponseBase(bool IsSuccess);

public abstract record FailedResponseBase<T>(bool IsSuccess, T? Data = default) : ResponseBase(IsSuccess);

public record FailedResponse() : UserResponse<string>(false);

public record FailedResponseWithMessage(string Message) : FailedResponseBase<string>(false, Message);