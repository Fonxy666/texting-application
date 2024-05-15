export class ResetPasswordRequest {
    constructor(public Email: string, public ResetCode: string, public NewPassword: string) { }
}