import { Injectable } from '@angular/core';
import CryptoJS from 'crypto-js';
import { Observable } from 'rxjs';
import { ErrorHandlerService } from '../error-handler-service/error-handler.service';
import { HttpClient } from '@angular/common/http';

@Injectable({
    providedIn: 'root'
})
export class CryptoService {

    constructor(
        private errorHandler: ErrorHandlerService,
        private http: HttpClient
    ) { }

    setEncryptionKeyFromUserInput(key: string): string {
        return CryptoJS.SHA256(key).toString(CryptoJS.enc.Hex).substring(0, 32);
    }
    
    encryptMessage(message: string, key: string): string {
        const ciphertext = CryptoJS.AES.encrypt(message, key).toString();
        return ciphertext;
    }
    
    decryptText(ciphertext: string, key: string): string | null {
        try {
            console.log(ciphertext);
            console.log(key);
            const parsedKey = CryptoJS.enc.Hex.parse(key);
            const bytes = CryptoJS.AES.decrypt(ciphertext, parsedKey, {
                mode: CryptoJS.mode.ECB,
                padding: CryptoJS.pad.Pkcs7
            });
            const plaintext = bytes.toString(CryptoJS.enc.Utf8);
    
            if (!plaintext) {
                throw new Error("Decryption returned empty plaintext");
            }
    
            return plaintext;
        } catch (error) {
            console.error("Error in decryptText:", error);
            return null;
        }
    }

    async generateKeyPair() {
        const keyPair = await window.crypto.subtle.generateKey(
        {
            name: "RSA-OAEP",
            modulusLength: 2048,
            publicExponent: new Uint8Array([1, 0, 1]),
            hash: "SHA-256",
        },
        true,
        ["encrypt", "decrypt"]
        );
        return keyPair;
    }

    async exportKey(key: CryptoKey) {
        return window.crypto.subtle.exportKey("jwk", key);
    }

    async exportKeyPair(keyPair: { publicKey: CryptoKey; privateKey: CryptoKey; }) {
        const publicKeyJwk = await this.exportKey(keyPair.publicKey);
        const privateKeyJwk = await this.exportKey(keyPair.privateKey);
        return { publicKeyJwk, privateKeyJwk };
    }

    async encryptPrivateKey(privateKeyJwk: JsonWebKey, password: string): Promise<string> {
        const privateKeyString = JSON.stringify(privateKeyJwk);
        const encryptionKey = this.setEncryptionKeyFromUserInput(password);
        const encryptedPrivateKey = this.encryptMessage(privateKeyString, encryptionKey);

        return encryptedPrivateKey;
    }

    async decryptPrivateKey(encryptedPrivateKey: string, password: string): Promise<any> {
        try {
            console.log(encryptedPrivateKey)
            console.log(password);
            const encryptionKey = this.setEncryptionKeyFromUserInput(password);
    
            const decryptedPrivateKeyString = this.decryptText(encryptedPrivateKey, encryptionKey);
    
            if (!decryptedPrivateKeyString) {
                throw new Error("Decryption of private key failed");
            }
    
            const decryptedPrivateKeyJwk: JsonWebKey = JSON.parse(decryptedPrivateKeyString);
    
            return decryptedPrivateKeyJwk;
        } catch (error) {
            console.error("Error in decryptPrivateKey:", error);
            return null;
        }
    }

    async generateSymmetricJsonWebKey(): Promise<JsonWebKey> {
        const randomBytes = CryptoJS.lib.WordArray.random(32);
        const symmetricKey = CryptoJS.enc.Base64.stringify(randomBytes);
    
        const jwk: JsonWebKey = {
            kty: 'oct',
            k: symmetricKey,
        };
    
        return jwk;
    }

    async distributeSymmetricKey(encryptedSymmetricKey: string, userPrivateKey: CryptoKey, newUserPublicKey: CryptoKey): Promise<string> {
        const encryptedSymmetricKeyBuffer = Uint8Array.from(atob(encryptedSymmetricKey), c => c.charCodeAt(0));
    
        const decryptedSymmetricKeyBuffer = await crypto.subtle.decrypt(
            {
                name: "RSA-OAEP"
            },
            userPrivateKey,
            encryptedSymmetricKeyBuffer
        );
    
        const newEncryptedSymmetricKeyBuffer = await crypto.subtle.encrypt(
            {
                name: "RSA-OAEP"
            },
            newUserPublicKey,
            decryptedSymmetricKeyBuffer
        );
    
        return btoa(String.fromCharCode(...new Uint8Array(newEncryptedSymmetricKeyBuffer)));
    }

    getEncryptedPrivateKey(): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/CryptoKey/GetPrivateKey`, { withCredentials: true })
        )
    }
}
