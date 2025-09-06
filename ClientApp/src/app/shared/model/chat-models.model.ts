export interface ConnectedUser {
    userId: string, 
    userName: string
}

export interface ChatRoomInvite {
    senderId: string, 
    roomId: string, 
    roomName: string, 
    senderName: string, 
    roomKey?: string
}
