namespace UserService.Model.Responses.User;

public record DeleteUserResponse(string Username, string Message, bool Successful);