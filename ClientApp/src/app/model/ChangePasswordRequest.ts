export class ChangePasswordRequest {
    constructor(public id: string, public oldpassword: string, public newpassword: string) { }
}