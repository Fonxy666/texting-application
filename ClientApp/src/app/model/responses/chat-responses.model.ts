export interface ReceiveMessageResponse {
    user: string,
    message: string,
    messageTime: string,
    userId?: string,
    messageId?: string,
    seenList?: string[],
    roomId: string,
    iv?: string
}