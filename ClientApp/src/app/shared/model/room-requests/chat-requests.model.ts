export interface StoreRoomSymmetricKeyRequest {
    encryptedKey: string;
    roomId: string;
}

export interface JoinRoomRequest {
    roomName: string,
    password: string
}

export interface CreateRoomRequest {
    roomName: string, 
    password: string, 
    encryptedSymmetricRoomKey: string
}

export interface ChangePasswordForRoomRequest {
    id: string, 
    oldPassword: string, 
    password: string
}

export interface GetMessagesRequest {
    roomId: string,
    index: number
}
