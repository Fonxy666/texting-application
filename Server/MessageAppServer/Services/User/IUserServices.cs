namespace Server.Services.User;

public interface IUserServices
{
    string SaveImageLocally(string userNameFileName, string base64Image);
}