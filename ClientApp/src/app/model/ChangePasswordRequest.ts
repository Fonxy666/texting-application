export class ChangePasswordRequest {
    constructor(public id: string, public oldPassword: string, public password: string) { }
}