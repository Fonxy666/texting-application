export class ChangePasswordRequestForUser {
    constructor(public oldPassword: string, public password: string) { }
}