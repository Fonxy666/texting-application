export class ChangePasswordRequest {
    constructor(public id: string, public oldpassword: string, public password: string, public passwordrepeat: string) { }
}