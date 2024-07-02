export class MessageRequest {
    constructor(public RoomId: string, public UserId: string, public Message: string, public AsAnonymous: boolean, public messageId?: string) { }

    static createWithId(roomId: string, userId: string, message: string, asAnonymous: boolean, messageId: string): MessageRequest {
        return new MessageRequest(roomId, userId, message, asAnonymous, messageId);
    }

    static createWithoutId(roomId: string, userId: string, message: string, asAnonymous: boolean): MessageRequest {
        return new MessageRequest(roomId, userId, message, asAnonymous);
    }
}