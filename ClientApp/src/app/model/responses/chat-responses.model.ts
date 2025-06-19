export interface ReceiveMessageResponse {
    user: string,
    text: string,
    sendTime: string,
    senderId?: string,
    messageId?: string,
    seenList?: string[],
    roomId: string,
    iv?: string
}

export interface RoomIdAndRoomNameResponse {
    roomId: string,
    roomName: string
}

export interface SymmetricKeyResponse {
    encryptedRoomKey: string,
    roomId: string,
    roomName: string
}
