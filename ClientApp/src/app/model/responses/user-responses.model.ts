export interface UserResponseSuccess<T> {
    isSuccess: true;
    data: T;
}
  
export interface UserResponseFailure {
    isSuccess: false;
    error?: {
        message?: string
    }
}

export interface UserKeys {
    publicKey: string;
    privateKey: string;
}

export interface UserName {
    userName: string;
}

export interface GetUserCredentials {
    userName: string;
    email: string;
    twoFactorEnabled: boolean;
}

export interface UserPrivateKeyAndIv {
    encryptedPrivateKey: string;
    iv: string;
}

export interface UserEncryptedPrivateKeyAndIv {
    encryptedPrivateKey: string;
    iv: string;
}

export interface ShowFriendRequestData {
    requestId: string;
    senderName: string;
    senderId: string;
    sentTime: Date;
    receiverName: string;
    receiverId: string;
}
  
export type UserResponse<T> = UserResponseSuccess<T> | UserResponseFailure;