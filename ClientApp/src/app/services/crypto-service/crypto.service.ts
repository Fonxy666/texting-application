import { Injectable } from '@angular/core';
import CryptoJS from 'crypto-js';

@Injectable({
    providedIn: 'root'
})
export class CryptoService {

    constructor() { }

    setEncryptionKeyFromUserInput(key: string): string {
        return CryptoJS.SHA256(key).toString(CryptoJS.enc.Hex).substring(0, 32);
    }
    
    encryptMessage(message: string, key: string): string {
        const ciphertext = CryptoJS.AES.encrypt(message, key).toString();
        return ciphertext;
    }
    
    decryptMessage(ciphertext: string, key: string): string {
        const bytes = CryptoJS.AES.decrypt(ciphertext, key);
        const plaintext = bytes.toString(CryptoJS.enc.Utf8);
        return plaintext;
    }
}
