export class MessageRequest {
    constructor(public roomId: string, public message: string, public asAnonymous: boolean, public iv: string, public messageId?: string) { }

    static createWithId(roomId: string, message: string, asAnonymous: boolean, messageId: string, iv: string): MessageRequest {
        return new MessageRequest(roomId, message, asAnonymous, messageId, iv);
    }

    static createWithoutId(roomId: string, message: string, asAnonymous: boolean, iv: string): MessageRequest {
        return new MessageRequest(roomId, message, asAnonymous, iv);
    }
}