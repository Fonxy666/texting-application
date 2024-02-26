export class LoginAuthTokenRequest {
    constructor(public UserName: string, public Password: string, public RememberMe: boolean, public Token: String) { }
}