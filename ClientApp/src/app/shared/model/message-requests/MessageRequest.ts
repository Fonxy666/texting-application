export class MessageRequest {
    constructor(public roomId: string, public text: string, public asAnonymous: boolean, public iv: string, public messageId?: string) { }

    static createWithId(roomId: string, text: string, asAnonymous: boolean, messageId: string, iv: string): MessageRequest {
        return new MessageRequest(roomId, text, asAnonymous, messageId, iv);
    }

    static createWithoutId(roomId: string, text: string, asAnonymous: boolean, iv: string): MessageRequest {
        return new MessageRequest(roomId, text, asAnonymous, iv);
    }
}

export interface ChangeMessageSeenHtttpRequest {
    messageId: string
}

export interface ChangeMessageSeenWebSocketRequest {
    userId: string
}
