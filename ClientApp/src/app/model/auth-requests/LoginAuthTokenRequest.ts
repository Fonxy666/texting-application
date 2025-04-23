export class LoginAuthTokenRequest {
    constructor(public userName: string, public rememberMe: boolean, public token: String) { }
}