export class ChangePasswordRequestForRoom {
    constructor(public id: string, public oldPassword: string, public password: string) { }
}