import { Injectable } from '@angular/core';
import { ErrorHandlerService } from '../error-handler-service/error-handler.service';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, Observable } from 'rxjs';
import { StoreRoomSymmetricKey } from '../../model/room-requests/StoreRoomSymmetricKey';

@Injectable({
    providedIn: 'root'
})
export class CryptoService {

    constructor(
        private errorHandler: ErrorHandlerService,
        private http: HttpClient
    ) { }

    async generateKeyPair() {
        const keyPair = await window.crypto.subtle.generateKey(
            {
                name: "RSA-OAEP",
                modulusLength: 2048,
                publicExponent: new Uint8Array([1, 0, 1]),
                hash: "SHA-256"
            },
            true,
            ["encrypt", "decrypt"]
        );
      
        const publicKey = await window.crypto.subtle.exportKey('spki', keyPair.publicKey);
        const privateKey = await window.crypto.subtle.exportKey('pkcs8', keyPair.privateKey);
      
        return {
            publicKey: this.bufferToBase64(publicKey),
            privateKey: this.bufferToBase64(privateKey)
        };
    }
    
    async generateSymmetricKey(): Promise<ArrayBuffer> {
        const key = await window.crypto.subtle.generateKey(
            {
                name: "AES-GCM",
                length: 256
            },
            true,
            ["encrypt", "decrypt"]
        );
    
        return await window.crypto.subtle.exportKey('raw', key);
    }

    bufferToBase64(buffer: ArrayBuffer): string {
        return btoa(String.fromCharCode(...new Uint8Array(buffer)));
    }

    async encryptPrivateKey(privateKey: string, password: string) {
        const key = await this.deriveKey(password);
      
        const iv = window.crypto.getRandomValues(new Uint8Array(12));
        const encryptedData = await window.crypto.subtle.encrypt(
            {
                name: "AES-GCM",
                iv: iv
            },
            key,
            this.base64ToBuffer(privateKey)
        );
      
        return {
            iv: this.bufferToBase64(iv.buffer),
            encryptedPrivateKey: this.bufferToBase64(encryptedData)
        };
    }

    isBase64(str: string): boolean {
        console.log("PEC56BWNBtO4lE9z61BaKW15yA==".length)
        if (str.length === 24 && !str.includes(" ")) {
            return true;
        } 
        return false;
    }

    async decryptPrivateKey(encryptedPrivateKey: string, password: string, ivBase64: string) {
        try {
            const key = await this.deriveKey(password);
            const iv = this.base64ToBuffer(ivBase64);
        
            const decryptedData = await window.crypto.subtle.decrypt(
                {
                    name: "AES-GCM",
                    iv: new Uint8Array(iv)
                },
                key,
                this.base64ToBuffer(encryptedPrivateKey)
            );
        
            return this.bufferToBase64(decryptedData);
        } catch (error) {
            console.error('Error in decryptPrivateKey:', error);
            return null;
        }
    }

    async deriveKey(password: string) {
        try {
            const passwordKey = await window.crypto.subtle.importKey(
                'raw',
                new TextEncoder().encode(password),
                'PBKDF2',
                false,
                ['deriveKey']
            );
          
            return window.crypto.subtle.deriveKey(
                {
                    name: 'PBKDF2',
                    salt: new Uint8Array(16),
                    iterations: 100000,
                    hash: 'SHA-256'
                },
                passwordKey,
                {
                    name: 'AES-GCM',
                    length: 256
                },
                false,
                ['encrypt', 'decrypt']
            );
        } catch (error) {
            console.error('Error in deriveKey:', error);
            throw error;
        }
    }

    base64ToBuffer(base64: string): ArrayBuffer {
        const binary = window.atob(base64);
        const len = binary.length;
        const buffer = new ArrayBuffer(len);
        const bytes = new Uint8Array(buffer);

        for (let i = 0; i < len; i++) {
             bytes[i] = binary.charCodeAt(i);
        }
        return buffer;
    }

    async encryptSymmetricKey(symmetricKey: ArrayBuffer, publicKey: CryptoKey): Promise<ArrayBuffer> {
        const encryptedSymmetricKey = await window.crypto.subtle.encrypt(
            {
                name: 'RSA-OAEP'
            },
            publicKey,
            symmetricKey
        );
    
        return encryptedSymmetricKey;
    }
    
    async decryptSymmetricKey(encryptedSymmetricKey: ArrayBuffer, privateKey: CryptoKey): Promise<CryptoKey> {
        const decryptedSymmetricKeyBuffer = await window.crypto.subtle.decrypt(
            {
                name: 'RSA-OAEP'
            },
            privateKey,
            encryptedSymmetricKey
        );
 
        const symmetricKey = await window.crypto.subtle.importKey(
            'raw',
            decryptedSymmetricKeyBuffer,
            { name: 'AES-GCM' },
            true,
            ['encrypt', 'decrypt']
        );
    
        return symmetricKey;
    }

    async exportCryptoKey(key: CryptoKey): Promise<ArrayBuffer> {
        return window.crypto.subtle.exportKey('raw', key);
      }

    async importPublicKeyFromBase64(base64PublicKey: string): Promise<CryptoKey> {
        const publicKeyBytes = this.base64ToBuffer(base64PublicKey);
        return await window.crypto.subtle.importKey(
            'spki',
            publicKeyBytes,
            {
                name: 'RSA-OAEP',
                hash: { name: 'SHA-256' },
            },
            true,
            ['encrypt']
        );
    }
    
