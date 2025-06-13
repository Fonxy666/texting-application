export interface ChangeAvatarRequest {
    userId: string;
    image: string;
}

export interface ChangeEmailRequest {
    oldEmail: string;
    newEmail: string;
}

export interface ChangeMessageRequest {
    id: string;
    message: string;
    iv: string;
}

export interface ChangePasswordRequestForUser {
    oldPassword: string;
    password: string;
}

export interface ResetPasswordRequest {
    Email: string;
    ResetCode: string;
    NewPassword: string;
}
