export class MessageRequest {
    constructor(public RoomId: string, public UserName: string, public Message: string, public AsAnonymous: boolean, public messageId?: string) { }

    static createWithId(roomId: string, userName: string, message: string, asAnonymous: boolean, messageId: string): MessageRequest {
        return new MessageRequest(roomId, userName, message, asAnonymous, messageId);
    }

    static createWithoutId(roomId: string, userName: string, message: string, asAnonymous: boolean): MessageRequest {
        return new MessageRequest(roomId, userName, message, asAnonymous);
    }
}