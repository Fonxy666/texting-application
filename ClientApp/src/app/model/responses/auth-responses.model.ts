export interface AuthResponseSuccess<T> {
    isSuccess: true;
    data: T;
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