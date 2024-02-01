export class ChangeEmailRequest {
    constructor(public oldEmail: string, public newEmail: string, public token: string) { }
}