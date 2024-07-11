export class RegistrationRequest {
    constructor(
        public email: string,
        public username: string,
        public password: string,
        public image: string,
        public phoneNumber: string,
        public publicKey: string,
        public encryptedPrivateKey: string,
        public iv: string
    ) { }
}