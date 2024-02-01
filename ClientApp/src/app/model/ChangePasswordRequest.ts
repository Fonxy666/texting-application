export class ChangePasswordRequest {
    constructor(public email: string, public oldpassword: string, public newpassword: string) { }
}