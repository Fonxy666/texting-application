export interface LoginAuthTokenRequest {
    userName: string;
    rememberMe: boolean;
    token: string;
}

export interface LoginRequest {
    userName: string;
    password: string;
    rememberMe: boolean
}

export interface EmailRequest {
    email: string;
}

export interface LoginRoomRequest {
    roomName: string;
    password: string;
}

export interface RegistrationRequest {
    email: string;
    userName: string;
    password: string;
    image: string;
    phoneNumber: string;
    publicKey: string;
    encryptedPrivateKey: string;
    iv: string;
}

export interface EmailAndUserNameRequest {
    email: string;
    userName: string;
}

export interface TokenValidatorRequest {
    email: string;
    verifyCode: String;
}
