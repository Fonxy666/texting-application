import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})

export class IndexedDBService {
    private dbName = 'encryptionKeys';
    private dbVersion = 1;
    private db!: IDBDatabase;

    constructor() {
        this.initDatabase();
    }

    private initDatabase() {
        const request = indexedDB.open(this.dbName, this.dbVersion);

        request.onerror = () => {
            console.error('Error opening IndexedDB:', request.error);
        };

        request.onsuccess = () => {
            this.db = request.result;
            console.log('IndexedDB opened successfully');
        };

        request.onupgradeneeded = () => {
            const db = request.result;
            if (!db.objectStoreNames.contains('keys')) {
                db.createObjectStore('keys', { keyPath: 'id' });
            }
        };
    }

    storeEncryptionKey(userId: string, encryptionKey: string) {
        const transaction = this.db.transaction(['keys'], 'readwrite');
        const objectStore = transaction.objectStore('keys');
        const request = objectStore.put({ id: userId, key: encryptionKey });

        request.onsuccess = () => {
            console.log('Encryption key stored in IndexedDB');
        };

        request.onerror = (event) => {
            console.error('Error storing encryption key:', request.error);
        };
    }

    getEncryptionKey(userId: string): Promise<string | null> {
        return new Promise((resolve, reject) => {
            const transaction = this.db.transaction(['keys'], 'readonly');
            const objectStore = transaction.objectStore('keys');
            const request = objectStore.get(userId);
    
            request.onsuccess = () => {
                if (request.result) {
                    resolve(request.result.key);
                } else {
                    resolve(null);
                }
            };
    
            request.onerror = () => {
                reject(`Error retrieving encryption key: ${request.error}`);
            };
        });
    }

    clearEncryptionKey(userId: string) {
        const transaction = this.db.transaction(['keys'], 'readwrite');
        const objectStore = transaction.objectStore('keys');
        const request = objectStore.delete(userId);

        request.onsuccess = () => {
            console.log('Encryption key cleared from IndexedDB');
        };

        request.onerror = (event) => {
            console.error('Error clearing encryption key:', request.error);
        };
    }
}