    async importPrivateKeyFromBase64(base64PrivateKey: string): Promise<CryptoKey> {
        const privateKeyBytes = this.base64ToBuffer(base64PrivateKey);
        return await window.crypto.subtle.importKey(
            'pkcs8',
            privateKeyBytes,
            {
                name: 'RSA-OAEP',
                hash: { name: 'SHA-256' },
            },
            true,
            ['decrypt']
        );
    }

    async encryptMessage(message: string, symmetricKey: CryptoKey): Promise<any> {
        try {
            const iv = window.crypto.getRandomValues(new Uint8Array(12));
            const encryptedData = await window.crypto.subtle.encrypt(
                {
                    name: "AES-GCM",
                    iv: iv
                },
                symmetricKey,
                new TextEncoder().encode(message)
            );
    
            return {
                iv: this.bufferToBase64(iv.buffer),
                encryptedMessage: this.bufferToBase64(encryptedData)
            };
        } catch (error) {
            console.error('Error in encryptMessage:', error);
            throw error;
        }
    }

    async decryptMessage(encryptedMessage: string, symmetricKey: CryptoKey, ivBase64: string): Promise<string> {
        try {
            const iv = this.base64ToBuffer(ivBase64);
            const decryptedData = await window.crypto.subtle.decrypt(
                {
                    name: "AES-GCM",
                    iv: new Uint8Array(iv)
                },
                symmetricKey,
                this.base64ToBuffer(encryptedMessage)
            );
    
            return new TextDecoder().decode(decryptedData);
        } catch (error) {
            console.error('Error in decryptMessage:', error);
            throw error;
        }
    }

    async decryptRoomKeyWithUserKey(userPublicKey: string, encryptionInput: string, roomId: string): Promise<string> {
        const cryptoKeyUserPublicKey = await this.importPublicKeyFromBase64(userPublicKey);
        const userEncryptedData = await firstValueFrom(this.getUserPrivateKeyAndIv());
        const encryptedRoomSymmetricKey = await firstValueFrom(this.getUserPrivateKeyForRoom(roomId));
        const encryptedRoomSymmetricKeyToArrayBuffer = this.base64ToBuffer(encryptedRoomSymmetricKey.encryptedKey);
        const decryptedUserPrivateKey = await this.decryptPrivateKey(userEncryptedData.encryptedPrivateKey, encryptionInput, userEncryptedData.iv);
        const decryptedUserCryptoPrivateKey = await this.importPrivateKeyFromBase64(decryptedUserPrivateKey!);
        const decryptedRoomKey = await this.decryptSymmetricKey(encryptedRoomSymmetricKeyToArrayBuffer, decryptedUserCryptoPrivateKey);
        const keyToArrayBuffer = await this.exportCryptoKey(decryptedRoomKey);
        const encryptRoomKeyForUser = await this.encryptSymmetricKey(keyToArrayBuffer, cryptoKeyUserPublicKey);
        return this.bufferToBase64(encryptRoomKeyForUser);
    }

    async decryptSymmetricKeyWrapper(roomId: string, userKey: string): Promise<CryptoKey> {
        const userEncryptedData = await firstValueFrom(this.getUserPrivateKeyAndIv());
        const encryptedRoomSymmetricKey = await firstValueFrom(this.getUserPrivateKeyForRoom(roomId));
        const encryptedRoomSymmetricKeyToArrayBuffer = this.base64ToBuffer(encryptedRoomSymmetricKey.encryptedKey);
        const decryptedUserPrivateKey = await this.decryptPrivateKey(userEncryptedData.encryptedPrivateKey, userKey, userEncryptedData.iv);
        const decryptedUserCryptoPrivateKey = await this.importPrivateKeyFromBase64(decryptedUserPrivateKey!);
        return await this.decryptSymmetricKey(encryptedRoomSymmetricKeyToArrayBuffer, decryptedUserCryptoPrivateKey);
    }

    async encryptMessageWrapper(roomId: string, userKey: string, inputMessage: string): Promise<any> {
        const userEncryptedData = await firstValueFrom(this.getUserPrivateKeyAndIv());
        const encryptedRoomSymmetricKey = await firstValueFrom(this.getUserPrivateKeyForRoom(roomId));
        const encryptedRoomSymmetricKeyToArrayBuffer = this.base64ToBuffer(encryptedRoomSymmetricKey.encryptedKey);
        const decryptedUserPrivateKey = await this.decryptPrivateKey(userEncryptedData.encryptedPrivateKey, userKey, userEncryptedData.iv);
        const decryptedUserCryptoPrivateKey = await this.importPrivateKeyFromBase64(decryptedUserPrivateKey!);
        const decryptedRoomKey = await this.decryptSymmetricKey(encryptedRoomSymmetricKeyToArrayBuffer, decryptedUserCryptoPrivateKey);
        return await this.encryptMessage(inputMessage, decryptedRoomKey);
    }

    getUserPrivateKeyAndIv(): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/CryptoKey/GetPrivateKeyAndIv`, { withCredentials: true })
        )
    }

    getUserPrivateKeyForRoom(roomId: string): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/CryptoKey/GetPrivateUserKey?roomId=${roomId}`, { withCredentials: true })
        )
    }

    sendEncryptedRoomKey(data: StoreRoomSymmetricKey): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.post(`/api/v1/CryptoKey/SaveEncryptedRoomKey`, data, { withCredentials: true })
        )
    }

    getPublicKey(userName: string): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/CryptoKey/GetPublicKey?userName=${userName}`, { withCredentials: true })
        )
    }

    userHaveKeyForRoom(userName: string, roomId: string): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/CryptoKey/ExamineIfUserHaveSymmetricKeyForRoom?userName=${userName}&roomId=${roomId}`, { withCredentials: true })
        )
    }
}
