export interface UserResponseSuccess<T> {
    isSuccess: true;
    data: T;
}
  
export interface UserResponseFailure {
    isSuccess: false;
    message?: string;
}

export interface UserKeys {
    publicKey: string;
    privateKey: string;
}

export interface GetUserCredentials {
    userName: string;
    email: string;
    twoFactorEnabled: boolean;
}

export interface UserPrivateKeyAndIv {
    privateKey: string;
    iv: string;
}

export interface UserEncryptedPrivateKeyAndIv {
    encryptedPrivateKey: string;
    iv: string;
}

export interface ShowFriendRequestData {
    connectionId: string;
    senderUserName: string;
    senderId: string;
    time: Date;
    receiverUserName: string;
    receiverId: string;
}
  
export type UserResponse<T> = UserResponseSuccess<T> | UserResponseFailure;