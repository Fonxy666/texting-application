export class ChangeMessageSeenRequest {
    constructor(public userId: string, public asAnonym: boolean, public messageId: string) { }
}