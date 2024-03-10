export class MessageRequest {
    constructor(public RoomId: string, public UserName: string, public Message: string, public AsAnonymous: boolean) { }
}