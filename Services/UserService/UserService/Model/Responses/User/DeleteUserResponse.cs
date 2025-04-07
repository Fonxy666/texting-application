namespace UserService.Model.Responses.User;

public record DeleteUserResponse(string Username, string Message, bool Successful);


/*
 * 
 * Will make this refactor later on
 * 
public abstract record UserResponse(bool Success);

public record DeleteUserSuccess(string Username, string Message) : UserResponse(true);

public record DeleteUserFailure() : UserResponse(false);

public record UserIdLoginSuccess(string UserId, string UserName) : UserResponse(true);

public record UserIdLoginFailure() : UserResponse(false);
*/