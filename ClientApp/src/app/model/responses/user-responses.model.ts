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
  
export type UserResponse<T> = UserResponseSuccess<T> | UserResponseFailure;