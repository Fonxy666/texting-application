import { Injectable } from '@angular/core';
import { CryptoKeys } from '../../model/auth-requests/CryptoKeys';

@Injectable({
    providedIn: 'root'
})
export class CryptoService {

    private crypto: SubtleCrypto;

    constructor() {
        this.crypto = window.crypto.subtle;
    }

    async generateKeyPair(): Promise<CryptoKeys> {
        try {
            const keyPair = await this.crypto.generateKey(
                {
                    name: "RSA-OAEP",
                    modulusLength: 2048,
                    publicExponent: new Uint8Array([0x01, 0x00, 0x01]),
                    hash: { name: "SHA-256" }
                },
                true,
                ["encrypt", "decrypt"]
            );
        
            const exportedPublicKey = await this.crypto.exportKey("spki", keyPair.publicKey);
        
            const exportedPrivateKey = await this.crypto.exportKey("pkcs8", keyPair.privateKey);
        
            let cryptoKeys = new CryptoKeys(this.arrayBufferToBase64(exportedPrivateKey), this.arrayBufferToBase64(exportedPublicKey));
        
            return cryptoKeys;
        } catch (error) {
            console.error("Error generating key pair:", error);
            throw error;
        }
    }

    async encryptPrivateKey(privateKey: ArrayBuffer, password: string): Promise<{ encryptedPrivateKey: string, salt: string }> {
        try {
            const salt = this.generateSalt();
        
            const derivedKey = await this.deriveKeyFromPassword(password, salt);
        
            const privateKeyArray = new Uint8Array(privateKey);
        
            const iv = window.crypto.getRandomValues(new Uint8Array(12)); 
            const encryptedPrivateKey = await window.crypto.subtle.encrypt(
                {
                name: 'AES-GCM',
                iv: iv,
                tagLength: 128
                },
                derivedKey,
                privateKeyArray
            );
        
            return {
                encryptedPrivateKey: this.arrayBufferToBase64(encryptedPrivateKey),
                salt: this.arrayBufferToBase64(salt)
            };
        } catch (error) {
            console.error("Error encrypting private key:", error);
            throw error;
        }
    }
    
    private async deriveKeyFromPassword(password: string, salt: ArrayBuffer): Promise<CryptoKey> {
        const encoder = new TextEncoder();
        const encodedPassword = encoder.encode(password);
        const subtleCrypto = window.crypto.subtle;
    
        const keyMaterial = await subtleCrypto.importKey(
            'raw',
            encodedPassword,
            { name: 'PBKDF2' },
            false,
            ['deriveBits', 'deriveKey']
        );
    
        const derivedKey = await subtleCrypto.deriveKey(
            {
                name: 'PBKDF2',
                salt: salt,
                iterations: 100000,
                hash: 'SHA-256'
            },
            keyMaterial,
            { name: 'AES-GCM', length: 256 },
            true,
            ['encrypt', 'decrypt']
        );
    
        return derivedKey;
    }

    private generateSalt(): Uint8Array {
        return window.crypto.getRandomValues(new Uint8Array(16));
    }

    private arrayBufferToBase64(buffer: ArrayBuffer): string {
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return btoa(binary);
    }
}
