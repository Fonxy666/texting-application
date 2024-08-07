export class ImageRequest {
    constructor(public roomId: string, public message: string, public asAnonymous: boolean, public iv: string, public messageId?: string) { }
}