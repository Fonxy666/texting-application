import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})

export class IndexedDBService {
    private dbName = 'encryptionKeys';
    private dbVersion = 1;
    private db!: IDBDatabase;
    public dbReady: Promise<IDBDatabase>;

    constructor() {
        this.dbReady = this.initDatabase();
    }

    private initDatabase(): Promise<IDBDatabase> {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(this.dbName, this.dbVersion);

            request.onerror = () => {
                console.error('Error opening IndexedDB:', request.error);
                reject(request.error);
            };

            request.onsuccess = () => {
                this.db = request.result;
                console.log('IndexedDB opened successfully');
                resolve(this.db);
            };

            request.onupgradeneeded = () => {
                const db = request.result;
                if (!db.objectStoreNames.contains('keys')) {
                    db.createObjectStore('keys', { keyPath: 'id' });
                }
            };
        });
    }

    storeEncryptionKey(userId: string, encryptionKey: string): Promise<boolean> {
        return this.dbReady.then((db) => {
            return new Promise((resolve) => {
                const transaction = db.transaction(['keys'], 'readwrite');
                const objectStore = transaction.objectStore('keys');
                const request = objectStore.put({ id: userId, key: encryptionKey });
    
                request.onsuccess = () => {
                    console.log('Encryption key stored in IndexedDB');
                    resolve(true);
                };
    
                request.onerror = () => {
                    console.error('Error storing encryption key:', request.error);
                    resolve(false);
                };
            });
        });
    }

    getEncryptionKey(userId: string): Promise<string | null> {
        return this.dbReady.then((db) => {
            return new Promise((resolve, reject) => {
                const transaction = db.transaction(['keys'], 'readonly');
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
        });
    }

    clearEncryptionKey(userId: string) {
        this.dbReady.then((db) => {
            const transaction = db.transaction(['keys'], 'readwrite');
            const objectStore = transaction.objectStore('keys');
            const request = objectStore.delete(userId);

            request.onsuccess = () => {
                console.log('Encryption key cleared from IndexedDB');
            };

            request.onerror = (event) => {
                console.error('Error clearing encryption key:', request.error);
            };
        });
    }
}