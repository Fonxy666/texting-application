export class LoginAuthTokenRequest {
    constructor(public userName: string, public password: string, public rememberMe: boolean, public token: String) { }
}