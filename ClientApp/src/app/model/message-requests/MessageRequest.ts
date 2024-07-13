export class MessageRequest {
    constructor(public roomId: string, public userId: string, public message: string, public asAnonymous: boolean, public iv: string, public messageId?: string) { }

    static createWithId(roomId: string, userId: string, message: string, asAnonymous: boolean, messageId: string, iv: string): MessageRequest {
        return new MessageRequest(roomId, userId, message, asAnonymous, messageId, iv);
    }

    static createWithoutId(roomId: string, userId: string, message: string, asAnonymous: boolean, iv: string): MessageRequest {
        return new MessageRequest(roomId, userId, message, asAnonymous, iv);
    }
}