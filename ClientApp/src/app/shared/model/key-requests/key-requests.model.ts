export interface RoomKeyRequest {
    roomId: string;
    connectionId: string;
    roomName: string;
}

export interface GetSymmetricKeyRequest {
    encryptedRoomKey: string;
    connectionId: string;
    roomId: string;
    roomName: string;
}
