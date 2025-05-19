export type AuthResponseSuccess<T> = T extends string ?
{
    isSuccess: true;
    message: T;
} : {
    isSuccess: true;
    message: T
}
  
export interface AuthResponseFailure {
    isSuccess: false;
    message?: string;
}

export interface UserKeys {
    publicKey: string;
    privateKey: string;
}

export interface UserCredentials {
    Id: string;
    Email: string;
}
  
export type AuthResponse<T> = AuthResponseSuccess<T> | AuthResponseFailure; 