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

    private bufferToBase64(buffer: ArrayBuffer): string {
        let binary = '';
        const bytes = new Uint8Array(buffer);
        const len = bytes.byteLength;
        for (let i = 0; i < len; i++) {
          binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
    }

    async generateSymmetricKey() {
        const key = await window.crypto.subtle.generateKey(
            {
                name: "AES-GCM",
                length: 256
            },
            true,
            ["encrypt", "decrypt"]
        );
      
        const exportedKey = await window.crypto.subtle.exportKey('raw', key);
        return this.bufferToBase64(exportedKey);
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

    async deriveKey(password: string) {
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

    async decryptPrivateKey(encryptedPrivateKey: string, password: string, ivBase64: string) {
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
    }
}
