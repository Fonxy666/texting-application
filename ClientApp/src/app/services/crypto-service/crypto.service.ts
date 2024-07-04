import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class CryptoService {

    constructor() { }

    async generateKeyPair() {
        const crypto = window.crypto.subtle;
        
        const keyPair = await crypto.generateKey(
            {
                name: "RSA-OAEP",
                modulusLength: 2048,
                publicExponent: new Uint8Array([1, 0, 1]),
                hash: "SHA-256"
            },
            true,
            ["encrypt", "decrypt"]
        );
      
        const publicKey = await crypto.exportKey("spki", keyPair.publicKey);
        const privateKey = await crypto.exportKey("pkcs8", keyPair.privateKey);
        
        return {
            publicKey: window.btoa(String.fromCharCode(...new Uint8Array(publicKey))),
            privateKey: window.btoa(String.fromCharCode(...new Uint8Array(privateKey)))
        };
    }

    async sendPublicKeyToServer(userId: string, publicKey: string) {
            const response = await fetch('placeholder', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ userId, publicKey })
        });
        
        if (!response.ok) {
            throw new Error('Failed to save public key');
        }
    }
}
