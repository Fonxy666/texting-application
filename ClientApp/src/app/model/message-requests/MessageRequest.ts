export class MessageRequest {
    constructor(public roomId: string, public message: string, public asAnonymous: boolean, public iv: string, public type: string, public messageId?: string) { }

    static createWithId(roomId: string, message: string, asAnonymous: boolean, messageId: string, iv: string, type: string): MessageRequest {
        return new MessageRequest(roomId, message, asAnonymous, messageId, iv, type);
    }

    static createWithoutId(roomId: string, message: string, asAnonymous: boolean, iv: string, type: string): MessageRequest {
        return new MessageRequest(roomId, message, asAnonymous, iv, type);
    }
}